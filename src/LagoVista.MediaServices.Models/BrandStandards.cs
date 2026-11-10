using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LagoVista.MediaServices.Models
{
    [CollectionName("MediaResources")]
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.BrandStandards_Title,
        MediaServicesResources.Names.BrandStandards_Help, MediaServicesResources.Names.BrandStandards_Description,
        EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/media-library-default",
        FactoryUrl: "/api/media/brandstandards/factory", GetListUrl: "/api/media/brandstandards",
        GetUrl: "/api/media/brandstandards/{id}", SaveUrl: "/api/media/brandstandards",
        DeleteUrl: "/api/media/brandstandards/{id}")]
    public class BrandStandards : EntityBase, IDescriptionEntity, IValidateable, IFormDescriptor, ISummaryFactory, IIconEntity
    {
        public BrandStandards()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/media-library-default";
            IsActive = true;
        }

        [FormField(LabelResource: MediaServicesResources.Names.Common_IsActive, FieldType: FieldTypes.CheckBox,
            ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public bool IsActive { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_BrandIdentityArtifact,
            HelpResource: MediaServicesResources.Names.BrandStandards_BrandIdentityArtifact_Help,
            FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public EntityHeader BrandIdentityArtifact { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_Logos,
            HelpResource: MediaServicesResources.Names.BrandStandards_Logos_Help,
            FieldType: FieldTypes.ChildListInline, ChildListDisplayMembers: "role,mediaResource,usageGuidance",
            ChildListDisplayMember: nameof(BrandStandardLogo.Role), IsReferenceField: false,
            FactoryUrl: "/api/media/brandstandards/logo/factory",
            ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public List<BrandStandardLogo> Logos { get; set; } = new List<BrandStandardLogo>();

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_Colors,
            HelpResource: MediaServicesResources.Names.BrandStandards_Colors_Help,
            FieldType: FieldTypes.ChildListInline, ChildListDisplayMembers: "name,role,hexColor",
            ChildListDisplayMember: nameof(BrandStandardColor.Name), IsReferenceField: false,
            FactoryUrl: "/api/media/brandstandards/color/factory",
            ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public List<BrandStandardColor> Colors { get; set; } = new List<BrandStandardColor>();

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_HeadingFontFamily,
            FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string HeadingFontFamily { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_BodyFontFamily,
            FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string BodyFontFamily { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_TypographyGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string TypographyGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_LogoUsageGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string LogoUsageGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_VoiceGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string VoiceGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_ToneGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string ToneGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_MessagingGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string MessagingGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_TerminologyGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string TerminologyGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_WritingGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string WritingGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_UsageGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string UsageGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandards_AvoidGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string AvoidGuidance { get; set; }

        public List<string> GetFormFields()
        {
            return new List<string>
            {
                nameof(Name), nameof(Key), nameof(Icon), nameof(Description), nameof(IsActive),
                nameof(BrandIdentityArtifact), nameof(Logos), nameof(Colors),
                nameof(HeadingFontFamily), nameof(BodyFontFamily), nameof(TypographyGuidance),
                nameof(LogoUsageGuidance), nameof(VoiceGuidance), nameof(ToneGuidance),
                nameof(MessagingGuidance), nameof(TerminologyGuidance), nameof(WritingGuidance),
                nameof(UsageGuidance), nameof(AvoidGuidance)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Name))
                result.AddUserError("Brand standards require a name.");
            if (String.IsNullOrWhiteSpace(Key))
                result.AddUserError("Brand standards require a key.");

            foreach (var logo in Logos ?? new List<BrandStandardLogo>())
            {
                if (logo == null)
                    result.AddUserError("Brand standards cannot contain an empty logo.");
                else
                    logo.Validate(result);
            }

            foreach (var color in Colors ?? new List<BrandStandardColor>())
            {
                if (color == null)
                    result.AddUserError("Brand standards cannot contain an empty palette entry.");
                else
                    color.Validate(result);
            }
        }

        public BrandStandardsSummary CreateSummary()
        {
            var summary = new BrandStandardsSummary { IsActive = IsActive };
            summary.Populate(this);
            return summary;
        }

        ISummaryData ISummaryFactory.CreateSummary() => CreateSummary();
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.BrandStandards_ListTitle,
        MediaServicesResources.Names.BrandStandards_Help, MediaServicesResources.Names.BrandStandards_Description,
        EntityDescriptionAttribute.EntityTypes.Summary, ResourceType: typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/media-library-default",
        FactoryUrl: "/api/media/brandstandards/factory", GetListUrl: "/api/media/brandstandards",
        GetUrl: "/api/media/brandstandards/{id}", SaveUrl: "/api/media/brandstandards",
        DeleteUrl: "/api/media/brandstandards/{id}")]
    public class BrandStandardsSummary : SummaryData
    {
        public bool IsActive { get; set; }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.BrandStandardLogo_Title,
        MediaServicesResources.Names.BrandStandardLogo_Help, MediaServicesResources.Names.BrandStandardLogo_Description,
        EntityDescriptionAttribute.EntityTypes.SimpleModel, typeof(MediaServicesResources),
        FactoryUrl: "/api/media/brandstandards/logo/factory")]
    public class BrandStandardLogo : IFormDescriptor, IValidateable
    {
        public BrandStandardLogo() { Id = Guid.NewGuid().ToId(); }

        public string Id { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardLogo_Role,
            HelpResource: MediaServicesResources.Names.BrandStandardLogo_Role_Help,
            FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Role { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardLogo_MediaResource,
            FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources),
            IsRequired: true, IsUserEditable: true)]
        public EntityHeader MediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardLogo_UsageGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string UsageGuidance { get; set; }

        public List<string> GetFormFields() => new List<string>
        {
            nameof(Role), nameof(MediaResource), nameof(UsageGuidance)
        };

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Role))
                result.AddUserError("A brand logo must have a role.");
            if (MediaResource == null || String.IsNullOrWhiteSpace(MediaResource.Id))
                result.AddUserError("A brand logo must reference a Media Resource.");
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.BrandStandardColor_Title,
        MediaServicesResources.Names.BrandStandardColor_Help, MediaServicesResources.Names.BrandStandardColor_Description,
        EntityDescriptionAttribute.EntityTypes.SimpleModel, typeof(MediaServicesResources),
        FactoryUrl: "/api/media/brandstandards/color/factory")]
    public class BrandStandardColor : IFormDescriptor, IValidateable
    {
        public BrandStandardColor() { Id = Guid.NewGuid().ToId(); }

        public string Id { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardColor_Name,
            FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Name { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardColor_Role,
            HelpResource: MediaServicesResources.Names.BrandStandardColor_Role_Help,
            FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Role { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardColor_HexColor,
            HelpResource: MediaServicesResources.Names.BrandStandardColor_HexColor_Help,
            FieldType: FieldTypes.Color, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string HexColor { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.BrandStandardColor_UsageGuidance,
            FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public string UsageGuidance { get; set; }

        public List<string> GetFormFields() => new List<string>
        {
            nameof(Name), nameof(Role), nameof(HexColor), nameof(UsageGuidance)
        };

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Name))
                result.AddUserError("A brand color must have a name.");
            if (String.IsNullOrWhiteSpace(Role))
                result.AddUserError("A brand color must have a role.");
            if (String.IsNullOrWhiteSpace(HexColor) || !Regex.IsMatch(HexColor, @"^#[0-9A-Fa-f]{6}$"))
                result.AddUserError("A brand color must use a six-digit hex value such as #123ABC.");
        }
    }
}
