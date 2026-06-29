using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Services
{
    public class HeyGenVideoService : IHeyGenVideoService
    {
        private readonly HttpClient _httpClient;
        private readonly IMediaServicesConnectionSettings _settings;

        public HeyGenVideoService(IHttpClientFactory httpClientFactory, IMediaServicesConnectionSettings settings)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://api.heygen.com/");

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }


        public async Task<InvokeResult<HeyGenVideoSubmission>> SubmitVideoAsync(HeyGenVideoRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.AvatarId))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen avatar ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("Video script is required.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen API key has not been configured.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/videos");

            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError($"HeyGen video submission failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var submissionResponse = JsonConvert.DeserializeObject<HeyGenVideoSubmissionResponse>(responseContent);

            if (string.IsNullOrWhiteSpace(submissionResponse?.Data?.VideoId))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video submission completed without returning a video ID.");
            }

            return InvokeResult<HeyGenVideoSubmission>.Create(new HeyGenVideoSubmission
            {
                VideoId = submissionResponse.Data.VideoId
            });
        }

        public async Task<InvokeResult<HeyGenAssetUploadResult>> UploadAssetAsync(Stream stream, string fileName, string contentType, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError("Asset stream is required.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError("Asset file name is required.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError("Asset content type is required.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "v3/assets");
            using var multipartContent = new MultipartFormDataContent();
            using var streamContent = new StreamContent(stream);

            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError("HeyGen API key has not been configured.");
            }

            request.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.Add("Idempotency-Key", idempotencyKey);
            }

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            multipartContent.Add(streamContent, "file", fileName);

            request.Content = multipartContent;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError($"HeyGen asset upload failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var uploadResponse = JsonConvert.DeserializeObject<HeyGenAssetUploadResponse>(responseContent);

            if (string.IsNullOrWhiteSpace(uploadResponse?.Data?.AssetId))
            {
                return InvokeResult<HeyGenAssetUploadResult>.FromError("HeyGen asset upload completed without returning an asset ID.");
            }

            return InvokeResult<HeyGenAssetUploadResult>.Create(new HeyGenAssetUploadResult
            {
                AssetId = uploadResponse.Data.AssetId
            });
        }

        public async Task<InvokeResult<HeyGenAvatarCreationResult>> CreatePhotoAvatarAsync(HeyGenPhotoAvatarRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("Photo avatar request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("Photo avatar name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.File?.AssetId))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("Photo avatar asset ID is required.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("HeyGen API key has not been configured.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/avatars");

            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            var requestJson = JsonConvert.SerializeObject(request);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError($"HeyGen photo avatar creation failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var createResponse = JsonConvert.DeserializeObject<HeyGenCreateAvatarResponse>(responseContent);

            if (string.IsNullOrWhiteSpace(createResponse?.Data?.AvatarItem?.Id))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("HeyGen photo avatar creation completed without returning an avatar ID.");
            }

            return InvokeResult<HeyGenAvatarCreationResult>.Create(new HeyGenAvatarCreationResult
            {
                AvatarId = createResponse.Data.AvatarItem.Id
            });
        }

        public async Task<InvokeResult<HeyGenAvatarStatusResult>> GetAvatarStatusAsync(string avatarId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return InvokeResult<HeyGenAvatarStatusResult>.FromError("HeyGen avatar ID is required.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenAvatarStatusResult>.FromError("HeyGen API key has not been configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v3/avatars/looks/{Uri.EscapeDataString(avatarId)}");

            request.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenAvatarStatusResult>.FromError($"HeyGen avatar status request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var statusResponse = JsonConvert.DeserializeObject<HeyGenAvatarStatusResponse>(responseContent);

            if (statusResponse?.Data == null)
            {
                return InvokeResult<HeyGenAvatarStatusResult>.FromError("HeyGen avatar status request completed without returning avatar data.");
            }

            return InvokeResult<HeyGenAvatarStatusResult>.Create(new HeyGenAvatarStatusResult
            {
                AvatarId = statusResponse.Data.Id,
                Status = statusResponse.Data.Status,
                ErrorCode = statusResponse.Data.Error?.Code,
                ErrorMessage = statusResponse.Data.Error?.Message
            });
        }
    }
}
