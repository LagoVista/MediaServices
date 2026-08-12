using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public enum VideoCompositionStatus
    {
        [EnumLabel(VideoComposition.Status_Draft, MediaServicesResources.Names.VideoCompositionStatus_Draft, typeof(MediaServicesResources))]
        Draft,

        [EnumLabel(VideoComposition.Status_Preparing, MediaServicesResources.Names.VideoCompositionStatus_Preparing, typeof(MediaServicesResources))]
        Preparing,

        [EnumLabel(VideoComposition.Status_Queued, MediaServicesResources.Names.VideoCompositionStatus_Queued, typeof(MediaServicesResources))]
        Queued,

        [EnumLabel(VideoComposition.Status_Assembling, MediaServicesResources.Names.VideoCompositionStatus_Assembling, typeof(MediaServicesResources))]
        Assembling,

        [EnumLabel(VideoComposition.Status_Uploading, MediaServicesResources.Names.VideoCompositionStatus_Uploading, typeof(MediaServicesResources))]
        Uploading,

        [EnumLabel(VideoComposition.Status_ProcessingAtVimeo, MediaServicesResources.Names.VideoCompositionStatus_ProcessingAtVimeo, typeof(MediaServicesResources))]
        ProcessingAtVimeo,

        [EnumLabel(VideoComposition.Status_Completed, MediaServicesResources.Names.VideoCompositionStatus_Completed, typeof(MediaServicesResources))]
        Completed,

        [EnumLabel(VideoComposition.Status_Failed, MediaServicesResources.Names.VideoCompositionStatus_Failed, typeof(MediaServicesResources))]
        Failed,

        [EnumLabel(VideoComposition.Status_Cancelled, MediaServicesResources.Names.VideoCompositionStatus_Cancelled, typeof(MediaServicesResources))]
        Cancelled
    }

    public enum VideoCompositionBlockType
    {
        [EnumLabel(VideoComposition.BlockType_Image, MediaServicesResources.Names.VideoCompositionBlockType_Image, typeof(MediaServicesResources))]
        Image,

        [EnumLabel(VideoComposition.BlockType_Video, MediaServicesResources.Names.VideoCompositionBlockType_Video, typeof(MediaServicesResources))]
        Video
    }

    public enum VideoCompositionBlockRole
    {
        [EnumLabel(
            VideoComposition.BlockRole_None,
            MediaServicesResources.Names.VideoCompositionBlockRole_None,
            typeof(MediaServicesResources))]
        None,

        [EnumLabel(
            VideoComposition.BlockRole_Intro,
            MediaServicesResources.Names.VideoCompositionBlockRole_Intro,
            typeof(MediaServicesResources))]
        Intro,

        [EnumLabel(
            VideoComposition.BlockRole_Content,
            MediaServicesResources.Names.VideoCompositionBlockRole_Content,
            typeof(MediaServicesResources))]
        Content,

        [EnumLabel(
            VideoComposition.BlockRole_CallToAction,
            MediaServicesResources.Names.VideoCompositionBlockRole_CallToAction,
            typeof(MediaServicesResources))]
        CallToAction
    }

    public enum VideoCompositionTextAlignment
    {
        [EnumLabel(VideoComposition.TextAlignment_Left, MediaServicesResources.Names.VideoCompositionTextAlignment_Left, typeof(MediaServicesResources))]
        Left,

        [EnumLabel(VideoComposition.TextAlignment_Center, MediaServicesResources.Names.VideoCompositionTextAlignment_Center, typeof(MediaServicesResources))]
        Center,

        [EnumLabel(VideoComposition.TextAlignment_Right, MediaServicesResources.Names.VideoCompositionTextAlignment_Right, typeof(MediaServicesResources))]
        Right
    }

    public enum VideoCompositionLabelBinding
    {
        None,
        Title,
        Subtitle,
        CallToAction
    }

    public enum VideoCompositionAssemblyStage
    {
        [EnumLabel(VideoComposition.AssemblyStage_None, MediaServicesResources.Names.VideoCompositionAssemblyStage_None, typeof(MediaServicesResources))]
        None,

        [EnumLabel(VideoComposition.AssemblyStage_Queued, MediaServicesResources.Names.VideoCompositionAssemblyStage_Queued, typeof(MediaServicesResources))]
        Queued,

        [EnumLabel(VideoComposition.AssemblyStage_DownloadingMedia, MediaServicesResources.Names.VideoCompositionAssemblyStage_DownloadingMedia, typeof(MediaServicesResources))]
        DownloadingMedia,

        [EnumLabel(VideoComposition.AssemblyStage_InspectingMedia, MediaServicesResources.Names.VideoCompositionAssemblyStage_InspectingMedia, typeof(MediaServicesResources))]
        InspectingMedia,

        [EnumLabel(VideoComposition.AssemblyStage_RenderingLabels, MediaServicesResources.Names.VideoCompositionAssemblyStage_RenderingLabels, typeof(MediaServicesResources))]
        RenderingLabels,

        [EnumLabel(VideoComposition.AssemblyStage_NormalizingMedia, MediaServicesResources.Names.VideoCompositionAssemblyStage_NormalizingMedia, typeof(MediaServicesResources))]
        NormalizingMedia,

        [EnumLabel(VideoComposition.AssemblyStage_Encoding, MediaServicesResources.Names.VideoCompositionAssemblyStage_Encoding, typeof(MediaServicesResources))]
        Encoding,

        [EnumLabel(VideoComposition.AssemblyStage_GeneratingThumbnail, MediaServicesResources.Names.VideoCompositionAssemblyStage_GeneratingThumbnail, typeof(MediaServicesResources))]
        GeneratingThumbnail,

        [EnumLabel(VideoComposition.AssemblyStage_UploadingThumbnail, MediaServicesResources.Names.VideoCompositionAssemblyStage_UploadingThumbnail, typeof(MediaServicesResources))]
        UploadingThumbnail,

        [EnumLabel(VideoComposition.AssemblyStage_UploadingToAzure, MediaServicesResources.Names.VideoCompositionAssemblyStage_UploadingToAzure, typeof(MediaServicesResources))]
        UploadingToAzure,

        [EnumLabel(VideoComposition.AssemblyStage_UploadingToVimeo, MediaServicesResources.Names.VideoCompositionAssemblyStage_UploadingToVimeo, typeof(MediaServicesResources))]
        UploadingToVimeo,

        [EnumLabel(VideoComposition.AssemblyStage_Completed, MediaServicesResources.Names.VideoCompositionAssemblyStage_Completed, typeof(MediaServicesResources))]
        Completed,

        [EnumLabel(VideoComposition.AssemblyStage_Failed, MediaServicesResources.Names.VideoCompositionAssemblyStage_Failed, typeof(MediaServicesResources))]
        Failed
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoComposition_Title, MediaServicesResources.Names.VideoComposition_Help, MediaServicesResources.Names.VideoComposition_Description,
        EntityDescriptionAttribute.EntityTypes.CoreIoTModel, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default",
        ListUIUrl: "/contentmanagement/videocompositions", EditUIUrl: "/contentmanagement/videocomposition/{id}", CreateUIUrl: "/contentmanagement/videocomposition/add",
        PreviewUIUrl: "/contentmanagement/videocomposition/{id}",
        GetListUrl: "/api/media/videocompositions", SaveUrl: "/api/media/videocomposition", GetUrl: "/api/media/videocomposition/{id}", FactoryUrl: "/api/media/videocomposition/factory", DeleteUrl: "/api/media/videocomposition/{id}",
        ClusterKey: "video", ModelType: EntityDescriptionAttribute.ModelTypes.Document, Shape: EntityDescriptionAttribute.EntityShapes.Entity, Lifecycle: EntityDescriptionAttribute.Lifecycles.RunTime,
        Sensitivity: EntityDescriptionAttribute.Sensitivities.Internal, IndexInclude: true, IndexTier: EntityDescriptionAttribute.IndexTiers.Primary, IndexPriority: 80, IndexTagsCsv: "media,video,composition,assembly,vimeo")]
    public class VideoComposition : EntityBase, IValidateable, IFormDescriptor, IFormDescriptorCol2, ISummaryFactory
    {
        public const string Status_Draft = "draft";
        public const string Status_Preparing = "preparing";
        public const string Status_Queued = "queued";
        public const string Status_Assembling = "assembling";
        public const string Status_Uploading = "uploading";
        public const string Status_ProcessingAtVimeo = "processing-at-vimeo";
        public const string Status_Completed = "completed";
        public const string Status_Failed = "failed";
        public const string Status_Cancelled = "cancelled";

        public const string BlockRole_None = "none";
        public const string BlockRole_Intro = "intro";
        public const string BlockRole_Content = "content";
        public const string BlockRole_CallToAction = "call-to-action";

        public const string BlockType_Image = "image";
        public const string BlockType_Video = "video";

        public const string TextAlignment_Left = "left";
        public const string TextAlignment_Center = "center";
        public const string TextAlignment_Right = "right";

        public const string AssemblyStage_None = "none";
        public const string AssemblyStage_Queued = "queued";
        public const string AssemblyStage_DownloadingMedia = "downloading-media";
        public const string AssemblyStage_InspectingMedia = "inspecting-media";
        public const string AssemblyStage_RenderingLabels = "rendering-labels";
        public const string AssemblyStage_NormalizingMedia = "normalizing-media";
        public const string AssemblyStage_Encoding = "encoding";
      
        public const string AssemblyStage_GeneratingThumbnail = "generating-thumbnail";
        public const string AssemblyStage_UploadingThumbnail = "uploading-thumbnail";
        public const string AssemblyStage_UploadingToAzure = "uploading-to-azure";
        public const string AssemblyStage_UploadingToVimeo = "uploading-to-vimeo";
        public const string AssemblyStage_Completed = "completed";
        public const string AssemblyStage_Failed = "failed";

        public const string DefaultLocaleCode = "en-US";

        public VideoComposition()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/video-production-default";
            Status = EntityHeader<VideoCompositionStatus>.Create(VideoCompositionStatus.Draft);
            DefaultLocale = DefaultLocaleCode;
            StatusChangedUtc = UtcTimestamp.Now;
        }

        [FormField(LabelResource: MediaServicesResources.Names.VideoComposition_Status, FieldType: FieldTypes.Picker, EnumType: typeof(VideoCompositionStatus), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: false)]
        public EntityHeader<VideoCompositionStatus> Status { get; set; }

        public string DefaultLocale { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string CallToAction { get; set; }

        public EntityHeader SourceEntity { get; set; }

        public string SourceEntityType { get; set; }

        public EntityHeader SourceCompositionTemplate { get; set; }

        public int SourceCompositionTemplateVersion { get; set; }

        public EntityHeader SourceVideoAvatar { get; set; }

        public string SourceScript { get; set; }

        public string SourceContentSha256 { get; set; }

        public string NotificationRunId { get; set; }

        public bool IsReady { get; set; }

        public string CurrentInputSha256 { get; set; }

        public string ExecutionInputSha256 { get; set; }

        public string OutputInputSha256 { get; set; }

        public bool IsCurrent =>
            IsReady &&
            !String.IsNullOrWhiteSpace(CurrentInputSha256) &&
            !String.IsNullOrWhiteSpace(OutputInputSha256) &&
            String.Equals(CurrentInputSha256, OutputInputSha256, StringComparison.OrdinalIgnoreCase);

        public string StatusChangedUtc { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoComposition_BackgroundMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader BackgroundMediaResource { get; set; }

        public EntityHeader BackgroundAudioMediaResource { get; set; }

        public double BackgroundAudioVolume { get; set; } = 0.20;

        public double BackgroundAudioFadeInSeconds { get; set; }

        public double BackgroundAudioFadeOutSeconds { get; set; }

        public bool LoopBackgroundAudio { get; set; } = true;

        [FormField(LabelResource: MediaServicesResources.Names.VideoComposition_OutputMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public EntityHeader OutputMediaResource { get; set; }

        public EntityHeader OutputMediaLibrary { get; set; }

        public EntityHeader PublishedVideoMediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoComposition_Blocks, FieldType: FieldTypes.ChildListInline, ChildListDisplayMembers: "key,mediaResourceFileName,type,durationSeconds", ChildListDisplayMember: nameof(VideoCompositionBlock.Key), IsReferenceField: false, FactoryUrl: "/api/media/videocomposition/block/factory", ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public List<VideoCompositionBlock> Blocks { get; set; } = new List<VideoCompositionBlock>();

        public VideoCompositionAssemblyState AssemblyState { get; set; } = new VideoCompositionAssemblyState();

        public string VimeoFolderUri { get; set; }

        public string VimeoFolderAssignedUtc { get; set; }

        public string VimeoVideoId { get; set; }

        public string VimeoVideoUri { get; set; }

        public string VimeoVideoUrl { get; set; }

        public string AssemblyRequestStorageReferenceName { get; set; }

        public string AssemblyRequestBlobUrl { get; set; }

        public string AssemblyRequestUrl { get; set; }

        public string AssemblyLaunchProvider { get; set; }

        public string AssemblyLaunchId { get; set; }

        public string AssemblyLaunchNamespace { get; set; }

        public string AssemblyLaunchJobName { get; set; }

        public string AssemblyLaunchedUtc { get; set; }

        public string SubmittedUtc { get; set; }

        public string CompletedUtc { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoComposition_ErrorMessage, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ErrorMessage { get; set; }

        public string CalculateCurrentInputSha256()
        {
            var content = new StringBuilder();

            content.AppendLine("version=4");
            content.AppendLine($"defaultLocale={NormalizeHashValue(DefaultLocale)}");
            content.AppendLine($"title={NormalizeHashValue(Title)}");
            content.AppendLine($"subtitle={NormalizeHashValue(Subtitle)}");
            content.AppendLine($"callToAction={NormalizeHashValue(CallToAction)}");
            content.AppendLine($"backgroundMediaResourceId={NormalizeHashValue(BackgroundMediaResource?.Id)}");
            content.AppendLine($"backgroundAudioMediaResourceId={NormalizeHashValue(BackgroundAudioMediaResource?.Id)}");
            content.AppendLine($"backgroundAudioVolume={NormalizeHashValue(BackgroundAudioVolume)}");
            content.AppendLine($"backgroundAudioFadeInSeconds={NormalizeHashValue(BackgroundAudioFadeInSeconds)}");
            content.AppendLine($"backgroundAudioFadeOutSeconds={NormalizeHashValue(BackgroundAudioFadeOutSeconds)}");
            content.AppendLine($"loopBackgroundAudio={LoopBackgroundAudio}");

            foreach (var block in (Blocks ?? new List<VideoCompositionBlock>()).OrderBy(block => block.SortOrder).ThenBy(block => block.Id, StringComparer.OrdinalIgnoreCase))
            {
                content.AppendLine("block");
                content.AppendLine($"id={NormalizeHashValue(block.Id)}");
                content.AppendLine($"key={NormalizeHashValue(block.Key)}");
                content.AppendLine($"sortOrder={block.SortOrder.ToString(CultureInfo.InvariantCulture)}");
                content.AppendLine($"type={block.Type}");
                content.AppendLine($"role={block.Role}");
                content.AppendLine($"mediaResourceId={NormalizeHashValue(block.MediaResource?.Id)}");
                content.AppendLine($"backgroundMediaResourceId={NormalizeHashValue(block.BackgroundMediaResource?.Id)}");
                content.AppendLine($"backgroundAudioMediaResourceId={NormalizeHashValue(block.BackgroundAudioMediaResource?.Id)}");
                content.AppendLine($"backgroundAudioVolume={NormalizeHashValue(block.BackgroundAudioVolume)}");
                content.AppendLine($"backgroundAudioFadeInSeconds={NormalizeHashValue(block.BackgroundAudioFadeInSeconds)}");
                content.AppendLine($"backgroundAudioFadeOutSeconds={NormalizeHashValue(block.BackgroundAudioFadeOutSeconds)}");
                content.AppendLine($"loopBackgroundAudio={block.LoopBackgroundAudio}");
                content.AppendLine($"presenterScale={NormalizeHashValue(block.PresenterScale)}");
                content.AppendLine($"presenterPositionX={NormalizeHashValue(block.PresenterPositionX)}");
                content.AppendLine($"presenterPositionY={NormalizeHashValue(block.PresenterPositionY)}");
                content.AppendLine($"durationSeconds={NormalizeHashValue(block.DurationSeconds)}");
                content.AppendLine($"fadeInSeconds={NormalizeHashValue(block.FadeInSeconds)}");
                content.AppendLine($"fadeOutSeconds={NormalizeHashValue(block.FadeOutSeconds)}");

                var imageIndex = 0;

                foreach (var image in block.OverlayImages ?? new List<VideoCompositionBlockImage>())
                {
                    content.AppendLine($"image[{imageIndex}]");
                    content.AppendLine($"id={NormalizeHashValue(image.Id)}");
                    content.AppendLine($"mediaResourceId={NormalizeHashValue(image.MediaResource?.Id)}");
                    content.AppendLine($"scale={NormalizeHashValue(image.Scale)}");
                    content.AppendLine($"positionX={NormalizeHashValue(image.PositionX)}");
                    content.AppendLine($"positionY={NormalizeHashValue(image.PositionY)}");
                    content.AppendLine($"opacity={NormalizeHashValue(image.Opacity)}");
                    content.AppendLine($"delaySeconds={NormalizeHashValue(image.DelaySeconds)}");
                    content.AppendLine($"visibleDurationSeconds={NormalizeHashValue(image.VisibleDurationSeconds)}");
                    content.AppendLine($"fadeInSeconds={NormalizeHashValue(image.FadeInSeconds)}");
                    content.AppendLine($"fadeOutSeconds={NormalizeHashValue(image.FadeOutSeconds)}");
                    imageIndex++;
                }

                var labelIndex = 0;

                foreach (var label in block.CompositionLabels ?? new List<VideoCompositionTextLabel>())
                {
                    content.AppendLine($"label[{labelIndex}]");
                    content.AppendLine($"id={NormalizeHashValue(label.Id)}");
                    content.AppendLine($"text={NormalizeHashValue(label.Text)}");
                    content.AppendLine($"binding={label.Binding}");
                    content.AppendLine($"x={label.X.ToString(CultureInfo.InvariantCulture)}");
                    content.AppendLine($"y={label.Y.ToString(CultureInfo.InvariantCulture)}");
                    content.AppendLine($"fontSize={label.FontSize.ToString(CultureInfo.InvariantCulture)}");
                    content.AppendLine($"bold={label.Bold}");
                    content.AppendLine($"color={NormalizeHashValue(label.Color)}");
                    content.AppendLine($"alignment={label.Alignment}");
                    content.AppendLine($"maxWidth={NormalizeHashValue(label.MaxWidth)}");
                    content.AppendLine($"delaySeconds={NormalizeHashValue(label.DelaySeconds)}");
                    content.AppendLine($"visibleDurationSeconds={NormalizeHashValue(label.VisibleDurationSeconds)}");
                    content.AppendLine($"fadeInSeconds={NormalizeHashValue(label.FadeInSeconds)}");
                    content.AppendLine($"fadeOutSeconds={NormalizeHashValue(label.FadeOutSeconds)}");

                    labelIndex++;
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content.ToString());
                var hash = sha256.ComputeHash(bytes);

                return BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
            }
        }

        private static string NormalizeHashValue(string value)
        {
            return value?.Trim() ?? String.Empty;
        }

        private static string NormalizeHashValue(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string NormalizeHashValue(double? value)
        {
            return value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : String.Empty;
        }

        private static string NormalizeHashValue(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : String.Empty;
        }

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(Icon),
                nameof(Description),
                nameof(Title),
                nameof(Subtitle),
                nameof(CallToAction),
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
                nameof(Status),
                nameof(OutputMediaResource),
                nameof(ErrorMessage)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (Status == null)
            {
                result.AddUserError("Video composition status is required.");
            }

            if (BackgroundAudioVolume < 0 || BackgroundAudioVolume > 1)
            {
                result.AddUserError("Video composition background audio volume must be between zero and one.");
            }

            if (BackgroundAudioFadeInSeconds < 0 || BackgroundAudioFadeOutSeconds < 0)
            {
                result.AddUserError("Video composition background audio fade durations cannot be negative.");
            }

            if (Status?.Value != VideoCompositionStatus.Draft && (Blocks == null || Blocks.Count == 0))
            {
                result.AddUserError("At least one video composition block is required.");
                return;
            }

            if (Blocks == null || Blocks.Count == 0)
            {
                return;
            }

            var duplicateKeys = Blocks.Where(block => !String.IsNullOrWhiteSpace(block.Key)).GroupBy(block => block.Key, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            if (duplicateKeys.Count > 0)
            {
                result.AddUserError($"Video composition block keys must be unique. Duplicate keys: {String.Join(", ", duplicateKeys)}.");
            }

            foreach (var block in Blocks)
            {
                block.Validate(result);
            }
        }

        public bool SetStatus(VideoCompositionStatus status)
        {
            if (Status?.Value == status)
            {
                return false;
            }

            Status = EntityHeader<VideoCompositionStatus>.Create(status);
            StatusChangedUtc = UtcTimestamp.Now;

            return true;
        }

        public VideoCompositionSummary CreateSummary()
        {
            var summary = new VideoCompositionSummary
            {
                Status = Status,
                OutputMediaResource = OutputMediaResource,
                BlockCount = Blocks?.Count ?? 0,
                TotalDurationSeconds = CalculateKnownDurationSeconds(),
                VimeoVideoUrl = VimeoVideoUrl,
                SubmittedUtc = SubmittedUtc,
                CompletedUtc = CompletedUtc,
                DefaultLocale = DefaultLocale,
                IsReady = IsReady,
                IsCurrent = IsCurrent,
                StatusChangedUtc = StatusChangedUtc,
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

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoCompositionBlock_Title, MediaServicesResources.Names.VideoCompositionBlock_Help, MediaServicesResources.Names.VideoCompositionBlock_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default", FactoryUrl: "/api/media/videocomposition/block/factory")]
    public sealed class VideoCompositionBlock : IFormDescriptor
    {
        public VideoCompositionBlock()
        {
            Id = Guid.NewGuid().ToId();
            PresenterScale = 1.0;
            PresenterPositionX = 0.5;
            PresenterPositionY = 0.5;
        }

        public string Id { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_Key, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Key { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_SortOrder, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public int SortOrder { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_Type, FieldType: FieldTypes.Picker, EnumType: typeof(VideoCompositionBlockType), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public VideoCompositionBlockType Type { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_Role, FieldType: FieldTypes.Picker, EnumType: typeof(VideoCompositionBlockRole), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public VideoCompositionBlockRole Role { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_MediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader MediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_MediaResourceFileName, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string MediaResourceFileName { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_MediaResourceMimeType, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string MediaResourceMimeType { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_BackgroundMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader BackgroundMediaResource { get; set; }

        public EntityHeader BackgroundAudioMediaResource { get; set; }

        public double BackgroundAudioVolume { get; set; } = 0.20;

        public double BackgroundAudioFadeInSeconds { get; set; }

        public double BackgroundAudioFadeOutSeconds { get; set; }

        public bool LoopBackgroundAudio { get; set; } = true;

        public List<VideoCompositionBlockImage> OverlayImages { get; set; } = new List<VideoCompositionBlockImage>();

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_PresenterScale, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public double PresenterScale { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_PresenterPositionX, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public double PresenterPositionX { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_PresenterPositionY, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public double PresenterPositionY { get; set; }

        public string ThumbnailUrl { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_DurationSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double? DurationSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_FadeInSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double FadeInSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_FadeOutSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double FadeOutSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionBlock_Labels, FieldType: FieldTypes.ChildListInline, ChildListDisplayMembers: "text,fontSize,alignment", ChildListDisplayMember: nameof(VideoCompositionTextLabel.Text), IsReferenceField: false, FactoryUrl: "/api/media/videocomposition/label/factory", ResourceType: typeof(MediaServicesResources), IsUserEditable: true)]
        public List<VideoCompositionTextLabel> CompositionLabels { get; set; } = new List<VideoCompositionTextLabel>();

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Key),
                nameof(SortOrder),
                nameof(Type),
                nameof(Role),
                nameof(MediaResource),
                nameof(MediaResourceFileName),
                nameof(MediaResourceMimeType),
                nameof(BackgroundMediaResource),
                nameof(BackgroundAudioMediaResource),
                nameof(BackgroundAudioVolume),
                nameof(BackgroundAudioFadeInSeconds),
                nameof(BackgroundAudioFadeOutSeconds),
                nameof(LoopBackgroundAudio),
                nameof(OverlayImages),
                nameof(PresenterScale),
                nameof(PresenterPositionX),
                nameof(PresenterPositionY),
                nameof(DurationSeconds),
                nameof(FadeInSeconds),
                nameof(FadeOutSeconds),
                nameof(CompositionLabels)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Id))
            {
                result.AddUserError("Every video composition block must have an ID.");
            }

            if (String.IsNullOrWhiteSpace(Key))
            {
                result.AddUserError("Every video composition block must have a key.");
            }

            if (Role != VideoCompositionBlockRole.Content && (MediaResource == null || String.IsNullOrWhiteSpace(MediaResource.Id)))
            {
                result.AddUserError($"Video composition block '{Key}' must reference a media resource.");
            }

            if (Role == VideoCompositionBlockRole.Content && Type != VideoCompositionBlockType.Video)
            {
                result.AddUserError($"Content block '{Key}' must be a video block.");
            }

            if (FadeInSeconds < 0 || FadeOutSeconds < 0)
            {
                result.AddUserError($"Video composition block '{Key}' cannot have negative fade durations.");
            }

            if (BackgroundAudioVolume < 0 || BackgroundAudioVolume > 1)
            {
                result.AddUserError($"Video composition block '{Key}' background audio volume must be between zero and one.");
            }

            if (BackgroundAudioFadeInSeconds < 0 || BackgroundAudioFadeOutSeconds < 0)
            {
                result.AddUserError($"Video composition block '{Key}' cannot have negative background audio fade durations.");
            }

            foreach (var image in OverlayImages ?? new List<VideoCompositionBlockImage>())
            {
                image.Validate(result, Key);
            }

            if (PresenterScale <= 0)
            {
                result.AddUserError($"Video composition block '{Key}' presenter scale must be greater than zero.");
            }

            if (PresenterPositionX < 0 || PresenterPositionX > 1)
            {
                result.AddUserError($"Video composition block '{Key}' presenter X position must be between zero and one.");
            }

            if (PresenterPositionY < 0 || PresenterPositionY > 1)
            {
                result.AddUserError($"Video composition block '{Key}' presenter Y position must be between zero and one.");
            }

            if (Type == VideoCompositionBlockType.Image && BackgroundMediaResource != null && !String.IsNullOrWhiteSpace(BackgroundMediaResource.Id))
            {
                result.AddUserError($"Image block '{Key}' cannot configure a presenter background override.");
            }

            if (Type == VideoCompositionBlockType.Image && (!DurationSeconds.HasValue || DurationSeconds.Value <= 0))
            {
                result.AddUserError($"Image block '{Key}' must have a duration greater than zero.");
            }

            foreach (var label in CompositionLabels ?? new List<VideoCompositionTextLabel>())
            {
                label.Validate(result, Key);
            }
        }
    }

    public sealed class VideoCompositionBlockImage
    {
        public VideoCompositionBlockImage()
        {
            Id = Guid.NewGuid().ToId();
        }

        public string Id { get; set; }
        public EntityHeader MediaResource { get; set; }
        public double Scale { get; set; } = 0.25;
        public double PositionX { get; set; } = 0.5;
        public double PositionY { get; set; } = 0.5;
        public double Opacity { get; set; } = 1.0;
        public double DelaySeconds { get; set; }
        public double? VisibleDurationSeconds { get; set; }
        public double FadeInSeconds { get; set; }
        public double FadeOutSeconds { get; set; }

        public void Validate(ValidationResult result, string blockKey)
        {
            if (MediaResource == null || String.IsNullOrWhiteSpace(MediaResource.Id))
            {
                result.AddUserError($"An image on block '{blockKey}' must reference a media resource.");
            }

            if (Scale <= 0)
            {
                result.AddUserError($"An image on block '{blockKey}' must have a scale greater than zero.");
            }

            if (PositionX < 0 || PositionX > 1 || PositionY < 0 || PositionY > 1)
            {
                result.AddUserError($"An image on block '{blockKey}' must use positions between zero and one.");
            }

            if (Opacity < 0 || Opacity > 1)
            {
                result.AddUserError($"An image on block '{blockKey}' opacity must be between zero and one.");
            }

            if (DelaySeconds < 0 || FadeInSeconds < 0 || FadeOutSeconds < 0)
            {
                result.AddUserError($"An image on block '{blockKey}' cannot have negative timing values.");
            }

            if (VisibleDurationSeconds.HasValue && VisibleDurationSeconds.Value <= 0)
            {
                result.AddUserError($"An image on block '{blockKey}' must have a visible duration greater than zero.");
            }
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoCompositionTextLabel_Title, MediaServicesResources.Names.VideoCompositionTextLabel_Help, MediaServicesResources.Names.VideoCompositionTextLabel_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default", FactoryUrl: "/api/media/videocomposition/label/factory")]
    public sealed class VideoCompositionTextLabel : IFormDescriptor
    {
        public VideoCompositionTextLabel()
        {
            Id = Guid.NewGuid().ToId();
        }

        public string Id { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_Text, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Text { get; set; }

        public VideoCompositionLabelBinding Binding { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_X, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public int X { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_Y, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public int Y { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_FontSize, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public int FontSize { get; set; } = 48;

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_Bold, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public bool Bold { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_Color, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Color { get; set; } = "#FFFFFF";

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_Alignment, FieldType: FieldTypes.Picker, EnumType: typeof(VideoCompositionTextAlignment), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public VideoCompositionTextAlignment Alignment { get; set; } = VideoCompositionTextAlignment.Left;

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_MaxWidth, FieldType: FieldTypes.Integer, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public int? MaxWidth { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_DelaySeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double DelaySeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_VisibleDurationSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double? VisibleDurationSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_FadeInSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double FadeInSeconds { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoCompositionTextLabel_FadeOutSeconds, FieldType: FieldTypes.Decimal, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public double FadeOutSeconds { get; set; }

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Text),
                nameof(Binding),
                nameof(X),
                nameof(Y),
                nameof(FontSize),
                nameof(Bold),
                nameof(Color),
                nameof(Alignment),
                nameof(MaxWidth),
                nameof(DelaySeconds),
                nameof(VisibleDurationSeconds),
                nameof(FadeInSeconds),
                nameof(FadeOutSeconds)
            };
        }

        public void Validate(ValidationResult result, string blockKey)
        {
            if (String.IsNullOrWhiteSpace(Text))
            {
                result.AddUserError($"A label on block '{blockKey}' does not contain text.");
            }

            if (FontSize <= 0)
            {
                result.AddUserError($"A label on block '{blockKey}' must have a font size greater than zero.");
            }

            if (DelaySeconds < 0 || FadeInSeconds < 0 || FadeOutSeconds < 0)
            {
                result.AddUserError($"A label on block '{blockKey}' cannot have negative timing values.");
            }

            if (VisibleDurationSeconds.HasValue && VisibleDurationSeconds.Value <= 0)
            {
                result.AddUserError($"A label on block '{blockKey}' must have a visible duration greater than zero.");
            }

            if (String.IsNullOrWhiteSpace(Color) || Color.Length != 7 || Color[0] != '#')
            {
                result.AddUserError($"A label on block '{blockKey}' must use a six-digit hexadecimal color such as #FFFFFF.");
            }
        }
    }

    public sealed class VideoCompositionAssemblyState
    {
        public string RequestId { get; set; }

        public string AttemptId { get; set; }

        public string ContractVersion { get; set; }

        public VideoCompositionAssemblyStage Stage { get; set; }

        public int? PercentComplete { get; set; }

        public string Message { get; set; }

        public long LastSequence { get; set; }

        public long? BytesCompleted { get; set; }

        public long? BytesTotal { get; set; }

        public int? ProcessedDurationSeconds { get; set; }

        public int? TotalDurationSeconds { get; set; }

        public long? OutputSizeBytes { get; set; }

        public int? OutputDurationSeconds { get; set; }

        public string OutputSha256 { get; set; }

        public string StartedUtc { get; set; }

        public string LastUpdatedUtc { get; set; }

        public string CompletedUtc { get; set; }

        public string ErrorMessage { get; set; }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoCompositions_Title, MediaServicesResources.Names.VideoComposition_Help, MediaServicesResources.Names.VideoComposition_Description,
        EntityDescriptionAttribute.EntityTypes.Dto, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default",
        ListUIUrl: "/contentmanagement/videocompositions", EditUIUrl: "/contentmanagement/videocomposition/{id}", CreateUIUrl: "/contentmanagement/videocomposition/add",
        PreviewUIUrl: "/contentmanagement/videocomposition/{id}",
        GetListUrl: "/api/media/videocompositions", SaveUrl: "/api/media/videocomposition", GetUrl: "/api/media/videocomposition/{id}", FactoryUrl: "/api/media/videocomposition/factory", DeleteUrl: "/api/media/videocomposition/{id}")]
    public class VideoCompositionSummary : SummaryData
    {
        public EntityHeader<VideoCompositionStatus> Status { get; set; }

        public EntityHeader OutputMediaResource { get; set; }

        public int BlockCount { get; set; }

        public string DefaultLocale { get; set; }

        public bool IsReady { get; set; }

        public bool IsCurrent { get; set; }

        public string StatusChangedUtc { get; set; }

        public int? TotalDurationSeconds { get; set; }

        public string VimeoVideoUrl { get; set; }

        public string SubmittedUtc { get; set; }

        public string CompletedUtc { get; set; }
    }
}
