using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class BrandStandardsTests
    {
        [TestMethod]
        public void NewBrandStandardsHasEditableCollectionsAndIsActive()
        {
            var standards = new BrandStandards();
            Assert.IsTrue(standards.IsActive);
            Assert.IsNotNull(standards.Logos);
            Assert.IsNotNull(standards.Colors);
            var fields = standards.GetFormFields();
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.BrandIdentityArtifact)));
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.Logos)));
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.Colors)));
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.HeadingFontFamily)));
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.VoiceGuidance)));
            Assert.IsTrue(fields.Contains(nameof(BrandStandards.AvoidGuidance)));
        }

        [TestMethod]
        public void BrandStandardsSummaryCarriesActivationState()
        {
            var standards = new BrandStandards { Name = "AppTrex", Key = "apptrex", IsActive = false };
            var summary = standards.CreateSummary();
            Assert.AreEqual("AppTrex", summary.Name);
            Assert.IsFalse(summary.IsActive);
        }

        [TestMethod]
        public void BrandStandardsAllowsDraftBeforeVisualStandardsAreProvided()
        {
            var standards = new BrandStandards { Name = "AppTrex", Key = "apptrex" };
            var validation = new ValidationResult();
            standards.Validate(validation);
            Assert.IsTrue(validation.Successful);
        }

        [TestMethod]
        public void BrandStandardsRejectsInvalidColorOrLogoReference()
        {
            var standards = new BrandStandards { Name = "AppTrex", Key = "apptrex" };
            standards.Colors.Add(new BrandStandardColor { Name = "Primary", Role = "Primary", HexColor = "blue" });
            standards.Logos.Add(new BrandStandardLogo { Role = "Primary" });
            var validation = new ValidationResult();
            standards.Validate(validation);
            Assert.IsFalse(validation.Successful);
        }

        [TestMethod]
        public void BrandStandardsValidatesWellFormedLogoAndPaletteEntries()
        {
            var standards = new BrandStandards { Name = "AppTrex", Key = "apptrex" };
            standards.Colors.Add(new BrandStandardColor { Name = "Primary", Role = "Primary", HexColor = "#184B77" });
            standards.Logos.Add(new BrandStandardLogo
            {
                Role = "Primary",
                MediaResource = new EntityHeader { Id = "LOGO1", Text = "AppTrex Logo" }
            });
            var validation = new ValidationResult();
            standards.Validate(validation);
            Assert.IsTrue(validation.Successful);
        }
    }
}
