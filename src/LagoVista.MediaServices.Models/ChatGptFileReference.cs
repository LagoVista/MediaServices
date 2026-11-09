using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace LagoVista.MediaServices.Models
{
    /// <summary>
    /// ChatGPT MCP native file argument. The host populates this from
    /// _meta["openai/fileParams"]; the model must never construct a signed URL.
    /// </summary>
    public sealed class ChatGptFileReference
    {
        [Required]
        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; }

        [Required]
        [JsonProperty("file_id")]
        public string FileId { get; set; }

        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }
    }

    /// <summary>
    /// Read-only transport probe; no Media Resource is created.
    /// </summary>
    public sealed class ChatGptFileProbeResult
    {
        [JsonProperty("file_id")]
        public string FileId { get; set; }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonProperty("content_size")]
        public long ContentSize { get; set; }

        [JsonProperty("content_sha256")]
        public string ContentSha256 { get; set; }

        [JsonProperty("detected_mime_type")]
        public string DetectedMimeType { get; set; }
    }
}
