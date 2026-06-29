using Newtonsoft.Json;

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
}
