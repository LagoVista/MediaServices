using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class ChatGptFileProbeTests
    {
        private static ChatGptFileReference File(string url = "https://files.oaiusercontent.com/file.png") =>
            new ChatGptFileReference { FileId = "file_test_123", DownloadUrl = url, FileName = "image.png" };

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public StubHandler(HttpResponseMessage response) => _response = response;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_response);
        }

        private static HttpClient Client(HttpResponseMessage response) => new HttpClient(new StubHandler(response));

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        [TestMethod]
        public async Task HostedPngIsStreamedAndHashedWithoutPersistence()
        {
            var image = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 1, 2, 3, 4, 5 };
            using (var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(image)
            })
            using (var client = Client(response))
            {
                var result = await ChatGptFileProbe.ProbeAsync(File(), client);
                Assert.IsTrue(result.Successful);
                Assert.AreEqual(image.Length, result.Result.ContentSize);
                Assert.AreEqual(Sha256(image), result.Result.ContentSha256);
                Assert.AreEqual("image/png", result.Result.DetectedMimeType);
                Assert.AreEqual("file_test_123", result.Result.FileId);
            }
        }

        [TestMethod]
        public async Task NonTrustedHostIsRejected()
        {
            using (var client = Client(new HttpResponseMessage(HttpStatusCode.OK)))
            {
                var result = await ChatGptFileProbe.ProbeAsync(File("https://127.0.0.1/private"), client);
                Assert.IsFalse(result.Successful);
            }
        }

        [TestMethod]
        public async Task RedirectIsRejectedRatherThanFollowed()
        {
            using (var response = new HttpResponseMessage(HttpStatusCode.Redirect))
            using (var client = Client(response))
            {
                var result = await ChatGptFileProbe.ProbeAsync(File(), client);
                Assert.IsFalse(result.Successful);
            }
        }

        [TestMethod]
        public async Task ArbitraryBytesAreNotAcceptedAsImages()
        {
            using (var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 })
            })
            using (var client = Client(response))
            {
                var result = await ChatGptFileProbe.ProbeAsync(File(), client);
                Assert.IsFalse(result.Successful);
            }
        }

        [TestMethod]
        public async Task OversizedContentLengthIsRejectedBeforeReading()
        {
            var content = new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            content.Headers.ContentLength = 20L * 1024 * 1024 + 1;
            using (var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content })
            using (var client = Client(response))
            {
                var result = await ChatGptFileProbe.ProbeAsync(File(), client);
                Assert.IsFalse(result.Successful);
            }
        }
    }
}
