using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class ImmutableMediaRevisionTests
    {
        private const string MediaId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string NewMediaId = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private static readonly EntityHeader Org = EntityHeader.Create("org-1", "Organization");
        private static readonly EntityHeader OtherOrg = EntityHeader.Create("org-2", "Other Organization");
        private static readonly EntityHeader User = EntityHeader.Create("user-1", "User");

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", String.Empty).ToLowerInvariant();
        }

        private static MediaResource CreateResource(byte[] bytes, bool includeImmutableMetadata = true)
        {
            var revision = new MediaResourceHistory
            {
                Id = "revision-1",
                StorageReferenceName = "revision-1.media",
                FileName = includeImmutableMetadata ? "historical.pdf" : null,
                MimeType = includeImmutableMetadata ? "application/pdf" : null,
                ContentSize = includeImmutableMetadata ? bytes.LongLength : (long?)null,
                ContentSha256 = includeImmutableMetadata ? ComputeSha256(bytes) : null
            };

            var resource = new MediaResource
            {
                Id = MediaId,
                Name = "Media",
                Key = "media",
                OwnerOrganization = Org,
                FileName = "current.mp4",
                MimeType = "video/mp4",
                ContentSize = 999,
                ContentSha256 = "current-hash",
                CurrentRevision = revision.Id
            };

            resource.History.Add(revision);
            return resource;
        }

        private static MediaServicesManager CreateManager(Mock<IMediaServicesRepo> repo, Mock<ISecurity> security = null, Mock<IAppConfig> appConfig = null)
        {
            if (security == null)
            {
                security = new Mock<ISecurity>(MockBehavior.Loose);
                security.Setup(service => service.AuthorizeAsync(
                        It.IsAny<IOwnedEntity>(),
                        It.IsAny<AuthorizeResult.AuthorizeActions>(),
                        It.IsAny<EntityHeader>(),
                        It.IsAny<EntityHeader>(),
                        It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                security.Setup(service => service.AuthorizeAsync(
                        It.IsAny<EntityHeader>(),
                        It.IsAny<EntityHeader>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                    .Returns(Task.CompletedTask);
            }

            return new MediaServicesManager(
                repo.Object,
                new Mock<ICategoryManager>().Object,
                new Mock<IMediaLibraryRepo>().Object,
                new Mock<ITextToSpeechService>().Object,
                new Mock<IAdminLogger>().Object,
                (appConfig ?? new Mock<IAppConfig>()).Object,
                new Mock<IDependencyManager>().Object,
                security.Object);
        }

        [TestMethod]
        public async Task ImmutableReadUsesSelectedRevisionMetadataAndIdentity()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var resource = CreateResource(bytes);
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.IsTrue(result.Successful);
            Assert.AreEqual(MediaId, result.Result.MediaResourceId);
            Assert.AreEqual("revision-1", result.Result.RevisionId);
            Assert.AreEqual("historical.pdf", result.Result.FileName);
            Assert.AreEqual("application/pdf", result.Result.ContentType);
            Assert.IsTrue(result.Result.ContentSize.HasValue);
            Assert.AreEqual<long>(bytes.LongLength, result.Result.ContentSize.Value);
            Assert.AreEqual(ComputeSha256(bytes), result.Result.ContentSha256);
            Assert.IsTrue(result.Result.ContentSizeVerified);
            Assert.IsTrue(result.Result.ContentSha256Verified);
            CollectionAssert.AreEqual(bytes, result.Result.ContentBytes);
        }

        [TestMethod]
        public async Task ImmutableReadFailsOnSizeMismatch()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var resource = CreateResource(bytes);
            resource.History[0].ContentSize = bytes.LongLength + 1;
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "content-size verification");
        }

        [TestMethod]
        public async Task ImmutableReadFailsOnHashMismatch()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var resource = CreateResource(bytes);
            resource.History[0].ContentSha256 = ComputeSha256(new byte[] { 9, 9, 9 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "SHA-256 verification");
        }

        [TestMethod]
        public async Task MissingRevisionReturnsNormalizedFailureWithoutStorageRead()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "missing", Org, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "Could not find media revision");
            repo.Verify(item => item.GetMediaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task AuthorizationFailureOccursBeforeStorageRead()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var security = new Mock<ISecurity>(MockBehavior.Loose);
            security.Setup(service => service.AuthorizeAsync(
                    It.IsAny<IOwnedEntity>(),
                    It.IsAny<AuthorizeResult.AuthorizeActions>(),
                    It.IsAny<EntityHeader>(),
                    It.IsAny<EntityHeader>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new NotAuthorizedException("Denied"));

            await Assert.ThrowsExactlyAsync<NotAuthorizedException>(
                () => CreateManager(repo, security).GetImmutableMediaRevisionAsync(MediaId, "revision-1", Org, User));

            repo.Verify(item => item.GetMediaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task CrossOrganizationCallerCannotResolveRevision()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "revision-1", OtherOrg, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "active organization");
            repo.Verify(item => item.GetMediaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task LegacyRevisionWithoutIdentityMetadataRemainsReadableButUnverified()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var resource = CreateResource(bytes, false);
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetImmutableMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.IsTrue(result.Successful);
            Assert.IsNull(result.Result.ContentSize);
            Assert.IsNull(result.Result.ContentSha256);
            Assert.IsFalse(result.Result.ContentSizeVerified);
            Assert.IsFalse(result.Result.ContentSha256Verified);
            CollectionAssert.AreEqual(bytes, result.Result.ContentBytes);
        }

        [TestMethod]
        public async Task NewRevisionCreationCapturesImmutableMetadata()
        {
            var bytes = new byte[] { 5, 4, 3, 2, 1 };
            var repo = new Mock<IMediaServicesRepo>();
            MediaResource saved = null;
            repo.Setup(item => item.TryGetMediaResourceRecordAsync(NewMediaId)).ReturnsAsync((MediaResource)null);
            repo.Setup(item => item.AddMediaAsync(It.IsAny<byte[]>(), Org.Id, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(InvokeResult.Success);
            repo.Setup(item => item.AddOrUpdateMediaResourceAsync(It.IsAny<MediaResource>()))
                .Callback<MediaResource>(resource => saved = resource)
                .Returns(Task.CompletedTask);

            using (var stream = new MemoryStream(bytes))
            {
                var result = await CreateManager(repo).AddResourceMediaAsync(
                    NewMediaId, stream, "asset.mp4", "video/mp4", Org, User, saveResourceRecord: true);
                Assert.IsTrue(result.Successful);
            }

            Assert.IsNotNull(saved);
            Assert.AreEqual(1, saved.History.Count);
            Assert.AreEqual(saved.CurrentRevision, saved.History[0].Id);
            Assert.AreEqual("asset.mp4", saved.History[0].FileName);
            Assert.AreEqual("video/mp4", saved.History[0].MimeType);
            Assert.IsTrue(saved.History[0].ContentSize.HasValue);
            Assert.AreEqual<long>(bytes.LongLength, saved.History[0].ContentSize.Value);
            Assert.AreEqual(ComputeSha256(bytes), saved.History[0].ContentSha256);
            Assert.IsFalse(String.IsNullOrWhiteSpace(saved.History[0].StorageReferenceName));
        }

        [TestMethod]
        public async Task ExistingRevisionDownloadUsesRevisionMetadata()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var resource = CreateResource(bytes);
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.AreEqual("historical.pdf", result.FileName);
            Assert.AreEqual("application/pdf", result.ContentType);
            CollectionAssert.AreEqual(bytes, result.ImageBytes);
        }

        [TestMethod]
        public async Task ExistingRevisionDownloadFallsBackForLegacyMetadata()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var resource = CreateResource(bytes, false);
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaAsync("revision-1.media", Org.Id)).ReturnsAsync(InvokeResult<byte[]>.Create(bytes));

            var result = await CreateManager(repo).GetMediaRevisionAsync(MediaId, "revision-1", Org, User);

            Assert.AreEqual("current.mp4", result.FileName);
            Assert.AreEqual("video/mp4", result.ContentType);
            CollectionAssert.AreEqual(bytes, result.ImageBytes);
        }
        [TestMethod]
        public async Task ImmutableReadLeaseTargetsSelectedRevisionAndReturnsAuthoritativeMetadata()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var resource = CreateResource(bytes);
            resource.CurrentRevision = "some-new-current-revision";

            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaReadUrlAsync(
                    "revision-1.media",
                    Org.Id,
                    TimeSpan.FromMinutes(60),
                    VideoProcessorStorageUrlScope.Public,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvokeResult<string>.Create("https://media.example.test/revision-1.media?signature=one"));

            var before = DateTime.UtcNow;
            var result = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User);
            var after = DateTime.UtcNow;

            Assert.IsTrue(result.Successful);
            Assert.AreEqual(MediaId, result.Result.MediaResourceId);
            Assert.AreEqual("revision-1", result.Result.RevisionId);
            Assert.AreEqual("historical.pdf", result.Result.FileName);
            Assert.AreEqual("application/pdf", result.Result.ContentType);
            Assert.AreEqual(bytes.LongLength, result.Result.ContentLength);
            Assert.AreEqual(ComputeSha256(bytes), result.Result.ContentSha256);
            Assert.IsTrue(result.Result.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Result.ValidUntilUtc >= before.AddMinutes(60));
            Assert.IsTrue(result.Result.ValidUntilUtc <= after.AddMinutes(60).AddSeconds(1));

            repo.Verify(item => item.GetMediaReadUrlAsync(
                "revision-1.media",
                Org.Id,
                TimeSpan.FromMinutes(60),
                VideoProcessorStorageUrlScope.Public,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ImmutableReadLeaseRejectsCrossOrganizationBeforeUrlCreation()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);

            var result = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", OtherOrg, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "active organization");
            repo.Verify(item => item.GetMediaReadUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<VideoProcessorStorageUrlScope>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ImmutableReadLeaseRejectsMissingRevisionBeforeUrlCreation()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);

            var result = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "missing", Org, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "Could not find media revision");
            repo.Verify(item => item.GetMediaReadUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<VideoProcessorStorageUrlScope>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ImmutableReadLeaseLifetimeIsBounded()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);

            var tooLong = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User, 121);
            var invalid = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User, 0);

            Assert.IsFalse(tooLong.Successful);
            Assert.IsFalse(invalid.Successful);
            StringAssert.Contains(tooLong.ErrorMessage, "between 1 and 120 minutes");
            repo.Verify(item => item.GetMediaReadUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<VideoProcessorStorageUrlScope>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ImmutableReadLeaseRetryRegeneratesUrlForSameRevision()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.SetupSequence(item => item.GetMediaReadUrlAsync(
                    "revision-1.media",
                    Org.Id,
                    TimeSpan.FromMinutes(30),
                    VideoProcessorStorageUrlScope.Public,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvokeResult<string>.Create("https://media.example.test/revision-1.media?signature=one"))
                .ReturnsAsync(InvokeResult<string>.Create("https://media.example.test/revision-1.media?signature=two"));

            var manager = CreateManager(repo);
            var first = await manager.CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User, 30);
            var second = await manager.CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User, 30);

            Assert.IsTrue(first.Successful);
            Assert.IsTrue(second.Successful);
            Assert.AreEqual(first.Result.MediaResourceId, second.Result.MediaResourceId);
            Assert.AreEqual(first.Result.RevisionId, second.Result.RevisionId);
            Assert.AreNotEqual(first.Result.Url, second.Result.Url);
        }

        [TestMethod]
        public async Task ImmutableReadLeaseRejectsNonHttpsExternalUrl()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            repo.Setup(item => item.GetMediaReadUrlAsync(
                    "revision-1.media",
                    Org.Id,
                    TimeSpan.FromMinutes(60),
                    VideoProcessorStorageUrlScope.Public,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvokeResult<string>.Create("http://media.example.test/revision-1.media?signature=one"));

            var result = await CreateManager(repo).CreateImmutableMediaReadLeaseAsync(MediaId, "revision-1", Org, User);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "HTTPS");
        }


        [TestMethod]
        public async Task PublicMediaUrlReturnsWebsiteReadyUrlWithMetadata()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            resource.IsPublic = true;
            resource.History[0].Width = 1672;
            resource.History[0].Height = 941;
            resource.History[0].MimeType = "image/png";
            resource.History[0].FileName = "virtual-team.png";
            resource.History[0].ThumbnailStorageReferenceName = "thumb.webp";
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var config = new Mock<IAppConfig>();
            config.Setup(item => item.WebAddress).Returns("https://api-dev.nuviot.com/");

            var result = await CreateManager(repo, appConfig: config)
                .GetPublicMediaResourceUrlAsync(MediaId, Org, User);

            Assert.IsTrue(result.Successful, result.ErrorMessage);
            Assert.AreEqual($"https://api-dev.nuviot.com/api/media/resource/{Org.Id}/{MediaId}/download", result.Result.Url);
            Assert.AreEqual($"https://api-dev.nuviot.com/api/media/resource/{Org.Id}/{MediaId}/thumbnail", result.Result.ThumbnailUrl);
            Assert.AreEqual("virtual-team.png", result.Result.FileName);
            Assert.AreEqual("image/png", result.Result.MimeType);
            Assert.AreEqual(1672, result.Result.Width);
            Assert.AreEqual(941, result.Result.Height);
            Assert.AreEqual("revision-1", result.Result.CurrentRevisionId);
            Assert.IsTrue(result.Result.IsPublic);
        }

        [TestMethod]
        public async Task PublicMediaUrlRejectsPrivateResource()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var result = await CreateManager(repo).GetPublicMediaResourceUrlAsync(MediaId, Org, User);
            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "not public");
        }

        [TestMethod]
        public async Task PublicMediaUrlRejectsWrongOrganization()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            resource.IsPublic = true;
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var result = await CreateManager(repo).GetPublicMediaResourceUrlAsync(MediaId, OtherOrg, User);
            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "active organization");
        }

        [TestMethod]
        public async Task PublicMediaUrlRejectsNonHttpsBaseAddress()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            resource.IsPublic = true;
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var config = new Mock<IAppConfig>();
            config.Setup(item => item.WebAddress).Returns("http://internal-service.local");
            var result = await CreateManager(repo, appConfig: config)
                .GetPublicMediaResourceUrlAsync(MediaId, Org, User);
            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "HTTPS");
        }

        [TestMethod]
        public async Task PublicMediaUrlRejectsMissingResource()
        {
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync((MediaResource)null);
            var result = await CreateManager(repo).GetPublicMediaResourceUrlAsync(MediaId, Org, User);
            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "Could not find");
        }

        [TestMethod]
        public async Task PublicMediaUrlRejectsMissingDownloadableFile()
        {
            var resource = CreateResource(new byte[] { 1, 2, 3 });
            resource.IsPublic = true;
            resource.IsFileUpload = false;
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(item => item.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(resource);
            var result = await CreateManager(repo).GetPublicMediaResourceUrlAsync(MediaId, Org, User);
            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.ErrorMessage, "downloadable file");
        }

        [TestMethod]
        public void ImmutableReadLeaseContractDoesNotExposeStorageProviderVocabulary()
        {
            var propertyNames = String.Join(",", typeof(ImmutableMediaReadLease).GetProperties().Select(property => property.Name)).ToLowerInvariant();

            Assert.IsFalse(propertyNames.Contains("storage"));
            Assert.IsFalse(propertyNames.Contains("blob"));
            Assert.IsFalse(propertyNames.Contains("bucket"));
            Assert.IsFalse(propertyNames.Contains("seaweed"));
            Assert.IsFalse(propertyNames.Contains("azure"));
            Assert.IsFalse(propertyNames.Contains("s3"));
        }

    }
}
