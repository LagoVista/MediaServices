using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    [CollectionName("MediaResources")]
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.ImageFamily_Title,
        MediaServicesResources.Names.ImageFamily_Help, MediaServicesResources.Names.ImageFamily_Description,
        EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/media-library-default",
        FactoryUrl: "/api/media/imagefamily/factory", GetListUrl: "/api/media/imagefamilies",
        GetUrl: "/api/media/imagefamily/{id}", SaveUrl: "/api/media/imagefamily",
        DeleteUrl: "/api/media/imagefamily/{id}")]
    public class ImageFamily : EntityBase, IDescriptionEntity, IValidateable, IFormDescriptor, ISummaryFactory, IIconEntity
    {
        public ImageFamily()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/media-library-default";
            IsActive = true;
        }

        [FormField(LabelResource: MediaServicesResources.Names.Common_IsActive, FieldType: FieldTypes.CheckBox,
            ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public bool IsActive { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_StyleGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_StyleGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsRequired: true, IsUserEditable: true,
            ResourceType: typeof(MediaServicesResources))]
        public string StyleGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_PaletteGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_PaletteGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string PaletteGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_RenderingGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_RenderingGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string RenderingGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_MoodGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_MoodGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string MoodGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_CompositionGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_CompositionGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string CompositionGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_UsageGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_UsageGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string UsageGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageFamily_AvoidGuidance,
            HelpResource: MediaServicesResources.Names.ImageFamily_AvoidGuidance_Help,
            FieldType: FieldTypes.MultiLineText, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string AvoidGuidance { get; set; }

        public List<string> GetFormFields()
        {
            return new List<string>
            {
                nameof(Name),
                nameof(Key),
                nameof(Icon),
                nameof(Description),
                nameof(IsActive),
                nameof(StyleGuidance),
                nameof(PaletteGuidance),
                nameof(RenderingGuidance),
                nameof(MoodGuidance),
                nameof(CompositionGuidance),
                nameof(UsageGuidance),
                nameof(AvoidGuidance)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(StyleGuidance))
                result.AddUserError("An image family requires base style guidance.");
        }

        public ImageFamilySummary CreateSummary()
        {
            var summary = new ImageFamilySummary { IsActive = IsActive };
            summary.Populate(this);
            return summary;
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.ImageFamilies_Title,
        MediaServicesResources.Names.ImageFamily_Help, MediaServicesResources.Names.ImageFamily_Description,
        EntityDescriptionAttribute.EntityTypes.Summary, ResourceType: typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/media-library-default",
        FactoryUrl: "/api/media/imagefamily/factory", GetListUrl: "/api/media/imagefamilies",
        GetUrl: "/api/media/imagefamily/{id}", SaveUrl: "/api/media/imagefamily",
        DeleteUrl: "/api/media/imagefamily/{id}")]
    public class ImageFamilySummary : SummaryData
    {
        public bool IsActive { get; set; }
    }
}
