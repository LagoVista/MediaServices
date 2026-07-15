using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public sealed class HeyGenWebhookEvent
    {
        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("event_type")]
        public string EventType { get; set; }

        [JsonProperty("event_data")]
        public JObject EventData { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
    }

    public sealed class HeyGenVideoWebhookData
    {
        [JsonProperty("video_id")]
        public string VideoId { get; set; }

        [JsonProperty("callback_id")]
        public string CallbackId { get; set; }

        [JsonProperty("url")]
        public string VideoUrl { get; set; }

        [JsonProperty("gif_download_url")]
        public string GifDownloadUrl { get; set; }

        [JsonProperty("video_page_url")]
        public string VideoPageUrl { get; set; }

        [JsonProperty("video_share_page_url")]
        public string VideoSharePageUrl { get; set; }

        [JsonProperty("error")]
        public JToken Error { get; set; }

        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }

        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class HeyGenVideoDetailsResponse
    {
        [JsonProperty("data")]
        public HeyGenVideoDetails Data { get; set; }
    }

    public class HeyGenVideoDetails
    {
        [JsonProperty("video_id")]
        public string VideoId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("video_url")]
        public string VideoUrl { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("caption_url")]
        public string CaptionUrl { get; set; }

        [JsonProperty("duration")]
        public decimal? Duration { get; set; }

        [JsonProperty("failure_code")]
        public string FailureCode { get; set; }

        [JsonProperty("failure_message")]
        public string FailureMessage { get; set; }
    }

    public class VimeoPullUploadRequest
    {
        [JsonProperty("upload")]
        public VimeoPullUploadSource Upload { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("privacy")]
        public VimeoPrivacySettings Privacy { get; set; }
    }

    public class VimeoPullUploadSource
    {
        [JsonProperty("approach")]
        public string Approach { get; set; } = "pull";

        [JsonProperty("link")]
        public string Link { get; set; }
    }

    public class VimeoTusUploadRequest
    {
        [JsonProperty("upload")]
        public VimeoTusUploadSource Upload { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("privacy")]
        public VimeoPrivacySettings Privacy { get; set; }
    }

    public class VimeoTusUploadSource
    {
        [JsonProperty("approach")]
        public string Approach { get; set; } = "tus";

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class VimeoPrivacySettings
    {
        [JsonProperty("view")]
        public string View { get; set; }
    }

    public class VimeoVideo
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("upload")]
        public VimeoUploadState Upload { get; set; }

        [JsonProperty("transcode")]
        public VimeoTranscodeState Transcode { get; set; }
    }

    public class VimeoUploadState
    {
        [JsonProperty("approach")]
        public string Approach { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("upload_link")]
        public string UploadLink { get; set; }
    }

    public class VimeoTranscodeState
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class VimeoTenantSettings
    {
        public string AccessToken { get; set; }

        public string DefaultFolderUri { get; set; }

        public string DefaultPrivacy { get; set; }
    }

    public class HeyGenVideoStatusResponse
    {
        [JsonProperty("data")]
        public HeyGenVideoStatusData Data { get; set; }
    }

    public class HeyGenVideoStatusData
    {
        [JsonProperty("video_id")]
        public string VideoId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("video_url")]
        public string VideoUrl { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("caption_url")]
        public string CaptionUrl { get; set; }

        [JsonProperty("duration")]
        public decimal? DurationSeconds { get; set; }

        [JsonProperty("error")]
        public HeyGenVideoStatusError Error { get; set; }
    }

    public class HeyGenVideoStatusError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class HeyGenVideoStatusResult
    {
        public string VideoId { get; set; }

        public string Status { get; set; }

        public string VideoUrl { get; set; }

        public string ThumbnailUrl { get; set; }

        public string CaptionUrl { get; set; }

        public decimal? DurationSeconds { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }
    }
}
