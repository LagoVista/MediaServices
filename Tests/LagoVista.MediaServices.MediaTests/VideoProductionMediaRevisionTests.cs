using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class VideoProductionMediaRevisionTests
    {
        private static MediaResource CreateVideo()
        {
            return new MediaResource
            {
                CurrentRevision = "REV-NEW",
                StorageReferenceName = "new.webm",
                ThumbnailStorageReferenceName = "new.jpg",
                History = new List<MediaResourceHistory>
                {
                    new MediaResourceHistory
                    {
                        Id = "REV-OLD",
                        StorageReferenceName = "old.webm",
                        ThumbnailStorageReferenceName = "old.jpg",
                        Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready)
                    },
                    new MediaResourceHistory
                    {
                        Id = "REV-NEW",
                        StorageReferenceName = "new.webm",
                        ThumbnailStorageReferenceName = "new.jpg",
                        Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready)
                    },
                    new MediaResourceHistory
                    {
                        Id = "REV-PENDING",
                        StorageReferenceName = "pending.webm",
                        Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending)
                    }
                }
            };
        }

        [TestMethod]
        public void HistoricalPreviewUsesItsOwnRevision()
        {
            var media = CreateVideo();
            Assert.AreEqual("old.webm", VideoProductionMediaRevision.ResolveStorageReferenceName(media, "rev-old"));
            Assert.AreEqual("new.webm", VideoProductionMediaRevision.ResolveStorageReferenceName(media, "REV-NEW"));
        }

        [TestMethod]
        public void CurrentVideoPreviewPreservesExistingBehavior()
        {
            var media = CreateVideo();
            Assert.AreEqual("new.webm", VideoProductionMediaRevision.ResolveStorageReferenceName(media));
            Assert.AreEqual("new.jpg", VideoProductionMediaRevision.ResolveStorageReferenceName(media, thumbnail: true));
        }

        [TestMethod]
        public void MissingRevisionNeverFallsBackToCurrent()
        {
            var media = CreateVideo();
            Assert.IsNull(VideoProductionMediaRevision.ResolveStorageReferenceName(media, "DOES-NOT-EXIST"));
        }

        [TestMethod]
        public void UnreadyRevisionCannotBePlayed()
        {
            var media = CreateVideo();
            Assert.IsNull(VideoProductionMediaRevision.ResolveStorageReferenceName(media, "REV-PENDING"));
        }

        [TestMethod]
        public void HistoricalThumbnailUsesSelectedRevision()
        {
            var media = CreateVideo();
            Assert.AreEqual("old.jpg", VideoProductionMediaRevision.ResolveStorageReferenceName(media, "REV-OLD", true));
        }
    }
}
