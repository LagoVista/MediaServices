using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class MediaGalleryTests
    {
        [TestMethod]
        public void ClassifiesImagesVideoAndAudio()
        {
            Assert.AreEqual("image", MediaGalleryItem.DetectKind("image/webp", "Picture", true, null));
            Assert.AreEqual("video", MediaGalleryItem.DetectKind("video/mp4", "Video", true, null));
            Assert.AreEqual("audio", MediaGalleryItem.DetectKind("audio/mpeg", "Audio", true, null));
        }

        [TestMethod]
        public void ClassifiesDocumentsArchivesAndLinks()
        {
            Assert.AreEqual("document", MediaGalleryItem.DetectKind("application/pdf", "Manual", true, null));
            Assert.AreEqual("archive", MediaGalleryItem.DetectKind("application/zip", "Other", true, null));
            Assert.AreEqual("link", MediaGalleryItem.DetectKind(null, "Other", false, "https://example.com/resource"));
            Assert.AreEqual("file", MediaGalleryItem.DetectKind(null, null, true, null));
        }

        [TestMethod]
        public void LimitsOriginalFallbackToSmallBrowserImages()
        {
            Assert.IsTrue(MediaGalleryItem.AllowsOriginalFallback("image/png", 1436730, true));
            Assert.IsTrue(MediaGalleryItem.AllowsOriginalFallback("image/webp", 2348725, true));
            Assert.IsFalse(MediaGalleryItem.AllowsOriginalFallback("image/png", 5L * 1024 * 1024, true));
            Assert.IsFalse(MediaGalleryItem.AllowsOriginalFallback("video/mp4", 1000, true));
            Assert.IsFalse(MediaGalleryItem.AllowsOriginalFallback("image/svg+xml", 1000, true));
            Assert.IsFalse(MediaGalleryItem.AllowsOriginalFallback("image/png", null, true));
            Assert.IsFalse(MediaGalleryItem.AllowsOriginalFallback("image/png", 1000, false));
        }

        [TestMethod]
        public void PageSizeDefaultsToFifteenAndContainsNoMediaBytes()
        {
            var page = new MediaGalleryPage();
            Assert.AreEqual(15, page.PageSize);
            Assert.IsNotNull(page.Items);
            Assert.AreEqual(0, page.Items.Count);
        }
    }
}
