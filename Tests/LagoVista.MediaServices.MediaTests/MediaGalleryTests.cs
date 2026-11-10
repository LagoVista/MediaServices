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
        public void GeneratedVideoWithExternalUrlIsStillVideo()
        {
            Assert.AreEqual("video", MediaGalleryItem.DetectKind(
                "video/mp4", "Video", false,
                "https://nuviotdev.blob.core.windows.net/production/video.mp4"));
            Assert.AreEqual("audio", MediaGalleryItem.DetectKind(
                "audio/mpeg", "Audio", false, "https://example.com/audio.mp3"));
            Assert.AreEqual("document", MediaGalleryItem.DetectKind(
                "application/pdf", "Other", false, "https://example.com/report.pdf"));
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
        public void NewImageBriefDefaultsToPublicAndLandscapeWithoutSaving()
        {
            var brief = new MediaImageCreationOpen();

            Assert.IsTrue(brief.IsPublic);
            Assert.AreEqual(1536, brief.Width);
            Assert.AreEqual(1024, brief.Height);
            Assert.IsNotNull(brief.Libraries);
            Assert.AreEqual(0, brief.Libraries.Count);
            Assert.IsNotNull(brief.Families);
            Assert.AreEqual(0, brief.Families.Count);
            Assert.IsNull(brief.InitialFamilyId);
            Assert.IsNull(brief.Name);
            Assert.IsNull(brief.Description);
        }

        [TestMethod]
        public void ImageBriefSupportsModelPrefillAndPrivateSelection()
        {
            var brief = new MediaImageCreationOpen
            {
                Name = "Insiders hero",
                Description = "An AI-powered virtual team meeting",
                Width = 1920,
                Height = 1080,
                IsPublic = false,
                InitialLibraryId = "marketing",
            };
            brief.Libraries.Add(new MediaGalleryLibrary { Id = "marketing", Name = "Marketing" });
            brief.InitialFamilyId = "apptrex-family";
            brief.Families.Add(new MediaImageCreationFamily { Id = "apptrex-family", Name = "AppTrex", Key = "apptrex" });

            Assert.IsFalse(brief.IsPublic);
            Assert.AreEqual("An AI-powered virtual team meeting", brief.Description);
            Assert.AreEqual(1920, brief.Width);
            Assert.AreEqual(1080, brief.Height);
            Assert.AreEqual("marketing", brief.InitialLibraryId);
            Assert.AreEqual(1, brief.Libraries.Count);
            Assert.AreEqual("apptrex-family", brief.InitialFamilyId);
            Assert.AreEqual("apptrex", brief.Families[0].Key);
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
