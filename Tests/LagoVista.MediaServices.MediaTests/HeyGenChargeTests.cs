using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class HeyGenChargeTests
    {
        private sealed class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
            public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) { _respond = respond; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_respond(request));
            }
        }

        private static HeyGenVideoService CreateService(Func<HttpRequestMessage, string> respond)
        {
            return CreateService(request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respond(request))
            });
        }

        private static HeyGenVideoService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(client => client.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(new TestHandler(respond)));
            var settings = new Mock<IMediaServicesConnectionSettings>();
            settings.SetupGet(s => s.HeyGenApiKey).Returns("test-key");

            return new HeyGenVideoService(factory.Object,
                new Mock<IAdminLogger>().Object,
                settings.Object,
                new Mock<ISecureStorage>().Object,
                new Mock<ICacheProvider>().Object);
        }

        [TestMethod]
        public async Task RequestLogs_Retry503AndUseConfirmedUsdWhenProviderRecovers()
        {
            var requests = 0;
            var service = CreateService(request =>
            {
                requests++;
                if (requests <= 2)
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("temporary provider outage")
                    };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{""data"":[{""request_id"":""request-recovered"",
                        ""endpoint"":""/v3/videos"",""job"":{""id"":""video-503"",""status"":""completed""},
                        ""charge_recorded"":true,""charge"":{""amount"":1.08,""unit"":""credits"",""amount_usd"":1.08}}],
                        ""has_more"":false}")
                };
            });

            var result = await service.GetVideoChargeAsync("video-503", DateTimeOffset.UtcNow.ToString("O"));

            Assert.IsTrue(result.Successful);
            Assert.AreEqual(3, requests);
            Assert.AreEqual(1.08m, result.Result.AmountUsd.Value);
            Assert.AreEqual("request-recovered", result.Result.RequestId);
        }

        [TestMethod]
        public async Task RequestLogs_StopRetrying503AfterBoundedAttempts()
        {
            var requests = 0;
            var service = CreateService(request =>
            {
                requests++;
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("provider still unavailable")
                };
            });

            var result = await service.GetVideoChargeAsync("video-503", DateTimeOffset.UtcNow.ToString("O"));

            Assert.IsFalse(result.Successful);
            Assert.AreEqual(4, requests);
            StringAssert.Contains(result.Errors[0].Message, "status 503");
        }

        [TestMethod]
        public async Task RequestLogs_DoNotRetryNonTransientErrors()
        {
            var requests = 0;
            var service = CreateService(request =>
            {
                requests++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("invalid provider key")
                };
            });

            var result = await service.GetVideoChargeAsync("video-auth", DateTimeOffset.UtcNow.ToString("O"));

            Assert.IsFalse(result.Successful);
            Assert.AreEqual(1, requests);
            StringAssert.Contains(result.Errors[0].Message, "status 401");
        }

        [TestMethod]
        public async Task RequestLogs_ReturnProviderCredits_WithoutInventingUsdCost()
        {
            var service = CreateService(request =>
            {
                StringAssert.Contains(request.RequestUri.Query, "job_id=video-001");
                return @"{""data"":[
                    {""request_id"":""request-1"",""endpoint"":""/v3/videos"",""job"":{""id"":""video-001"",""status"":""completed""},
                    ""charge"":{""amount"":12.5,""unit"":""credits"",""amount_usd"":null,""refunded"":false},""charge_recorded"":true},
                    {""request_id"":""request-2"",""job"":{""id"":""someone-else"",""status"":""completed""},
                    ""charge"":{""amount"":99,""unit"":""credits""},""charge_recorded"":true}
                    ],""has_more"":false}";
            });

            var result = await service.GetVideoChargeAsync("video-001", DateTimeOffset.UtcNow.AddDays(-2).ToString("O"));
            Assert.IsTrue(result.Successful);
            Assert.IsTrue(result.Result.Found);
            Assert.IsTrue(result.Result.ChargeRecorded);
            Assert.AreEqual(1, result.Result.MatchingRequests);
            Assert.AreEqual(12.5m, result.Result.Amount.Value);
            Assert.AreEqual("credits", result.Result.Unit);
            Assert.IsFalse(result.Result.AmountUsd.HasValue);
            Assert.AreEqual("request-1", result.Result.RequestId);
        }

        [TestMethod]
        public async Task RequestLogs_DistinguishMissingChargeFromConfirmedZeroCharge()
        {
            var noRecord = CreateService(request => @"{""data"":[{""job"":{""id"":""video-002""},
                ""charge_recorded"":false,""charge"":null}],""has_more"":false}");
            var pending = await noRecord.GetVideoChargeAsync("video-002", DateTimeOffset.UtcNow.ToString("O"));
            Assert.IsTrue(pending.Successful);
            Assert.IsFalse(pending.Result.ChargeRecorded);
            Assert.IsFalse(pending.Result.Amount.HasValue);

            var zeroRecord = CreateService(request => @"{""data"":[{""job"":{""id"":""video-002""},
                ""charge_recorded"":true,""charge"":null}],""has_more"":false}");
            var confirmed = await zeroRecord.GetVideoChargeAsync("video-002", DateTimeOffset.UtcNow.ToString("O"));
            Assert.IsTrue(confirmed.Successful);
            Assert.IsTrue(confirmed.Result.ChargeRecorded);
            Assert.AreEqual(0m, confirmed.Result.Amount.Value);
        }

        [TestMethod]
        public async Task RequestLogs_FollowPaginationToken()
        {
            var requests = 0;
            var service = CreateService(request =>
            {
                requests++;
                if (requests == 1)
                    return @"{""data"":[],""has_more"":true,""next_token"":""cursor-2""}";
                StringAssert.Contains(request.RequestUri.Query, "token=cursor-2");
                return @"{""data"":[{""request_id"":""request-2"",""job"":{""id"":""video-003"",""status"":""completed""},
                    ""charge_recorded"":true,""charge"":{""amount"":1.5,""unit"":""minutes"",""amount_usd"":2.25}}],
                    ""has_more"":false}";
            });

            var result = await service.GetVideoChargeAsync("video-003", DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            Assert.IsTrue(result.Successful);
            Assert.AreEqual(2, requests);
            Assert.AreEqual(1.5m, result.Result.Amount.Value);
            Assert.AreEqual(2.25m, result.Result.AmountUsd.Value);
            Assert.AreEqual("minutes", result.Result.Unit);
        }
    }
}
