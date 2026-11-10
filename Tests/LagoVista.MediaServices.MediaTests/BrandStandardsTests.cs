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
        [TestMethod]
        public void BrandStandardsFieldsProvideAiAuthoringPrompts()
        {
            var standardsFields = new[]
            {
                nameof(BrandStandards.BrandIdentityArtifact),
                nameof(BrandStandards.Logos),
                nameof(BrandStandards.Colors),
                nameof(BrandStandards.HeadingFontFamily),
                nameof(BrandStandards.BodyFontFamily),
                nameof(BrandStandards.TypographyGuidance),
                nameof(BrandStandards.LogoUsageGuidance),
                nameof(BrandStandards.VoiceGuidance),
                nameof(BrandStandards.ToneGuidance),
                nameof(BrandStandards.MessagingGuidance),
                nameof(BrandStandards.TerminologyGuidance),
                nameof(BrandStandards.WritingGuidance),
                nameof(BrandStandards.UsageGuidance),
                nameof(BrandStandards.AvoidGuidance)
            };

            foreach (var field in standardsFields)
            {
                AssertHasPrompt(typeof(BrandStandards), field);
            }

            foreach (var field in new[]
            {
                nameof(BrandStandardLogo.Role),
                nameof(BrandStandardLogo.MediaResource),
                nameof(BrandStandardLogo.UsageGuidance)
            })
            {
                AssertHasPrompt(typeof(BrandStandardLogo), field);
            }

            foreach (var field in new[]
            {
                nameof(BrandStandardColor.Name),
                nameof(BrandStandardColor.Role),
                nameof(BrandStandardColor.HexColor),
                nameof(BrandStandardColor.UsageGuidance)
            })
            {
                AssertHasPrompt(typeof(BrandStandardColor), field);
            }
        }

        private static void AssertHasPrompt(System.Type modelType, string fieldName)
        {
            var property = modelType.GetProperty(fieldName);
            Assert.IsNotNull(property, $"{modelType.Name}.{fieldName} must exist.");
            var attribute = property.GetCustomAttributes(typeof(LagoVista.Core.Attributes.FormFieldAttribute), true)
                .OfType<LagoVista.Core.Attributes.FormFieldAttribute>()
                .SingleOrDefault();
            Assert.IsNotNull(attribute, $"{modelType.Name}.{fieldName} must have FormField metadata.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(attribute.AiChatPrompt),
                $"{modelType.Name}.{fieldName} must give the AI field-authoring guidance.");
        }

    }
}
