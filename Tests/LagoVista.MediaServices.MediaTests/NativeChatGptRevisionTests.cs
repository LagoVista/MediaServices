using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class NativeChatGptRevisionTests
    {
        [TestMethod]
        public void SetContentTypeAllocatesNewObjectWithoutChangingPriorRevision()
        {
            const string originalBlob = "original-media-file.media";
            const string originalRevisionId = "FA7F4652AE1846A6AC6F9E5231367206";
            var previous = new MediaResourceHistory
            {
                Id = originalRevisionId,
                StorageReferenceName = originalBlob,
                MimeType = "image/png",
                ContentSize = 2106427
            };
            var resource = new MediaResource
            {
                StorageReferenceName = originalBlob,
                MimeType = "image/png",
                CurrentRevision = originalRevisionId,
                History = new List<MediaResourceHistory> { previous }
            };

            resource.SetContentType("image/png");

            Assert.AreNotEqual(originalBlob, resource.StorageReferenceName);
            Assert.AreEqual("image/png", resource.MimeType);
            Assert.AreEqual(originalRevisionId, resource.CurrentRevision);
            Assert.AreEqual(originalBlob, resource.History[0].StorageReferenceName);
            Assert.AreEqual(1, resource.History.Count);
        }

        [TestMethod]
        public void RepeatedRevisionsReceiveDistinctStorageReferences()
        {
            var resource = new MediaResource { StorageReferenceName = "first.media" };
            resource.SetContentType("image/png");
            var second = resource.StorageReferenceName;
            resource.SetContentType("image/png");
            var third = resource.StorageReferenceName;

            Assert.AreNotEqual("first.media", second);
            Assert.AreNotEqual(second, third);
        }
    }
}
