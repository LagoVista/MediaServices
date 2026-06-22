using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models.Resources;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    public enum GeneratedImageStyles
    {
        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_StudioPortrait, MediaServicesResources.Names.GeneratedImageStyles_StudioPortrait, typeof(MediaServicesResources))]
        StudioPortrait,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_EditorialPhotography, MediaServicesResources.Names.GeneratedImageStyles_EditorialPhotography, typeof(MediaServicesResources))]
        EditorialPhotography,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_FlatIllustration, MediaServicesResources.Names.GeneratedImageStyles_FlatIllustration, typeof(MediaServicesResources))]
        FlatIllustration,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_EditorialIllustration, MediaServicesResources.Names.GeneratedImageStyles_EditorialIllustration, typeof(MediaServicesResources))]
        EditorialIllustration,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_CorporateMemphis, MediaServicesResources.Names.GeneratedImageStyles_CorporateMemphis, typeof(MediaServicesResources))]
        CorporateMemphis,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_Abstract, MediaServicesResources.Names.GeneratedImageStyles_Abstract, typeof(MediaServicesResources))]
        Abstract,

        [EnumLabel(ImageGenerationRequest.GeneratedImageStyles_ThreeDimensionalIllustration, MediaServicesResources.Names.GeneratedImageStyles_ThreeDimensionalIllustration, typeof(MediaServicesResources))]
        ThreeDimensionalIllustration
    }

    public enum GeneratedImageQualities
    {
        [EnumLabel(ImageGenerationRequest.GeneratedImageQualities_Standard, MediaServicesResources.Names.GeneratedImageQualities_Standard, typeof(MediaServicesResources))]
        Standard,

        [EnumLabel(ImageGenerationRequest.GeneratedImageQualities_Premium, MediaServicesResources.Names.GeneratedImageQualities_Premium, typeof(MediaServicesResources))]
        Premium
    }

    public enum GeneratedImageSizes
    {
        [EnumLabel(ImageGenerationRequest.GeneratedImageSizes_Square, MediaServicesResources.Names.GeneratedImageSizes_Square, typeof(MediaServicesResources))]
        Square1024x1024,

        [EnumLabel(ImageGenerationRequest.GeneratedImageSizes_Landscape, MediaServicesResources.Names.GeneratedImageSizes_Landscape, typeof(MediaServicesResources))]
        Landscape1536x1024,

        [EnumLabel(ImageGenerationRequest.GeneratedImageSizes_Portrait, MediaServicesResources.Names.GeneratedImageSizes_Portrait, typeof(MediaServicesResources))]
        Portrait1024x1536,
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.ImageGenerationRequest_Title, MediaServicesResources.Names.ImageGenerationRequest_Help, MediaServicesResources.Names.ImageGenerationRequest_Description, EntityDescriptionAttribute.EntityTypes.ChildObject, typeof(MediaServicesResources), 
        FactoryUrl: "/api/media/imagegeneration/factory")]
    public class ImageGenerationRequest : IFormDescriptor
    {
        public const string GeneratedImageStyles_StudioPortrait = "studioportrait";
        public const string GeneratedImageStyles_EditorialPhotography = "editorialphotography";
        public const string GeneratedImageStyles_FlatIllustration = "flatillustration";
        public const string GeneratedImageStyles_EditorialIllustration = "editorialillustration";
        public const string GeneratedImageStyles_CorporateMemphis = "corporatememphis";
        public const string GeneratedImageStyles_Abstract = "abstract";
        public const string GeneratedImageStyles_ThreeDimensionalIllustration = "threedimensionalillustration";

        public const string GeneratedImageQualities_Standard = "standard";
        public const string GeneratedImageQualities_Premium = "premium";

        public const string GeneratedImageSizes_Square = "square1024x1024";
        public const string GeneratedImageSizes_Landscape = "landscape1536x1024";
        public const string GeneratedImageSizes_Portrait = "portrait1024x1536";

        public string ResourceName { get; set; }

        public string EntityId { get; set; }

        public string EntityTypeName { get; set; }

        public string EntityFieldName { get; set; }

        public string MediaResourceId { get; set; }

        public string PreviousResponseId { get; set; }

        public string ReferenceMediaResourceId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_ImageStyle, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_ImageStyle_Help, EnumType: typeof(GeneratedImageStyles), FieldType: FieldTypes.Picker, IsRequired: true, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<GeneratedImageStyles> ImageStyle { get; set; } = EntityHeader<GeneratedImageStyles>.Create(GeneratedImageStyles.EditorialPhotography);

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_ImageSize, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_ImageSize_Help, EnumType: typeof(GeneratedImageSizes), FieldType: FieldTypes.Picker, IsRequired: true, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<GeneratedImageSizes> ImageSize { get; set; } = EntityHeader<GeneratedImageSizes>.Create(GeneratedImageSizes.Square1024x1024);

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_ImageQuality, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_ImageQuality_Help, EnumType: typeof(GeneratedImageQualities), FieldType: FieldTypes.Picker, IsRequired: true, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<GeneratedImageQualities> ImageQuality { get; set; } = EntityHeader<GeneratedImageQualities>.Create(GeneratedImageQualities.Standard);

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_NumberGenerated, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_NumberGenerated_Help, FieldType: FieldTypes.Integer, IsRequired: true, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public int NumberGenerated { get; set; } = 1;

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_ImagePurpose, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_ImagePurpose_Help, FieldType: FieldTypes.MultiLineText, IsRequired: false, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string ImagePurpose { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_ImageGenerationStyleGuidance, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_ImageGenerationStyleGuidance_Help, FieldType: FieldTypes.MultiLineText, IsRequired: false, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string ImageGenerationStyleGuidance { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_UserPrompt, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_UserPrompt_Help, FieldType: FieldTypes.MultiLineText, IsRequired: true, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public string UserPrompt { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.ImageGenerationRequest_IsPublic, HelpResource: MediaServicesResources.Names.ImageGenerationRequest_IsPublic_Help, FieldType: FieldTypes.CheckBox, IsRequired: false, IsUserEditable: true, ResourceType: typeof(MediaServicesResources))]
        public bool IsPublic { get; set; } = true;

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(ImageStyle),
                nameof(ImageSize),
                nameof(ImageQuality),
                nameof(NumberGenerated),
                nameof(ImagePurpose),
                nameof(ImageGenerationStyleGuidance),
                nameof(UserPrompt),
                nameof(IsPublic)
            };
        }

        public ImageGenerationRequest CreateSnapshot()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ImageGenerationRequest>(json);
        }
    }

    public class ImageGenerationResponse
    {
        public string ImageUrl { get; set; }

        public string NewResponse { get; set; }
    }
}