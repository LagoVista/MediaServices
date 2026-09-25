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
using System.Security.Cryptography;
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

        private static MediaServicesManager CreateManager(Mock<IMediaServicesRepo> repo, Mock<ISecurity> security = null)
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
                new Mock<IAppConfig>().Object,
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
    }
}
