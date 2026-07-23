using LagoVista.Core;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.MediaServices.Models
{
    public enum VideoAvatarProvider
    {
        [EnumLabel(VideoAvatar.Provider_HeyGen, MediaServicesResources.Names.VideoAvatarProvider_HeyGen, typeof(MediaServicesResources))]
        HeyGen
    }

    public enum VideoAvatarRole
    {
        [EnumLabel(VideoAvatar.Role_Primary, MediaServicesResources.Names.VideoAvatarRole_Primary, typeof(MediaServicesResources))]
        Primary,

        [EnumLabel(VideoAvatar.Role_Editorial, MediaServicesResources.Names.VideoAvatarRole_Editorial, typeof(MediaServicesResources))]
        Editorial,

        [EnumLabel(VideoAvatar.Role_Campaign, MediaServicesResources.Names.VideoAvatarRole_Campaign, typeof(MediaServicesResources))]
        Campaign,

        [EnumLabel(VideoAvatar.Role_Experimental, MediaServicesResources.Names.VideoAvatarRole_Experimental, typeof(MediaServicesResources))]
        Experimental
    }

    public enum VideoAvatarStatus
    {
        [EnumLabel(VideoAvatar.Status_Draft, MediaServicesResources.Names.VideoAvatarStatus_Draft, typeof(MediaServicesResources))]
        Draft,

        [EnumLabel(VideoAvatar.Status_Preparing, MediaServicesResources.Names.VideoAvatarStatus_Preparing, typeof(MediaServicesResources))]
        Preparing,

        [EnumLabel(VideoAvatar.Status_WaitingForProvider, MediaServicesResources.Names.VideoAvatarStatus_WaitingForProvider, typeof(MediaServicesResources))]
        WaitingForProvider,

        [EnumLabel(VideoAvatar.Status_Ready, MediaServicesResources.Names.VideoAvatarStatus_Ready, typeof(MediaServicesResources))]
        Ready,

        [EnumLabel(VideoAvatar.Status_Failed, MediaServicesResources.Names.VideoAvatarStatus_Failed, typeof(MediaServicesResources))]
        Failed,

        [EnumLabel(VideoAvatar.Status_Archived, MediaServicesResources.Names.VideoAvatarStatus_Archived, typeof(MediaServicesResources))]
        Archived
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoAvatar_Title, MediaServicesResources.Names.VideoAvatar_Help, MediaServicesResources.Names.VideoAvatar_Description, EntityDescriptionAttribute.EntityTypes.CoreIoTModel, typeof(MediaServicesResources),
        GetUrl: "/api/media/videoavatar/{id}", GetListUrl: "/api/media/videoavatars", FactoryUrl: "/api/media/videoavatar/factory", SaveUrl: "/api/media/videoavatar", DeleteUrl: "/api/media/videoavatar/{id}",
        ListUIUrl: "/contentmanagement/videoavatars", EditUIUrl: "/contentmanagement/videoavatar/{id}", CreateUIUrl: "/contentmanagement/videoavatar/add",
        AiIconGuidance: "Represent a Video Avatar as a polished human presenter or collaborator prepared for AI-assisted video generation. Use a clean person or role-based silhouette as the dominant metaphor, optionally with one simple video or digital accent such as a frame, lens mark, signal layer, subtle orbit, or assistant indicator. The icon should feel like a reusable on-camera identity within a product system, not a generic user profile. Avoid generic profile placeholders, smiley faces, cartoon mascots, robot heads, chat bubbles, headset support icons, or overly expressive facial features. Keep the shape simple, centered, and readable at small sizes. For specific instance icons, preserve this same visual idea while adapting the role, look, or meaning of the individual video avatar.",
        Icon: "lago-icon://system/nuvos-semantic-icon/video-avatar-default", ClusterKey: "video", ModelType: EntityDescriptionAttribute.ModelTypes.Configuration, Shape: EntityDescriptionAttribute.EntityShapes.Entity, Lifecycle: EntityDescriptionAttribute.Lifecycles.DesignTime,
        Sensitivity: EntityDescriptionAttribute.Sensitivities.Internal, IndexInclude: true, IndexTier: EntityDescriptionAttribute.IndexTiers.Secondary, IndexPriority: 70, IndexTagsCsv: "media,video,avatar,heygen")]
    public class VideoAvatar : EntityBase, IValidateable, IFormDescriptor, ISummaryFactory
    {
        public const string Provider_HeyGen = "heygen";

        public const string Role_Primary = "primary";
        public const string Role_Editorial = "editorial";
        public const string Role_Campaign = "campaign";
        public const string Role_Experimental = "experimental";

        public const string Status_Draft = "draft";
        public const string Status_Preparing = "preparing";
        public const string Status_WaitingForProvider = "waiting-for-provider";
        public const string Status_Ready = "ready";
        public const string Status_Failed = "failed";
        public const string Status_Archived = "archived";

        public VideoAvatar()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/video-avatar-default";
            Provider = EntityHeader<VideoAvatarProvider>.Create(VideoAvatarProvider.HeyGen);
            Role = EntityHeader<VideoAvatarRole>.Create(VideoAvatarRole.Primary);
            Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Draft);
            AlternateLookResources = new List<ImageEntityHeader>();
            Looks = new List<VideoAvatarLook>();
            Voices = new List<VideoAvatarVoice>();
        }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_SourceImage, FieldType: FieldTypes.FileUpload, DisplayImageSize: "1024x1024", GeneratedImageSize: "1024x1024", ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true, AiImageQuality: "premium",
            ImageGenerationStyleGuidance: "Create a polished, contemporary portrait photograph that serves as the canonical identity image for this Video Avatar. Use warm natural or soft studio-style lighting, a restrained and professional background, believable posture, and a clean composition that keeps the subject clearly readable at profile, card, and directory sizes. Frame the image primarily from the chest or shoulders upward, with the face clearly visible and the subject oriented toward the camera. The subject should appear professional, approachable, confident, and credible, with a natural friendly expression rather than an exaggerated grin. Maintain a refined editorial-photography look with realistic skin tones, subtle depth of field, balanced contrast, and a visually clean finish. Avoid generic stock-photo staging, harsh or dramatic lighting, busy backgrounds, theatrical poses, exaggerated futuristic elements, embedded text, logos, watermarks, or visual clutter.",
            AiImagePurpose: "Create the primary source image for this Video Avatar. This image is reconciled into the primary provider look and is also used as the canonical identity image across profile, card, directory, and avatar-selection surfaces.")]
        public ImageEntityHeader PrimaryLookResource { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_EditorialImage, FieldType: FieldTypes.FileUploads, DisplayImageSize: "1024x1024", GeneratedImageSize: "1024x1024", ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true, AiImageQuality: "premium",
            ImageGenerationStyleGuidance: "Create polished, contemporary portrait photographs that preserve the recognizable identity of the primary Video Avatar image while providing useful alternate wardrobe, pose, framing, or professional-setting variations. Each image should remain suitable for provider avatar generation, with the face clearly visible, believable posture, clean composition, realistic lighting, and no embedded text, logos, watermarks, visual clutter, or dramatically inconsistent photographic styles.",
            AiReferenceImageField: "primaryLookResource",
            AiImagePurpose: "Create alternate source images that will each be reconciled into an additional provider look for this Video Avatar.")]
        public List<ImageEntityHeader> AlternateLookResources { get; set; }

        public List<VideoAvatarLook> Looks { get; set; }

        [System.Obsolete("Use PrimaryLookResource instead.")]
        public ImageEntityHeader AvatarImage
        {
            get => PrimaryLookResource;
            set
            {
                if (PrimaryLookResource == null)
                {
                    PrimaryLookResource = value;
                }
            }
        }

        public bool ShouldSerializeAvatarImage()
        {
            return false;
        }

        [System.Obsolete("Use AlternateLookResources instead.")]
        public List<ImageEntityHeader> EditorialImages
        {
            get => AlternateLookResources;
            set
            {
                if ((AlternateLookResources == null || AlternateLookResources.Count == 0) && value != null)
                {
                    AlternateLookResources = value;
                }
            }
        }

        public bool ShouldSerializeEditorialImages()
        {
            return false;
        }


        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_Provider, FieldType: FieldTypes.Picker, EnumType: typeof(VideoAvatarProvider), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public EntityHeader<VideoAvatarProvider> Provider { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_Role, FieldType: FieldTypes.Picker, EnumType: typeof(VideoAvatarRole), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public EntityHeader<VideoAvatarRole> Role { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_Status, FieldType: FieldTypes.Picker, EnumType: typeof(VideoAvatarStatus), ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: false)]
        public EntityHeader<VideoAvatarStatus> Status { get; set; }

        public string ProviderAvatarGroupId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_ProviderAssetId, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ProviderAssetId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_ProviderAvatarId, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ProviderAvatarId { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_ProviderAvatarStatus, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ProviderAvatarStatus { get; set; }


        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_IsDefault, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: true)]
        public bool IsDefault { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_LastStatusCheckUtc, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public UtcTimestamp? LastStatusCheck { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_LastUsedUtc, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public UtcTimestamp? LastUsed { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.VideoAvatar_ErrorMessage, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources), IsRequired: false, IsUserEditable: false)]
        public string ErrorMessage { get; set; }

        public List<VideoAvatarVoice> Voices { get; set; }

        public VideoAvatarVoice GetDefaultVoice()
        {
            if (Voices == null || Voices.Count == 0)
            {
                return null;
            }

            return Voices.FirstOrDefault(voice => voice.IsDefault) ?? Voices.FirstOrDefault();
        }

        public VideoAvatarSummary CreateSummary()
        {
            var summary = new VideoAvatarSummary
            {
                SourceImage = PrimaryLookResource,
                Provider = Provider,
                Role = Role,
                Status = Status,
                ProviderAvatarId = ProviderAvatarId,
                ProviderAvatarStatus = ProviderAvatarStatus,
                IsDefault = IsDefault
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
                nameof(IsDefault),
                nameof(Icon),
                nameof(Description),
                nameof(PrimaryLookResource),
                nameof(AlternateLookResources),
            };
        }



        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }
    }

    public class VideoAvatarLook
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToId();

        public string Name { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;

        public ImageEntityHeader SourceMediaResource { get; set; }

        public string SourceContentSha256 { get; set; }

        public EntityHeader<VideoAvatarStatus> Status { get; set; } = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Draft);

        public string ProviderAssetId { get; set; }

        public string ProviderAvatarId { get; set; }

        public string ProviderAvatarStatus { get; set; }

        public UtcTimestamp? LastStatusCheck { get; set; }

        public string BillingEventId { get; set; }

        public UtcTimestamp? BilledUtc { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class VideoAvatarVoice
    {
        public string Id { get; set; }

        public string Label { get; set; }

        public string VoiceId { get; set; }

        public string VoiceName { get; set; }

        public string LanguageCode { get; set; }

        public string LanguageName { get; set; }

        public string Locale { get; set; }

        public string Gender { get; set; }

        public string Accent { get; set; }

        public string VoiceType { get; set; }

        public bool IsDefault { get; set; }

        public bool IsPreviewable { get; set; }

        public string PreviewAudioUrl { get; set; }

        public string Notes { get; set; }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoAvatar_Title, MediaServicesResources.Names.VideoAvatar_Help, MediaServicesResources.Names.VideoAvatar_Description, EntityDescriptionAttribute.EntityTypes.Summary, typeof(MediaServicesResources),
        Icon: "lago-icon://system/nuvos-semantic-icon/video-avatar-default",
        GetUrl: "/api/media/videoavatar/{id}", GetListUrl: "/api/media/videoavatars", FactoryUrl: "/api/media/videoavatar/factory", SaveUrl: "/api/media/videoavatar", DeleteUrl: "/api/media/videoavatar/{id}",
        ListUIUrl: "/contentmanagement/videoavatars", EditUIUrl: "/contentmanagement/videoavatar/{id}", CreateUIUrl: "/contentmanagement/videoavatar/add")]
    public class VideoAvatarSummary : SummaryData
    {
        public ImageEntityHeader SourceImage { get; set; }

        public EntityHeader<VideoAvatarProvider> Provider { get; set; }
        public EntityHeader<VideoAvatarRole> Role { get; set; }
        public EntityHeader<VideoAvatarStatus> Status { get; set; }

        public string ProviderAvatarId { get; set; }
        public string ProviderAvatarStatus { get; set; }

        public bool IsDefault { get; set; }
    }
}