using Newtonsoft.Json;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Services
{
    public sealed class HeyGenPhotoAvatarRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "photo";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("file")]
        public HeyGenPhotoAvatarFile File { get; set; }
    }

    public sealed class HeyGenPhotoAvatarFile
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "asset_id";

        [JsonProperty("asset_id")]
        public string AssetId { get; set; }
    }

    internal sealed class HeyGenCreateAvatarResponse
    {
        [JsonProperty("data")]
        public HeyGenCreateAvatarData Data { get; set; }
    }

    internal sealed class HeyGenCreateAvatarData
    {
        [JsonProperty("avatar_item")]
        public HeyGenAvatarItem AvatarItem { get; set; }
    }

    internal sealed class HeyGenAvatarItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal sealed class HeyGenAvatarStatusResponse
    {
        [JsonProperty("data")]
        public HeyGenAvatarStatusData Data { get; set; }
    }

    internal sealed class HeyGenAvatarStatusData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error")]
        public HeyGenAvatarStatusError Error { get; set; }
    }

    internal sealed class HeyGenAvatarStatusError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal sealed class HeyGenVideoSubmissionResponse
    {
        [JsonProperty("data")]
        public HeyGenVideoSubmissionData Data { get; set; }
    }

    internal sealed class HeyGenVideoSubmissionData
    {
        [JsonProperty("video_id")]
        public string VideoId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("output_format")]
        public string OutputFormat { get; set; }
    }

    public sealed class HeyGenVoiceListRequest
    {
        public string Engine { get; set; } = "starfish";

        public string Language { get; set; }

        public string Gender { get; set; }

        public string Type { get; set; }

        public int? Limit { get; set; } = 100;

        public string Token { get; set; }
    }

    public sealed class HeyGenVoiceListResult
    {
        public List<HeyGenVoiceSummary> Voices { get; set; } = new List<HeyGenVoiceSummary>();

        public bool HasMore { get; set; }

        public string NextToken { get; set; }
    }

    public sealed class HeyGenVoiceSummary
    {
        public string VoiceId { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string Locale { get; set; }
        public string Gender { get; set; }
        public string Accent { get; set; }
        public string Age { get; set; }
        public string Type { get; set; }
        public string PreviewAudioUrl { get; set; }
        public string Engine { get; set; }
        public bool SupportInteractiveAvatar { get; set; }
        public bool SupportLocale { get; set; }
        public bool SupportPause { get; set; }
        public bool IsPreviewable { get; set; }
        public List<string> SupportedEngines { get; set; } = new List<string>();
    }

    public sealed class HeyGenSpeechPreviewRequest
    {
        public string VoiceId { get; set; }
        public string Text { get; set; }
        public string Locale { get; set; }
    }

    public sealed class HeyGenSpeechPreviewResult
    {
        public string AudioUrl { get; set; }
        public int? DurationSeconds { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string Currency { get; set; }
    }

    public sealed class HeyGenVoiceListResponse
    {
        [JsonProperty("data")]
        public List<HeyGenVoiceItem> Data { get; set; }

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        [JsonProperty("next_token")]
        public string NextToken { get; set; }
    }

    public sealed class HeyGenVoiceItem
    {
        [JsonProperty("voice_id")]
        public string VoiceId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("accent")]
        public string Accent { get; set; }

        [JsonProperty("age")]
        public string Age { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("preview_audio_url")]
        public string PreviewAudioUrl { get; set; }

        [JsonProperty("support_interactive_avatar")]
        public bool SupportInteractiveAvatar { get; set; }

        [JsonProperty("support_locale")]
        public bool SupportLocale { get; set; }

        [JsonProperty("support_pause")]
        public bool SupportPause { get; set; }

        [JsonProperty("engine")]
        public string Engine { get; set; }

        [JsonProperty("engines")]
        public List<string> Engines { get; set; }

        [JsonProperty("supported_engines")]
        public List<string> SupportedEngines { get; set; }
    }

    public sealed class HeyGenSpeechPreviewApiRequest
    {
        [JsonProperty("voice_id")]
        public string VoiceId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }
    }

    internal sealed class HeyGenSpeechPreviewResponse
    {
        [JsonProperty("data")]
        public HeyGenSpeechPreviewData Data { get; set; }
    }

    internal sealed class HeyGenSpeechPreviewData
    {
        [JsonProperty("audio_url")]
        public string AudioUrl { get; set; }

        [JsonProperty("duration_seconds")]
        public int? DurationSeconds { get; set; }

        [JsonProperty("duration")]
        public decimal? Duration { get; set; }
    }
}
