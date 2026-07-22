using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public enum VideoProductionStatus
    {
        [EnumLabel(VideoProduction.Status_Draft, MediaServicesResources.Names.VideoProductionStatus_Draft, typeof(MediaServicesResources))]
        Draft,

        [EnumLabel(VideoProduction.Status_PreparingAvatar, MediaServicesResources.Names.VideoProductionStatus_PreparingAvatar, typeof(MediaServicesResources))]
        PreparingAvatar,

        [EnumLabel(VideoProduction.Status_WaitingForAvatar, MediaServicesResources.Names.VideoProductionStatus_WaitingForAvatar, typeof(MediaServicesResources))]
        WaitingForAvatar,

        [EnumLabel(VideoProduction.Status_GeneratingPreviewAudio, MediaServicesResources.Names.VideoProductionStatus_GeneratingPreviewAudio, typeof(MediaServicesResources))]
        GeneratingPreviewAudio,

        [EnumLabel(VideoProduction.Status_PreviewAudioReady, MediaServicesResources.Names.VideoProductionStatus_PreviewAudioReady, typeof(MediaServicesResources))]
        PreviewAudioReady,

        [EnumLabel(VideoProduction.Status_UploadingBackground, MediaServicesResources.Names.VideoProductionStatus_UploadingBackground, typeof(MediaServicesResources))]
        UploadingBackground,

        [EnumLabel(VideoProduction.Status_Submitting, MediaServicesResources.Names.VideoProductionStatus_Submitting, typeof(MediaServicesResources))]
        Submitting,

        [EnumLabel(VideoProduction.Status_Submitted, MediaServicesResources.Names.VideoProductionStatus_Submitted, typeof(MediaServicesResources))]
        Submitted,

        [EnumLabel(VideoProduction.Status_Rendering, MediaServicesResources.Names.VideoProductionStatus_Rendering, typeof(MediaServicesResources))]
        Rendering,

        [EnumLabel(VideoProduction.Status_ProviderCompleted, MediaServicesResources.Names.VideoProductionStatus_ProviderCompleted, typeof(MediaServicesResources))]
        ProviderCompleted,

        [EnumLabel(VideoProduction.Status_ImportingProviderVideo, MediaServicesResources.Names.VideoProductionStatus_ImportingProviderVideo, typeof(MediaServicesResources))]
        ImportingProviderVideo,

        [EnumLabel(VideoProduction.Status_ProviderVideoReady, MediaServicesResources.Names.VideoProductionStatus_ProviderVideoReady, typeof(MediaServicesResources))]
        ProviderVideoReady,

        [EnumLabel(VideoProduction.Status_ImportingToVimeo, MediaServicesResources.Names.VideoProductionStatus_ImportingToVimeo, typeof(MediaServicesResources))]
        ImportingToVimeo,

        [EnumLabel(VideoProduction.Status_ProcessingAtVimeo, MediaServicesResources.Names.VideoProductionStatus_ProcessingAtVimeo, typeof(MediaServicesResources))]
        ProcessingAtVimeo,

        [EnumLabel(VideoProduction.Status_UpdatingEntity, MediaServicesResources.Names.VideoProductionStatus_UpdatingEntity, typeof(MediaServicesResources))]
        UpdatingEntity,

        [EnumLabel(VideoProduction.Status_Completed, MediaServicesResources.Names.VideoProductionStatus_Completed, typeof(MediaServicesResources))]
        Completed,

        [EnumLabel(VideoProduction.Status_Failed, MediaServicesResources.Names.VideoProductionStatus_Failed, typeof(MediaServicesResources))]
        Failed,

        [EnumLabel(VideoProduction.Status_Cancelled, MediaServicesResources.Names.VideoProductionStatus_Cancelled, typeof(MediaServicesResources))]
        Cancelled
    }

    public enum VideoProductionProvider
    {
        [EnumLabel(VideoProduction.Provider_HeyGen, MediaServicesResources.Names.VideoProductionProvider_HeyGen, typeof(MediaServicesResources))]
        HeyGen
    }

    public enum VideoProductionQuality
    {
        [EnumLabel(VideoProduction.Quality_Standard, MediaServicesResources.Names.VideoProductionProvider_QualityStandard, typeof(MediaServicesResources))]
        Standard,
        [EnumLabel(VideoProduction.Quality_Premium, MediaServicesResources.Names.VideoProductionProvider_QualityPremium, typeof(MediaServicesResources))]
        Premium
    }

    public enum VideoProductionEngine
    {
        [EnumLabel(VideoProduction.Engine_AvatarIII, MediaServicesResources.Names.VideoProductionEngine_AvatarIII, typeof(MediaServicesResources))]
        AvatarIII,

        [EnumLabel(VideoProduction.Engine_AvatarIV, MediaServicesResources.Names.VideoProductionEngine_AvatarIV, typeof(MediaServicesResources))]
        AvatarIV,

        [EnumLabel(VideoProduction.Engine_AvatarV, MediaServicesResources.Names.VideoProductionEngine_AvatarV, typeof(MediaServicesResources))]
        AvatarV
    }

    public enum VideoProductionResolution
    {
        [EnumLabel(VideoProduction.Resolution_HD720, MediaServicesResources.Names.VideoProductionResolution_HD720, typeof(MediaServicesResources))]
        HD720,

        [EnumLabel(VideoProduction.Resolution_FullHD1080, MediaServicesResources.Names.VideoProductionResolution_FullHD1080, typeof(MediaServicesResources))]
        FullHD1080,

        [EnumLabel(VideoProduction.Resolution_UHD4K, MediaServicesResources.Names.VideoProductionResolution_UHD4K, typeof(MediaServicesResources))]
        UHD4K
    }

    public enum VideoProductionAspectRatio
    {
        [EnumLabel(VideoProduction.AspectRatio_Landscape16x9, MediaServicesResources.Names.VideoProductionAspectRatio_Landscape16x9, typeof(MediaServicesResources))]
        Landscape16x9,

        [EnumLabel(VideoProduction.AspectRatio_Portrait9x16, MediaServicesResources.Names.VideoProductionAspectRatio_Portrait9x16, typeof(MediaServicesResources))]
        Portrait9x16,

        [EnumLabel(VideoProduction.AspectRatio_Portrait4x5, MediaServicesResources.Names.VideoProductionAspectRatio_Portrait4x5, typeof(MediaServicesResources))]
        Portrait4x5,

        [EnumLabel(VideoProduction.AspectRatio_Landscape5x4, MediaServicesResources.Names.VideoProductionAspectRatio_Landscape5x4, typeof(MediaServicesResources))]
        Landscape5x4,

        [EnumLabel(VideoProduction.AspectRatio_Square1x1, MediaServicesResources.Names.VideoProductionAspectRatio_Square1x1, typeof(MediaServicesResources))]
        Square1x1,

        [EnumLabel(VideoProduction.AspectRatio_Auto, MediaServicesResources.Names.VideoProductionAspectRatio_Auto, typeof(MediaServicesResources))]
        Auto
    }

    public enum VideoProductionFit
    {
        [EnumLabel(VideoProduction.Fit_Automatic, MediaServicesResources.Names.VideoProductionFit_Automatic, typeof(MediaServicesResources))]
        Automatic,

        [EnumLabel(VideoProduction.Fit_Contain, MediaServicesResources.Names.VideoProductionFit_Contain, typeof(MediaServicesResources))]
        Contain,

        [EnumLabel(VideoProduction.Fit_Cover, MediaServicesResources.Names.VideoProductionFit_Cover, typeof(MediaServicesResources))]
        Cover
    }

    public enum VideoProductionExpressiveness
    {
        [EnumLabel(VideoProduction.Expressiveness_Low, MediaServicesResources.Names.VideoProductionExpressiveness_Low, typeof(MediaServicesResources))]
        Low,

        [EnumLabel(VideoProduction.Expressiveness_Medium, MediaServicesResources.Names.VideoProductionExpressiveness_Medium, typeof(MediaServicesResources))]
        Medium,

        [EnumLabel(VideoProduction.Expressiveness_High, MediaServicesResources.Names.VideoProductionExpressiveness_High, typeof(MediaServicesResources))]
        High
    }

    public enum VideoProductionOutputMode
    {
        [EnumLabel(VideoProduction.OutputMode_TransparentPresenter, MediaServicesResources.Names.VideoProductionOutputMode_TransparentPresenter, typeof(MediaServicesResources))]
        TransparentPresenter,

        [EnumLabel(VideoProduction.OutputMode_CompositedBackground, MediaServicesResources.Names.VideoProductionOutputMode_CompositedBackground, typeof(MediaServicesResources))]
        CompositedBackground
    }

    public sealed class VideoProductionRun
    {
        public VideoProductionRun()
        {
            Id = Guid.NewGuid().ToId();
            Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Draft);
        }

        public string Id { get; set; }
        public string ProviderVideoId { get; set; }
        public EntityHeader<VideoProductionStatus> Status { get; set; }
        public string InputSha256 { get; set; }
        public string SubmittedUtc { get; set; }
        public string CompletedUtc { get; set; }
        public string LastStatusCheckUtc { get; set; }
        public int? DurationSeconds { get; set; }
        public decimal? ActualVideoGenerationCost { get; set; }
        public string BillingEventId { get; set; }
        public string ProviderVideoUrl { get; set; }
        public string ProviderThumbnailUrl { get; set; }
        public string ProviderCaptionUrl { get; set; }
        public EntityHeader OutputMediaResource { get; set; }
        public string ErrorMessage { get; set; }
    }

    public sealed class VideoProductionSettings
    {
        public VideoProductionSettings()
        {
            OutputMode = EntityHeader<VideoProductionOutputMode>.Create(VideoProductionOutputMode.TransparentPresenter);
            Resolution = EntityHeader<VideoProductionResolution>.Create(VideoProductionResolution.FullHD1080);
            AspectRatio = EntityHeader<VideoProductionAspectRatio>.Create(VideoProductionAspectRatio.Landscape16x9);
            Fit = EntityHeader<VideoProductionFit>.Create(VideoProductionFit.Automatic);
            Expressiveness = EntityHeader<VideoProductionExpressiveness>.Create(VideoProductionExpressiveness.Low);
            RemoveBackground = true;
            GenerateTransparentPresenter = true;
            CaptionStyle = "default";
        }

        public EntityHeader<VideoProductionOutputMode> OutputMode { get; set; }

        public string SpecialInstructions { get; set; }

        public decimal? VoiceSpeed { get; set; }

        public decimal? VoicePitch { get; set; }

        public decimal? VoiceVolume { get; set; }

        public EntityHeader<VideoProductionResolution> Resolution { get; set; }

        public EntityHeader<VideoProductionAspectRatio> AspectRatio { get; set; }

        public EntityHeader<VideoProductionFit> Fit { get; set; }

        public bool RemoveBackground { get; set; }

        public bool GenerateTransparentPresenter { get; set; }

        public bool BurnInCaptions { get; set; }

        public string CaptionStyle { get; set; }

        public string MotionPrompt { get; set; }

        public EntityHeader<VideoProductionExpressiveness> Expressiveness { get; set; }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoProduction_Title, MediaServicesResources.Names.VideoProduction_Help, MediaServicesResources.Names.VideoProduction_Description, EntityDescriptionAttribute.EntityTypes.CoreIoTModel, typeof(MediaServicesResources),
        GetUrl: "/api/media/videoproduction/{id}", GetListUrl: "/api/media/videoproductions", FactoryUrl: "/api/media/videoproduction/factory", SaveUrl: "/api/media/videoproduction", DeleteUrl: "/api/media/videoproduction/{id}",
        ListUIUrl: "/contentmanagement/videoproductions", EditUIUrl: "/contentmanagement/videoproduction/{id}", CreateUIUrl: "/contentmanagement/videoproduction/add",
        AiIconGuidance: "Represent a Video Production as a structured AI-assisted video composition or rendering job. Use a clean video frame, timeline, play surface, or production card as the dominant metaphor, optionally with one simple digital accent such as a spark, layer, progress mark, signal arc, or assistant indicator. The icon should feel like a managed production workflow that combines script, avatar, background, preview audio, rendered output, and delivery status. Avoid generic media-player icons, clapperboards with excessive detail, film reels, cartoon cameras, robot heads, chat bubbles, or overly decorative effects. Keep the shape simple, centered, and readable at small sizes. For specific instance icons, preserve this same visual idea while adapting the meaning of the individual production or output.",
        Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default", ClusterKey: "video", ModelType: EntityDescriptionAttribute.ModelTypes.Document, Shape: EntityDescriptionAttribute.EntityShapes.Entity, Lifecycle: EntityDescriptionAttribute.Lifecycles.RunTime,
        Sensitivity: EntityDescriptionAttribute.Sensitivities.Internal, IndexInclude: true, IndexTier: EntityDescriptionAttribute.IndexTiers.Primary, IndexPriority: 80, IndexTagsCsv: "media,video,production,heygen,vimeo")]
    public class VideoProduction : EntityBase, IValidateable, IFormDescriptor, IFormDescriptorCol2, ISummaryFactory
    {
        public const string Provider_HeyGen = "heygen";

        public const string Quality_Standard = "standard";
        public const string Quality_Premium = "premium";

        public const string Engine_AvatarIII = "avatar-iii";
        public const string Engine_AvatarIV = "avatar-iv";
        public const string Engine_AvatarV = "avatar-v";

        public const string Resolution_HD720 = "hd-720";
        public const string Resolution_FullHD1080 = "full-hd-1080";
        public const string Resolution_UHD4K = "uhd-4k";

        public const string AspectRatio_Landscape16x9 = "landscape-16x9";
        public const string AspectRatio_Portrait9x16 = "portrait-9x16";
        public const string AspectRatio_Portrait4x5 = "portrait-4x5";
        public const string AspectRatio_Landscape5x4 = "landscape-5x4";
        public const string AspectRatio_Square1x1 = "square-1x1";
        public const string AspectRatio_Auto = "auto";

        public const string Fit_Automatic = "automatic";
        public const string Fit_Contain = "contain";
        public const string Fit_Cover = "cover";

        public const string Expressiveness_Low = "low";
        public const string Expressiveness_Medium = "medium";
        public const string Expressiveness_High = "high";

        public const string OutputMode_TransparentPresenter = "transparent-presenter";
        public const string OutputMode_CompositedBackground = "composited-background";

        public const string Status_Draft = "draft";
        public const string Status_PreparingAvatar = "preparing-avatar";
        public const string Status_WaitingForAvatar = "waiting-for-avatar";
        public const string Status_GeneratingPreviewAudio = "generating-preview-audio";
        public const string Status_PreviewAudioReady = "preview-audio-ready";
        public const string Status_UploadingBackground = "uploading-background";
        public const string Status_Submitting = "submitting";
        public const string Status_Submitted = "submitted";
        public const string Status_Rendering = "rendering";
        public const string Status_ProviderCompleted = "provider-completed";
        public const string Status_ImportingProviderVideo = "importing-provider-video";
        public const string Status_ProviderVideoReady = "provider-video-ready";
        public const string Status_ImportingToVimeo = "importing-to-vimeo";
        public const string Status_ProcessingAtVimeo = "processing-at-vimeo";
        public const string Status_UpdatingEntity = "updating-entity";
        public const string Status_Completed = "completed";
        public const string Status_Failed = "failed";
        public const string Status_Cancelled = "cancelled";

        public const string DefaultLocaleCode = "en-US";

        public VideoProduction()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/video-production-default";
            Provider = EntityHeader<VideoProductionProvider>.Create(VideoProductionProvider.HeyGen);
            Quality = EntityHeader<VideoProductionQuality>.Create(VideoProductionQuality.Standard);
            Engine = EntityHeader<VideoProductionEngine>.Create(VideoProductionEngine.AvatarIV);
            Settings = new VideoProductionSettings();
            Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Draft);
            StatusChangedUtc = UtcTimestamp.Now;
            DefaultLocale = DefaultLocaleCode;
            CostCurrency = "USD";
        }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_VideoAvatar, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public EntityHeader VideoAvatar { get; set; }

        public string VideoAvatarLookId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_BackgroundMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public EntityHeader BackgroundMediaResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_PreviewAudioMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public EntityHeader PreviewAudioMediaResource { get; set; }

        public string PreviewAudioScriptSha256 { get; set; }

        public string PreviewAudioGeneratedUtc { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_FinalVideoMediaResource, FieldType: FieldTypes.EntityHeaderPicker, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public EntityHeader FinalVideoMediaResource { get; set; }

        public EntityHeader OutputMediaLibrary { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_Script, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Script { get; set; }

        public string VoiceBindingId { get; set; }


        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_VideoName, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string VideoName { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_VoiceId, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string VoiceId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_VoiceName, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string VoiceName { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_LanguageCode, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string LanguageCode { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_Locale, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string Locale { get; set; }

        public string DefaultLocale { get; set; }

        public bool IsReady { get; set; }

        public string CurrentInputSha256 { get; set; }

        public string ExecutionInputSha256 { get; set; }

        public string OutputInputSha256 { get; set; }

        public bool IsCurrent =>
            IsReady &&
            !String.IsNullOrWhiteSpace(CurrentInputSha256) &&
            !String.IsNullOrWhiteSpace(OutputInputSha256) &&
            String.Equals(CurrentInputSha256, OutputInputSha256, StringComparison.OrdinalIgnoreCase);


        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_TargetEntityType, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string TargetEntityType { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_TargetEntityId, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string TargetEntityId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_TargetEntityName, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string TargetEntityName { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_TargetEntityProperty, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public string TargetEntityProperty { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_Provider, FieldType: FieldTypes.Picker, EnumType: typeof(VideoProductionProvider), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public EntityHeader<VideoProductionProvider> Provider { get; set; }

        public EntityHeader<VideoProductionQuality> Quality { get; set; }

        public EntityHeader<VideoProductionEngine> Engine { get; set; }

        public VideoProductionSettings Settings { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_Status, FieldType: FieldTypes.Picker, EnumType: typeof(VideoProductionStatus), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: false)]
        public EntityHeader<VideoProductionStatus> Status { get; set; }

        public string ProviderAvatarId { get; set; }
        public string ProviderBackgroundAssetId { get; set; }
        public string ProviderVideoId { get; set; }
        public string ProviderVideoUrl { get; set; }
        public string ProviderThumbnailUrl { get; set; }
        public string ProviderCaptionUrl { get; set; }

        public string VimeoFolderUri { get; set; }
        public string VimeoFolderAssignedUtc { get; set; }
        public string VimeoVideoId { get; set; }
        public string VimeoVideoUri { get; set; }
        public string VimeoVideoUrl { get; set; }

        public int? EstimatedDurationSeconds { get; set; }
        public int? ActualDurationSeconds { get; set; }

        public decimal? EstimatedPreviewAudioCost { get; set; }
        public decimal? ActualPreviewAudioCost { get; set; }
        public int PreviewAudioGenerationCount { get; set; }
        public decimal? TotalPreviewAudioCost { get; set; }

        public decimal? EstimatedAvatarCreationCost { get; set; }
        public decimal? ActualAvatarCreationCost { get; set; }

        public decimal? EstimatedVideoGenerationCost { get; set; }
        public decimal? ActualVideoGenerationCost { get; set; }
        public int VideoGenerationCount { get; set; }
        public decimal? TotalVideoGenerationCost { get; set; }

        public decimal? EstimatedTotalCost { get; set; }
        public decimal? ActualTotalCost { get; set; }
        public decimal? TotalProductionCost { get; set; }

        public string CostCurrency { get; set; }
        public string CostModelVersion { get; set; }

        public string PreviewAudioBillingEventId { get; set; }
        public string AvatarCreationBillingEventId { get; set; }
        public string VideoGenerationBillingEventId { get; set; }

        public List<VideoProductionRun> Runs { get; set; } = new List<VideoProductionRun>();

        public string SubmittedUtc { get; set; }
        public string CompletedUtc { get; set; }
        public string LastStatusCheckUtc { get; set; }
        public string StatusChangedUtc { get; set; }
        public string ProviderVideoImportRequestId { get; set; }
        public string ProviderVideoImportAttemptId { get; set; }
        public string ProviderVideoImportRequestStorageReferenceName { get; set; }
        public string ProviderVideoImportRequestBlobUrl { get; set; }
        public string ProviderVideoImportRequestUrl { get; set; }
        public string ProviderVideoImportLaunchProvider { get; set; }
        public string ProviderVideoImportLaunchId { get; set; }
        public string ProviderVideoImportLaunchNamespace { get; set; }
        public string ProviderVideoImportLaunchJobName { get; set; }
        public string ProviderVideoImportLaunchedUtc { get; set; }
        public string ProviderVideoImportStage { get; set; }
        public string ProviderVideoImportMessage { get; set; }
        public int? ProviderVideoImportPercentComplete { get; set; }
        public long? ProviderVideoImportBytesCompleted { get; set; }
        public long? ProviderVideoImportBytesTotal { get; set; }
        public string ProviderVideoImportStartedUtc { get; set; }
        public string ProviderVideoImportLastUpdatedUtc { get; set; }
        public string ProviderVideoImportCompletedUtc { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoProduction_ErrorMessage, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ErrorMessage { get; set; }

        public string CalculateCurrentInputSha256()
        {
            var content = String.Join("\n", new[]
            {
        $"version=4",
        $"defaultLocale={NormalizeHashValue(DefaultLocale)}",
        $"locale={NormalizeHashValue(Locale)}",
        $"languageCode={NormalizeHashValue(LanguageCode)}",
        $"script={NormalizeHashValue(Script)}",
        $"videoName={NormalizeHashValue(VideoName)}",
        $"voiceBindingId={NormalizeHashValue(VoiceBindingId)}",
        $"voiceId={NormalizeHashValue(VoiceId)}",
        $"videoAvatarId={NormalizeHashValue(VideoAvatar?.Id)}",
        $"videoAvatarLookId={NormalizeHashValue(VideoAvatarLookId)}",
        $"providerAvatarId={NormalizeHashValue(ProviderAvatarId)}",
        $"backgroundMediaResourceId={NormalizeHashValue(BackgroundMediaResource?.Id)}",
        $"provider={NormalizeHashValue(Provider?.Id)}",
        $"quality={NormalizeHashValue(Quality?.Id)}",
        $"engine={NormalizeHashValue(Engine?.Id)}",
        $"specialInstructions={NormalizeHashValue(Settings?.SpecialInstructions)}",
        $"voiceSpeed={NormalizeHashValue(Settings?.VoiceSpeed)}",
        $"voicePitch={NormalizeHashValue(Settings?.VoicePitch)}",
        $"voiceVolume={NormalizeHashValue(Settings?.VoiceVolume)}",
        $"resolution={NormalizeHashValue(Settings?.Resolution?.Id)}",
        $"aspectRatio={NormalizeHashValue(Settings?.AspectRatio?.Id)}",
        $"fit={NormalizeHashValue(Settings?.Fit?.Id)}",
        $"outputMode={NormalizeHashValue(Settings?.OutputMode?.Id)}",
        $"removeBackground={Settings?.RemoveBackground ?? false}",
        $"generateTransparentPresenter={Settings?.GenerateTransparentPresenter ?? false}",
        $"burnInCaptions={Settings?.BurnInCaptions ?? false}",
        $"captionStyle={NormalizeHashValue(Settings?.CaptionStyle)}",
        $"motionPrompt={NormalizeHashValue(Settings?.MotionPrompt)}",
        $"expressiveness={NormalizeHashValue(Settings?.Expressiveness?.Id)}"
    });

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = sha256.ComputeHash(bytes);

                return BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
            }
        }

        private static string NormalizeHashValue(string value)
        {
            return value?.Trim() ?? String.Empty;
        }

        private static string NormalizeHashValue(decimal? value)
        {
            return value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : String.Empty;
        }

        public bool SetStatus(VideoProductionStatus status)
        {
            if (Status?.Value == status)
            {
                return false;
            }

            Status = EntityHeader<VideoProductionStatus>.Create(status);
            StatusChangedUtc = UtcTimestamp.Now;

            return true;
        }

        public VideoProductionSummary CreateSummary()
        {
            var summary = new VideoProductionSummary
            {
                VideoAvatar = VideoAvatar,
                BackgroundMediaResource = BackgroundMediaResource,
                PreviewAudioMediaResource = PreviewAudioMediaResource,
                FinalVideoMediaResource = FinalVideoMediaResource,
                VideoName = VideoName,
                TargetEntityType = TargetEntityType,
                TargetEntityId = TargetEntityId,
                TargetEntityName = TargetEntityName,
                TargetEntityProperty = TargetEntityProperty,
                Provider = Provider,
                Quality = Quality,
                ProviderVideoId = ProviderVideoId,
                VimeoVideoUrl = VimeoVideoUrl,
                Status = Status,
                DefaultLocale = DefaultLocale,
                IsReady = IsReady,
                IsCurrent = IsCurrent,
                StatusChangedUtc = StatusChangedUtc,
                EstimatedTotalCost = EstimatedTotalCost,
                ActualTotalCost = ActualTotalCost,
                TotalProductionCost = TotalProductionCost,
                CostCurrency = CostCurrency,
                SubmittedUtc = SubmittedUtc,
                CompletedUtc = CompletedUtc
            };

            summary.Populate(this);

            return summary;
        }

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(Icon),
                nameof(Description),
                nameof(VideoName),
                nameof(VideoAvatar),
                nameof(BackgroundMediaResource),
                nameof(OutputMediaLibrary),
                nameof(Script),
                nameof(TargetEntityType),
                nameof(TargetEntityId),
                nameof(TargetEntityName),
                nameof(TargetEntityProperty)
            };
        }

        public List<string> GetFormFieldsCol2()
        {
            return new List<string>()
            {
                nameof(Provider),
                nameof(Quality),
                nameof(Status),
                nameof(VoiceId),
                nameof(VoiceName),
                nameof(LanguageCode),
                nameof(Locale),
                nameof(PreviewAudioMediaResource),
                nameof(FinalVideoMediaResource),
                nameof(ErrorMessage)
            };
        }

        public void Validate(ValidationResult result)
        {
            if (VideoAvatar == null || string.IsNullOrWhiteSpace(VideoAvatar.Id))
            {
                result.AddUserError("Video avatar is required.");
            }

            if (string.IsNullOrWhiteSpace(Script))
            {
                result.AddUserError("Script is required.");
            }

            if (string.IsNullOrWhiteSpace(VideoName))
            {
                result.AddUserError("Video name is required.");
            }

            if (Provider == null)
            {
                result.AddUserError("Provider is required.");
            }

            if (Quality == null)
            {
                result.AddUserError("Video production quality is required.");
            }

            if (Engine == null)
            {
                result.AddUserError("Video production engine is required.");
            }

            if (Settings == null)
            {
                result.AddUserError("Video production settings are required.");
            }
            else
            {
                if (Settings.OutputMode == null)
                {
                    result.AddUserError("Video production output mode is required.");
                }

                if (Settings.Resolution == null)
                {
                    result.AddUserError("Video resolution is required.");
                }

                if (Settings.AspectRatio == null)
                {
                    result.AddUserError("Video aspect ratio is required.");
                }

                if (Settings.Fit == null)
                {
                    result.AddUserError("Video fit is required.");
                }

                if (Settings.VoiceSpeed.HasValue && (Settings.VoiceSpeed.Value < 0.5m || Settings.VoiceSpeed.Value > 2.0m))
                {
                    result.AddUserError("Voice speed must be between 0.5 and 2.0.");
                }

                if (Settings.VoicePitch.HasValue && (Settings.VoicePitch.Value < -50m || Settings.VoicePitch.Value > 50m))
                {
                    result.AddUserError("Voice pitch must be between -50 and 50.");
                }

                if (Settings.VoiceVolume.HasValue && Settings.VoiceVolume.Value <= 0)
                {
                    result.AddUserError("Voice volume must be greater than zero.");
                }

                if (Settings.Expressiveness != null && Engine?.Value != VideoProductionEngine.AvatarIV)
                {
                    result.AddUserError("Expressiveness is only supported by the Avatar IV engine.");
                }

                if (!Settings.RemoveBackground)
                {
                    result.AddUserError("Avatar background removal is required for supported video production output modes.");
                }

                if (Settings.OutputMode?.Value == VideoProductionOutputMode.TransparentPresenter)
                {
                    if (!Settings.GenerateTransparentPresenter)
                    {
                        result.AddUserError("Transparent presenter output must generate a transparent presenter video.");
                    }

                    if (BackgroundMediaResource != null && !String.IsNullOrWhiteSpace(BackgroundMediaResource.Id))
                    {
                        result.AddUserError("Transparent presenter output cannot include a background Media Resource.");
                    }
                }

                if (Settings.OutputMode?.Value == VideoProductionOutputMode.CompositedBackground)
                {
                    if (Settings.GenerateTransparentPresenter)
                    {
                        result.AddUserError("Video-with-background output cannot generate a transparent presenter video.");
                    }

                    if (BackgroundMediaResource == null || String.IsNullOrWhiteSpace(BackgroundMediaResource.Id))
                    {
                        result.AddUserError("A background Media Resource is required for video-with-background output.");
                    }
                }

                if (!String.IsNullOrWhiteSpace(Settings.SpecialInstructions) && Settings.SpecialInstructions.Length > 1000)
                {
                    result.AddUserError("Special instructions cannot exceed 1000 characters.");
                }

                if (!String.IsNullOrWhiteSpace(Settings.MotionPrompt) && Settings.MotionPrompt.Length > 1000)
                {
                    result.AddUserError("Motion prompt cannot exceed 1000 characters.");
                }

                if (!String.IsNullOrWhiteSpace(Settings.CaptionStyle) && Settings.CaptionStyle.Length > 100)
                {
                    result.AddUserError("Caption style cannot exceed 100 characters.");
                }
            }

            if (Status == null)
            {
                result.AddUserError("Status is required.");
            }

            if (string.IsNullOrWhiteSpace(VoiceId))
            {
                result.AddUserError("Voice ID is required.");
            }

            if (string.IsNullOrWhiteSpace(TargetEntityType) || string.IsNullOrWhiteSpace(TargetEntityId) || string.IsNullOrWhiteSpace(TargetEntityProperty))
            {
                result.AddUserError("Target entity type, ID, and property are required.");
            }
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoProduction_Title, MediaServicesResources.Names.VideoProduction_Help, MediaServicesResources.Names.VideoProduction_Description, EntityDescriptionAttribute.EntityTypes.Summary, typeof(MediaServicesResources),
        GetUrl: "/api/media/videoproduction/{id}", GetListUrl: "/api/media/videoproductions", FactoryUrl: "/api/media/videoproduction/factory", SaveUrl: "/api/media/videoproduction", DeleteUrl: "/api/media/videoproduction/{id}",
        ListUIUrl: "/contentmanagement/videoproductions", EditUIUrl: "/contentmanagement/videoproduction/{id}", CreateUIUrl: "/contentmanagement/videoproduction/add", Icon: "icon-fo-video")]
    public class VideoProductionSummary : SummaryData
    {
        public EntityHeader VideoAvatar { get; set; }
        public EntityHeader BackgroundMediaResource { get; set; }
        public EntityHeader PreviewAudioMediaResource { get; set; }
        public EntityHeader FinalVideoMediaResource { get; set; }

        public string DefaultLocale { get; set; }
        public bool IsReady { get; set; }
        public bool IsCurrent { get; set; }
        public string StatusChangedUtc { get; set; }

        public string VideoName { get; set; }

        public string TargetEntityType { get; set; }
        public string TargetEntityId { get; set; }
        public string TargetEntityName { get; set; }
        public string TargetEntityProperty { get; set; }

        public EntityHeader<VideoProductionProvider> Provider { get; set; }
        public EntityHeader<VideoProductionQuality> Quality { get; set; }
        public string ProviderVideoId { get; set; }
        public string VimeoVideoUrl { get; set; }

        public EntityHeader<VideoProductionStatus> Status { get; set; }

        public decimal? EstimatedTotalCost { get; set; }
        public decimal? ActualTotalCost { get; set; }
        public decimal? TotalProductionCost { get; set; }
        public string CostCurrency { get; set; }

        public string SubmittedUtc { get; set; }
        public string CompletedUtc { get; set; }
    }
}