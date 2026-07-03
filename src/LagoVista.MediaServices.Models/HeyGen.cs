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
        public string AssetId { get; set; }
    }

    public sealed class HeyGenVoiceSettings
    {
        public string Locale { get; set; }
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
        public string AvatarId { get; set; }
    }



    public sealed class HeyGenAvatarStatusResult
    {
        public string AvatarId { get; set; }
        public string Status { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsReady => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);
    }
}
