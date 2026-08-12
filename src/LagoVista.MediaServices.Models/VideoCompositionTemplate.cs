using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.MediaServices.Models
{
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoCompositionTemplate_Title, MediaServicesResources.Names.VideoCompositionTemplate_Help, MediaServicesResources.Names.VideoCompositionTemplate_Description,
        EntityDescriptionAttribute.EntityTypes.CoreIoTModel, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default",
        ListUIUrl: "/contentmanagement/videocompositiontemplates", EditUIUrl: "/contentmanagement/videocompositiontemplate/{id}", CreateUIUrl: "/contentmanagement/videocompositiontemplate/add",
        PreviewUIUrl: "/contentmanagement/videocompositiontemplate/{id}",
        GetListUrl: "/api/media/videocompositiontemplates", SaveUrl: "/api/media/videocompositiontemplate", GetUrl: "/api/media/videocompositiontemplate/{id}", FactoryUrl: "/api/media/videocompositiontemplate/factory", DeleteUrl: "/api/media/videocompositiontemplate/{id}",
        ClusterKey: "video", ModelType: EntityDescriptionAttribute.ModelTypes.Document, Shape: EntityDescriptionAttribute.EntityShapes.Entity, Lifecycle: EntityDescriptionAttribute.Lifecycles.DesignTime,
        Sensitivity: EntityDescriptionAttribute.Sensitivities.Internal, IndexInclude: true, IndexTier: EntityDescriptionAttribute.IndexTiers.Primary, IndexPriority: 78, IndexTagsCsv: "media,video,composition,template")]
    public class VideoCompositionTemplate : EntityBase, IValidateable, IFormDescriptor, IFormDescriptorCol2, ISummaryFactory
    {
        public VideoCompositionTemplate()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/video-production-default";
            DefaultLocale = VideoComposition.DefaultLocaleCode;
            Blocks = new List<VideoCompositionBlock>();
            IsActive = true;
            Version = 1;
        }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_Version, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public int Version { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_IsActive, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public bool IsActive { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_DefaultLocale, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string DefaultLocale { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_BackgroundMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader BackgroundMediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_BackgroundAudioMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader BackgroundAudioMediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_BackgroundAudioVolume, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public double BackgroundAudioVolume { get; set; } = 0.20;

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_BackgroundAudioFadeInSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double BackgroundAudioFadeInSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_BackgroundAudioFadeOutSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double BackgroundAudioFadeOutSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_LoopBackgroundAudio, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public bool LoopBackgroundAudio { get; set; } = true;

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_OutputMediaLibrary, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader OutputMediaLibrary { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTemplate_Blocks, FieldType: FieldTypes.ChildListInline, ChildListDisplayMembers: "key,mediaResourceFileName,type,durationSeconds", ChildListDisplayMember: nameof(VideoCompositionBlock.Key), IsReferenceField: false, FactoryUrl: "/api/media/videocomposition/block/factory", ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public List<VideoCompositionBlock> Blocks { get; set; }

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(Icon),
                nameof(Description),
                nameof(DefaultLocale),
                nameof(BackgroundMediaResource),
                nameof(BackgroundAudioMediaResource),
                nameof(BackgroundAudioVolume),
                nameof(BackgroundAudioFadeInSeconds),
                nameof(BackgroundAudioFadeOutSeconds),
                nameof(LoopBackgroundAudio),
                nameof(OutputMediaLibrary),
                nameof(Blocks)
            };
        }

        public List<string> GetFormFieldsCol2()
        {
            return new List<string>()
            {
                nameof(Version),
                nameof(IsActive),
                nameof(Category)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                result.AddUserError("A video composition template requires a name.");
            }

            if (String.IsNullOrWhiteSpace(Key))
            {
                result.AddUserError("A video composition template requires a key.");
            }

            if (Version < 1)
            {
                result.AddUserError("Video composition template version must be greater than zero.");
            }

            if (String.IsNullOrWhiteSpace(DefaultLocale))
            {
                result.AddUserError("A video composition template requires a default locale.");
            }

            if (BackgroundAudioVolume < 0 || BackgroundAudioVolume > 1)
            {
                result.AddUserError("Video composition template background audio volume must be between zero and one.");
            }

            if (BackgroundAudioFadeInSeconds < 0 || BackgroundAudioFadeOutSeconds < 0)
            {
                result.AddUserError("Video composition template background audio fade durations cannot be negative.");
            }

            if (Blocks == null || Blocks.Count == 0)
            {
                result.AddUserError("A video composition template requires at least one block.");
                return;
            }

            var duplicateKeys = Blocks.Where(block => !String.IsNullOrWhiteSpace(block.Key)).GroupBy(block => block.Key, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            if (duplicateKeys.Count > 0)
            {
                result.AddUserError($"Video composition template block keys must be unique. Duplicate keys: {String.Join(", ", duplicateKeys)}.");
            }

            foreach (var block in Blocks)
            {
                block.Validate(result);
            }
        }

        public VideoCompositionTemplateSummary CreateSummary()
        {
            var summary = new VideoCompositionTemplateSummary
            {
                Version = Version,
                IsActive = IsActive,
                DefaultLocale = DefaultLocale,
                BlockCount = Blocks?.Count ?? 0,
                IsEntityProductionTemplate = IsActive && Blocks != null && Blocks.Count == 3 &&
                    Blocks.Count(block => block.Role == VideoCompositionBlockRole.Intro) == 1 &&
                    Blocks.Count(block => block.Role == VideoCompositionBlockRole.Content) == 1 &&
                    Blocks.Count(block => block.Role == VideoCompositionBlockRole.CallToAction) == 1,
                TotalDurationSeconds = CalculateKnownDurationSeconds()
            };

            summary.Populate(this);
            return summary;
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }

        private int? CalculateKnownDurationSeconds()
        {
            if (Blocks == null || Blocks.Count == 0 || Blocks.Any(block => !block.DurationSeconds.HasValue))
            {
                return null;
            }

            return Convert.ToInt32(Math.Ceiling(Blocks.Sum(block => block.DurationSeconds.Value)));
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoCompositionTemplate_Title, MediaServicesResources.Names.VideoCompositionTemplate_Help, MediaServicesResources.Names.VideoCompositionTemplate_Description,
        EntityDescriptionAttribute.EntityTypes.Summary, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default",
        GetUrl: "/api/media/videocompositiontemplate/{id}", GetListUrl: "/api/media/videocompositiontemplates", FactoryUrl: "/api/media/videocompositiontemplate/factory", SaveUrl: "/api/media/videocompositiontemplate", DeleteUrl: "/api/media/videocompositiontemplate/{id}",
        ListUIUrl: "/contentmanagement/videocompositiontemplates", EditUIUrl: "/contentmanagement/videocompositiontemplate/{id}", CreateUIUrl: "/contentmanagement/videocompositiontemplate/add")]
    public sealed class CreateVideoCompositionTemplateFromCompositionRequest
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public string Description { get; set; }

        public EntityHeader Category { get; set; }
    }

    public class VideoCompositionTemplateSummary : SummaryData
    {
        public int Version { get; set; }

        public bool IsActive { get; set; }

        public string DefaultLocale { get; set; }

        public int BlockCount { get; set; }

        public bool IsEntityProductionTemplate { get; set; }

        public int? TotalDurationSeconds { get; set; }
    }
}
