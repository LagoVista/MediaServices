using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IHeyGenVideoService
    {
        Task<InvokeResult<HeyGenAssetUploadResult>> UploadAssetAsync(Stream stream, string fileName, string contentType, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenVideoSubmission>> SubmitVideoAsync(HeyGenVideoRequest request, VideoProductionQuality quality, VideoProductionSettings settings, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenAvatarCreationResult>> CreatePhotoAvatarAsync(HeyGenPhotoAvatarRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenAvatarStatusResult>> GetAvatarStatusAsync(string avatarId, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenVoiceListResult>> GetVoicesAsync(HeyGenVoiceListRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenSpeechPreviewResult>> GenerateSpeechPreviewAsync(HeyGenSpeechPreviewRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenWebhookRegistration>> EnsureWebhookRegistrationAsync(EntityHeader secretOwner, EntityHeader user, string callbackUrl, CancellationToken cancellationToken = default);
        Task<InvokeResult> ValidateWebhookSignatureAsync(EntityHeader secretOwner, EntityHeader secretReader, string rawPayload, string signature, string timestamp, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenVideoDetails>> GetVideoAsync(string videoId, CancellationToken cancellationToken = default);

        Task<InvokeResult<HeyGenVideoStatusResult>> GetVideoStatusAsync(string videoId, CancellationToken cancellationToken = default);
    }

    internal sealed class HeyGenCreateWebhookEndpointRequest
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("events")]
        public List<string> Events { get; set; } = new List<string>();
    }

    internal sealed class HeyGenCreateWebhookEndpointResponse
    {
        [JsonProperty("data")]
        public HeyGenWebhookEndpointData Data { get; set; }
    }

    internal sealed class HeyGenWebhookEndpointData
    {
        [JsonProperty("endpoint_id")]
        public string EndpointId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("events")]
        public List<string> Events { get; set; } = new List<string>();

        [JsonProperty("secret")]
        public string Secret { get; set; }
    }

}
