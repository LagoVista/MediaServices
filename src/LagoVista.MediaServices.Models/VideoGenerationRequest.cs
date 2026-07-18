using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using System;
namespace LagoVista.MediaServices.Models
{

    public sealed class VideoGenerationRequest : TableStorageEntity
    {
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }

        [Obsolete("Use VideoAvatarId and VideoAvatarLookId.")]
        public string AvatarMediaResourceId { get; set; }

        public string HeyGenAvatarId { get; set; }
        public string BackgroundMediaResourceId { get; set; }

        public string Script { get; set; }
        public string VideoName { get; set; }

        public string LanguageCode { get; set; }

        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string EntityProperty { get; set; }

        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string LastUpdatedUtc { get; set; }

        public string ErrorMessage { get; set; }

        // Populated during later steps.
        public string HeyGenBackgroundAssetId { get; set; }
        public string HeyGenVideoId { get; set; }
        public string HeyGenVideoUrl { get; set; }

        public string VimeoVideoId { get; set; }
        public string VimeoVideoUrl { get; set; }

        public string HeyGenSubmittedUtc { get; set; }
        public string HeyGenCompletedUtc { get; set; }
        public string VimeoSubmittedUtc { get; set; }
        public string VimeoCompletedUtc { get; set; }
        public string EntityUpdatedUtc { get; set; }
        public string BillingEventSubmittedUtc { get; set; }
    }

    public static class VideoGenerationRequestStatuses
    {
        public const string Created = "created";
        public const string Submitting = "submitting";
        public const string Submitted = "submitted";
        public const string Failed = "failed";
        public const string WaitingForAvatar = "waiting-for-avatar";
    }
}
