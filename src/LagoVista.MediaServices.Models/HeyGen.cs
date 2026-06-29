using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public sealed class HeyGenVideoRequest
    {
        public string AvatarId { get; set; }
        public string Script { get; set; }
        public string VoiceId { get; set; }
        public string LanguageCode { get; set; }
        public string Title { get; set; }
        public string CallbackId { get; set; }
        public string CallbackUrl { get; set; }
        public string Resolution { get; set; }
        public string AspectRatio { get; set; }
        public HeyGenBackground Background { get; set; }
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
