using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class ImageFamilyTests
    {
        [TestMethod]
        public void NewImageFamilyIsActive()
        {
            var family = new ImageFamily();
            Assert.IsTrue(family.IsActive);
        }

        [TestMethod]
        public void ImageFamilySummaryCarriesActivationState()
        {
            var family = new ImageFamily
            {
                Name = "AppTrex",
                Key = "apptrex",
                IsActive = false,
                StyleGuidance = "Warm, approachable business imagery."
            };

            var summary = family.CreateSummary();
            Assert.IsFalse(summary.IsActive);
            Assert.AreEqual("AppTrex", summary.Name);
        }

        [TestMethod]
        public void StudioOpenModelSupportsCreateAndReopenWithoutPersistedWorkflowState()
        {
            var open = new MediaImageFamilyStudioOpen
            {
                Family = new ImageFamily { Name = "AppTrex", Key = "apptrex" },
            };
            Assert.IsNull(open.InitialFamilyId);
            Assert.AreEqual("AppTrex", open.Family.Name);
            Assert.IsNotNull(open.Families);
            Assert.IsNotNull(open.Libraries);
            Assert.AreEqual(0, open.Families.Count);
            Assert.AreEqual(0, open.Libraries.Count);
        }

        [TestMethod]
        public void ImageFamilyFormMetadataIncludesEditableGuidance()
        {
            var fields = new ImageFamily().GetFormFields();
            Assert.IsTrue(fields.Contains(nameof(ImageFamily.StyleGuidance)));
            Assert.IsTrue(fields.Contains(nameof(ImageFamily.PaletteGuidance)));
            Assert.IsTrue(fields.Contains(nameof(ImageFamily.AvoidGuidance)));
            Assert.IsTrue(fields.Contains(nameof(ImageFamily.IsActive)));
            Assert.IsTrue(fields.Contains(nameof(ImageFamily.ReferenceImages)));
            Assert.IsNotNull(new ImageFamily().ReferenceImages);
        }
    }
}
