using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class VimeoPublishJobAuthorizationTests
    {
        private const string OrganizationId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string OtherOrganizationId = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private const string MediaId = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
        private const string OtherMediaId = "DDDDDDDDDDDDDDDDDDDDDDDDDDDD";

        private static VideoComposition Composition() => new VideoComposition
        {
            OwnerOrganization = EntityHeader.Create(OrganizationId, "Publishing organization"),
            PublishedVideoMediaResource = EntityHeader.Create(MediaId, "Vimeo publication")
        };

        private static VideoProcessorCallbackRegistration Registration() => new VideoProcessorCallbackRegistration
        {
            OrganizationId = OrganizationId,
            MediaResourceId = MediaId
        };

        private static async Task<InvokeResult<MediaResource>> ResolveAsync(
            Mock<IMediaServicesRepo> repo, VideoComposition composition, VideoProcessorCallbackRegistration registration)
        {
            var method = typeof(VideoAssemblyRequestManager).GetMethod(
                "GetRegisteredVimeoPublishingResourceAsync", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "The authenticated background publish path must keep its scoped resource resolver.");
            var task = (Task<InvokeResult<MediaResource>>)method.Invoke(
                null, new object[] { repo.Object, composition, registration });
            return await task;
        }

        [TestMethod]
        public async Task ValidRegisteredJobReadsOwnedResourceWithoutInteractiveUser()
        {
            var media = new MediaResource
            {
                Id = MediaId,
                OwnerOrganization = EntityHeader.Create(OrganizationId, "Publishing organization")
            };
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(r => r.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(media);

            var result = await ResolveAsync(repo, Composition(), Registration());

            Assert.IsTrue(result.Successful);
            Assert.AreSame(media, result.Result);
            repo.Verify(r => r.GetMediaResourceRecordAsync(MediaId), Times.Once);
        }

        [TestMethod]
        public async Task OtherOrganizationResourceIsRejectedAfterRepoRead()
        {
            var media = new MediaResource
            {
                Id = MediaId,
                OwnerOrganization = EntityHeader.Create(OtherOrganizationId, "Other organization")
            };
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(r => r.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync(media);

            var result = await ResolveAsync(repo, Composition(), Registration());

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.Errors[0].Message, "registered organization");
            repo.Verify(r => r.UpdateMediaResourceRecordAsync(It.IsAny<MediaResource>()), Times.Never);
        }

        [TestMethod]
        public async Task MismatchedRegisteredMediaIdIsRejectedBeforeRepoRead()
        {
            var registration = Registration();
            registration.MediaResourceId = OtherMediaId;
            var repo = new Mock<IMediaServicesRepo>();

            var result = await ResolveAsync(repo, Composition(), registration);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.Errors[0].Message, "does not match");
            repo.Verify(r => r.GetMediaResourceRecordAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task MismatchedRegisteredOrganizationIsRejectedBeforeRepoRead()
        {
            var registration = Registration();
            registration.OrganizationId = OtherOrganizationId;
            var repo = new Mock<IMediaServicesRepo>();

            var result = await ResolveAsync(repo, Composition(), registration);

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.Errors[0].Message, "registered organization");
            repo.Verify(r => r.GetMediaResourceRecordAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task MissingRegisteredResourceFailsClosed()
        {
            var repo = new Mock<IMediaServicesRepo>();
            repo.Setup(r => r.GetMediaResourceRecordAsync(MediaId)).ReturnsAsync((MediaResource)null);

            var result = await ResolveAsync(repo, Composition(), Registration());

            Assert.IsFalse(result.Successful);
            StringAssert.Contains(result.Errors[0].Message, "unavailable");
        }
    }
}
