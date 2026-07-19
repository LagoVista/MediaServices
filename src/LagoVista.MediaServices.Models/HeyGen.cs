using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public sealed class HeyGenVideoRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "avatar";

        [JsonProperty("avatar_id")]
        public string AvatarId { get; set; }

        [JsonProperty("script")]
        public string Script { get; set; }

        [JsonProperty("voice_id")]
        public string VoiceId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("callback_id")]
        public string CallbackId { get; set; }

        [JsonProperty("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        [JsonProperty("aspect_ratio")]
        public string AspectRatio { get; set; }

        [JsonProperty("fit", NullValueHandling = NullValueHandling.Ignore)]
        public string Fit { get; set; }

        [JsonProperty("remove_background", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RemoveBackground { get; set; }

        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public HeyGenCaptionSettings Caption { get; set; }

        [JsonProperty("motion_prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string MotionPrompt { get; set; }

        [JsonProperty("expressiveness", NullValueHandling = NullValueHandling.Ignore)]
        public string Expressiveness { get; set; }

        [JsonProperty("engine", NullValueHandling = NullValueHandling.Ignore)]
        public HeyGenEngineSettings Engine { get; set; }

        [JsonProperty("output_format")]
        public string OutputFormat { get; set; } = "mp4";

        [JsonProperty("background")]
        public HeyGenBackground Background { get; set; }

        [JsonProperty("voice_settings")]
        public HeyGenVoiceSettings VoiceSettings { get; set; }
    }

    public sealed class HeyGenAssetUploadResponse
    {
        [JsonProperty("data")]
        public HeyGenAssetUploadData Data { get; set; }
    }

    public sealed class HeyGenAssetUploadData
    {
        [JsonProperty("asset_id")]
        public string AssetId { get; set; }
    }

    public sealed class HeyGenBackground
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("asset_id", NullValueHandling = NullValueHandling.Ignore)]
        public string AssetId { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
    }

    public sealed class HeyGenVoiceSettings
    {
        [JsonProperty("speed", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Speed { get; set; }

        [JsonProperty("pitch", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Pitch { get; set; }

        [JsonProperty("volume", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Volume { get; set; }

        [JsonProperty("locale", NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; set; }
    }

    public sealed class HeyGenCaptionSettings
    {
        [JsonProperty("file_format")]
        public string FileFormat { get; set; } = "srt";

        [JsonProperty("style", NullValueHandling = NullValueHandling.Ignore)]
        public string Style { get; set; }
    }

    public sealed class HeyGenEngineSettings
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("reference_look_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceLookId { get; set; }
    }

    public sealed class HeyGenVideoSubmission
    {
        public string VideoId { get; set; }
    }

    public sealed class HeyGenAssetUploadResult
    {
        public string AssetId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }


    public sealed class HeyGenAvatarCreationResult
    {
        public string AvatarGroupId { get; set; }
        public string AvatarId { get; set; }
        public string Status { get; set; }
    }



    public sealed class HeyGenAvatarStatusResult
    {
        public string AvatarId { get; set; }
        public string Status { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsReady => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);
    }


    public sealed class VideoProductionProviderState
    {
        public decimal? ActualCost { get; set; }
        public string ProviderVideoUrl { get; set; }

        public string ProviderThumbnailUrl { get; set; }

        public string ProviderCaptionUrl { get; set; }

        public int? ActualDurationSeconds { get; set; }

        public EntityHeader<VideoProductionStatus> Status { get; set; }

        public string CompletedUtc { get; set; }

        public string LastStatusCheckUtc { get; set; }

        public string ErrorMessage { get; set; }
    }
}
