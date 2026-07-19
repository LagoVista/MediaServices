using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RingCentral;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Services
{
    public class HeyGenVideoService : IHeyGenVideoService
    {
        private readonly HttpClient _httpClient;
        private readonly IMediaServicesConnectionSettings _settings;
        private readonly IAdminLogger _adminLogger;

        private static readonly TimeSpan WebhookRegistrationLockDuration = TimeSpan.FromMinutes(2);

        private readonly ISecureStorage _secureStorage;
        private readonly ICacheProvider _cacheProvider;

        public HeyGenVideoService(IHttpClientFactory httpClientFactory, IAdminLogger adminLogger, IMediaServicesConnectionSettings settings, ISecureStorage secureStorage, ICacheProvider cacheProvider)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://api.heygen.com/");

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        }


        public async Task<InvokeResult<HeyGenWebhookRegistration>> EnsureWebhookRegistrationAsync(EntityHeader secretOwner, EntityHeader user, string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (secretOwner == null || String.IsNullOrWhiteSpace(secretOwner.Id))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("Webhook secret owner is required.");
            }

            if (user == null || String.IsNullOrWhiteSpace(user.Id))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("User is required to access webhook registration.");
            }

            if (String.IsNullOrWhiteSpace(callbackUrl))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("HeyGen webhook callback URL is required.");
            }

            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri) || callbackUri.Scheme != Uri.UriSchemeHttps)
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("HeyGen webhook callback URL must be an absolute HTTPS URL.");
            }

            var existingResult = await TryGetWebhookRegistrationAsync(secretOwner, user);
            if (existingResult.Successful && existingResult.Result != null)
            {
                if (!String.Equals(existingResult.Result.CallbackUrl, callbackUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeResult<HeyGenWebhookRegistration>.FromError(
                        $"The stored HeyGen webhook callback URL '{existingResult.Result.CallbackUrl}' does not match '{callbackUrl}'.");
                }

                return existingResult;
            }

            var lockToken = Guid.NewGuid().ToString("N");
            var lockAcquired = await _cacheProvider.AttemptAcquireLockAsync(
                HeyGenWebhookConstants.RegistrationLockKey,
                lockToken,
                WebhookRegistrationLockDuration);

            if (!lockAcquired)
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("HeyGen webhook registration is already being prepared.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                existingResult = await TryGetWebhookRegistrationAsync(secretOwner, user);
                if (existingResult.Successful && existingResult.Result != null)
                {
                    if (!String.Equals(existingResult.Result.CallbackUrl, callbackUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeResult<HeyGenWebhookRegistration>.FromError(
                            $"The stored HeyGen webhook callback URL '{existingResult.Result.CallbackUrl}' does not match '{callbackUrl}'.");
                    }

                    return existingResult;
                }

                var createResult = await CreateWebhookEndpointAsync(callbackUrl, cancellationToken);
                if (!createResult.Successful)
                {
                    return createResult;
                }

                var saveResult = await SaveWebhookRegistrationAsync(secretOwner, createResult.Result);
                if (!saveResult.Successful)
                {
                    return saveResult.ToInvokeResult<HeyGenWebhookRegistration>();
                }

                return createResult;
            }
            finally
            {
                await _cacheProvider.ReleaseLockAsync(HeyGenWebhookConstants.RegistrationLockKey, lockToken);
            }
        }

        private async Task<InvokeResult<HeyGenWebhookRegistration>> TryGetWebhookRegistrationAsync(EntityHeader secretOwner, EntityHeader user)
        {
            var secretResult = await _secureStorage.GetSecretAsync(secretOwner, HeyGenWebhookConstants.RegistrationSecretId, user);

            if (!secretResult.Successful || String.IsNullOrWhiteSpace(secretResult.Result))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("HeyGen webhook registration has not been created.");
            }

            try
            {
                var registration = JsonConvert.DeserializeObject<HeyGenWebhookRegistration>(secretResult.Result);

                if (registration == null ||
                    String.IsNullOrWhiteSpace(registration.EndpointId) ||
                    String.IsNullOrWhiteSpace(registration.SigningSecret) ||
                    String.IsNullOrWhiteSpace(registration.CallbackUrl))
                {
                    return InvokeResult<HeyGenWebhookRegistration>.FromError("Stored HeyGen webhook registration is incomplete.");
                }

                return InvokeResult<HeyGenWebhookRegistration>.Create(registration);
            }
            catch (JsonException ex)
            {
                _adminLogger.AddException(this.Tag(), ex);

                return InvokeResult<HeyGenWebhookRegistration>.FromError("Stored HeyGen webhook registration could not be read.");
            }
        }

        private async Task<InvokeResult<HeyGenWebhookRegistration>> CreateWebhookEndpointAsync(string callbackUrl, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError("HeyGen API key has not been configured.");
            }

            var apiRequest = new HeyGenCreateWebhookEndpointRequest
            {
                Url = callbackUrl,
                Events = new List<string>
        {
            HeyGenWebhookConstants.EventVideoSuccess,
            HeyGenWebhookConstants.EventVideoFail
        }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/webhooks/endpoints");

            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);
            httpRequest.Headers.Add("Idempotency-Key", HeyGenWebhookConstants.RegistrationIdempotencyKey);

            var requestJson = JsonConvert.SerializeObject(apiRequest);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError(
                    $"HeyGen webhook registration failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var createResponse = JsonConvert.DeserializeObject<HeyGenCreateWebhookEndpointResponse>(responseContent);

            if (String.IsNullOrWhiteSpace(createResponse?.Data?.EndpointId))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError(
                    "HeyGen webhook registration completed without returning an endpoint ID.");
            }

            if (String.IsNullOrWhiteSpace(createResponse.Data.Secret))
            {
                return InvokeResult<HeyGenWebhookRegistration>.FromError(
                    "HeyGen webhook registration completed without returning a signing secret.");
            }

            return InvokeResult<HeyGenWebhookRegistration>.Create(new HeyGenWebhookRegistration
            {
                EndpointId = createResponse.Data.EndpointId,
                SigningSecret = createResponse.Data.Secret,
                CallbackUrl = createResponse.Data.Url ?? callbackUrl,
                Events = createResponse.Data.Events ?? apiRequest.Events
            });
        }

        private async Task<InvokeResult> SaveWebhookRegistrationAsync(EntityHeader secretOwner, HeyGenWebhookRegistration registration)
        {
            if (registration == null)
            {
                return InvokeResult.FromError("HeyGen webhook registration is required.");
            }

            var value = JsonConvert.SerializeObject(registration);
            var saveResult = await _secureStorage.AddSecretAsync(secretOwner, HeyGenWebhookConstants.RegistrationSecretId, value);

            if (!saveResult.Successful)
            {
                return saveResult.ToInvokeResult();
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult<HeyGenVideoSubmission>> SubmitVideoAsync(HeyGenVideoRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video request is required.");
            }

            if (String.IsNullOrWhiteSpace(request.AvatarId))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen avatar ID is required.");
            }

            if (String.IsNullOrWhiteSpace(request.Script))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("Video script is required.");
            }

            if (String.IsNullOrWhiteSpace(request.VoiceId))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen voice ID is required.");
            }

            if (String.IsNullOrWhiteSpace(request.Resolution))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video resolution is required.");
            }

            if (String.IsNullOrWhiteSpace(request.AspectRatio))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video aspect ratio is required.");
            }

            if (String.IsNullOrWhiteSpace(request.Engine?.Type))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen rendering engine is required.");
            }

            if (String.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen API key has not been configured.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/videos");

            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            _adminLogger.WriteJson(nameof(HeyGenVideoRequest), request);

            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenVideoSubmission>.FromError($"HeyGen video submission failed with status {(int)response.StatusCode}: {responseContent}");
            }

            HeyGenVideoSubmissionResponse submissionResponse;

            try
            {
                submissionResponse = JsonConvert.DeserializeObject<HeyGenVideoSubmissionResponse>(responseContent);
            }
            catch (JsonException ex)
            {
                _adminLogger.AddException(this.Tag(), ex);
                return InvokeResult<HeyGenVideoSubmission>.FromError("HeyGen video submission returned an invalid response.");
            }

            if (String.IsNullOrWhiteSpace(submissionResponse?.Data?.VideoId))
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

            _adminLogger.WriteJson("CREATEAVATR", responseContent);

            var createResponse = JsonConvert.DeserializeObject<HeyGenCreateAvatarResponse>(responseContent);

            if (string.IsNullOrWhiteSpace(createResponse?.Data?.AvatarItem?.Id))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("HeyGen photo avatar creation completed without returning an avatar ID.");
            }

            var avatarGroupId = createResponse.Data.AvatarGroup?.Id ?? createResponse.Data.AvatarItem.GroupId;

            if (string.IsNullOrWhiteSpace(avatarGroupId))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError("HeyGen photo avatar creation completed without returning an avatar group ID.");
            }

            if (!string.IsNullOrWhiteSpace(request.AvatarGroupId) &&
                !string.Equals(request.AvatarGroupId, avatarGroupId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<HeyGenAvatarCreationResult>.FromError($"HeyGen created avatar look '{createResponse.Data.AvatarItem.Id}' under unexpected avatar group '{avatarGroupId}'. Expected '{request.AvatarGroupId}'.");
            }

            return InvokeResult<HeyGenAvatarCreationResult>.Create(new HeyGenAvatarCreationResult
            {
                AvatarGroupId = avatarGroupId,
                AvatarId = createResponse.Data.AvatarItem.Id,
                Status = createResponse.Data.AvatarItem.Status
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

        public async Task<InvokeResult<HeyGenVoiceListResult>> GetVoicesAsync(HeyGenVoiceListRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenVoiceListResult>.FromError("HeyGen API key has not been configured.");
            }

            var url = BuildVoiceListUrl(request);

            _adminLogger.Trace($"{this.Tag()} Request Url: {url}");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenVoiceListResult>.FromError($"HeyGen voice list request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            _adminLogger.WriteJson("GetVoicesResponse", responseContent);

            var voiceResponse = JsonConvert.DeserializeObject<HeyGenVoiceListResponse>(responseContent);

            var result = new HeyGenVoiceListResult
            {
                HasMore = voiceResponse?.HasMore ?? false,
                NextToken = voiceResponse?.NextToken
            };

            if (voiceResponse?.Data != null)
            {
                foreach (var voice in voiceResponse.Data)
                {
                    var voiceId = string.IsNullOrWhiteSpace(voice.VoiceId) ? voice.Id : voice.VoiceId;
                    var name = (voice.Name ?? string.Empty).Trim();

                    result.Voices.Add(new HeyGenVoiceSummary
                    {
                        VoiceId = voiceId,
                        Name = string.IsNullOrWhiteSpace(name) ? voiceId : name,
                        Language = voice.Language,
                        Locale = voice.Locale,
                        Gender = voice.Gender,
                        Accent = voice.Accent,
                        Age = voice.Age,
                        Type = voice.Type,
                        PreviewAudioUrl = voice.PreviewAudioUrl,
                        SupportInteractiveAvatar= voice.SupportInteractiveAvatar,
                        SupportLocale = voice.SupportLocale,
                        SupportPause = voice.SupportPause,
                        IsPreviewable = !string.IsNullOrWhiteSpace(voice.PreviewAudioUrl)
                    });
                }
            }

            return InvokeResult<HeyGenVoiceListResult>.Create(result);
        }

        private static string BuildVoiceListUrl(HeyGenVoiceListRequest request)
        {
            var query = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Engine))
            {
                request.Engine = "starfish";
            }

            if (!string.IsNullOrWhiteSpace(request.Engine))
            {
                query.Add($"engine={Uri.EscapeDataString(request.Engine)}");
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                query.Add($"type={Uri.EscapeDataString(request.Type)}");
            }

            if (!string.IsNullOrWhiteSpace(request.Language))
            {
                query.Add($"language={Uri.EscapeDataString(request.Language)}");
            }

            if (!string.IsNullOrWhiteSpace(request.Gender))
            {
                query.Add($"gender={Uri.EscapeDataString(request.Gender)}");
            }

            if (request.Limit.HasValue)
            {
                query.Add($"limit={request.Limit.Value}");
            }

            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                query.Add($"token={Uri.EscapeDataString(request.Token)}");
            }

            return $"v3/voices?{string.Join("&", query)}";
        }

        public async Task<InvokeResult<HeyGenVideoStatusResult>> GetVideoStatusAsync(string videoId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(videoId))
            {
                return InvokeResult<HeyGenVideoStatusResult>.FromError("HeyGen video ID is required.");
            }

            if (String.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenVideoStatusResult>.FromError("HeyGen API key has not been configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/video_status.get?video_id={Uri.EscapeDataString(videoId)}");

            request.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenVideoStatusResult>.FromError($"HeyGen video status request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var statusResponse = JsonConvert.DeserializeObject<HeyGenVideoStatusResponse>(responseContent);

            if (statusResponse?.Data == null)
            {
                return InvokeResult<HeyGenVideoStatusResult>.FromError("HeyGen video status request completed without returning video data.");
            }

            return InvokeResult<HeyGenVideoStatusResult>.Create(new HeyGenVideoStatusResult
            {
                VideoId = statusResponse.Data.VideoId,
                Status = statusResponse.Data.Status,
                VideoUrl = statusResponse.Data.VideoUrl,
                ThumbnailUrl = statusResponse.Data.ThumbnailUrl,
                CaptionUrl = statusResponse.Data.CaptionUrl,
                DurationSeconds = statusResponse.Data.DurationSeconds,
                ErrorCode = statusResponse.Data.Error?.Code,
                ErrorMessage = statusResponse.Data.Error?.Message
            });
        }

        public async Task<InvokeResult<HeyGenSpeechPreviewResult>> GenerateSpeechPreviewAsync(HeyGenSpeechPreviewRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError("Speech preview request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.VoiceId))
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError("Voice ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError("Preview text is required.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError("HeyGen API key has not been configured.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3/voices/speech");
            httpRequest.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            var apiRequest = new HeyGenSpeechPreviewApiRequest
            {
                VoiceId = request.VoiceId,
                Text = request.Text,
                Locale = request.Locale
            };

            var requestJson = JsonConvert.SerializeObject(apiRequest, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError($"HeyGen speech preview request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var previewResponse = JsonConvert.DeserializeObject<HeyGenSpeechPreviewResponse>(responseContent);
            if (string.IsNullOrWhiteSpace(previewResponse?.Data?.AudioUrl))
            {
                return InvokeResult<HeyGenSpeechPreviewResult>.FromError("HeyGen speech preview completed without returning an audio URL.");
            }

            var durationSeconds = previewResponse.Data.DurationSeconds ?? (previewResponse.Data.Duration.HasValue ? (int?)Math.Ceiling(previewResponse.Data.Duration.Value) : null);

            return InvokeResult<HeyGenSpeechPreviewResult>.Create(new HeyGenSpeechPreviewResult
            {
                AudioUrl = previewResponse.Data.AudioUrl,
                DurationSeconds = durationSeconds,
                EstimatedCost = durationSeconds.HasValue ? Math.Round(durationSeconds.Value * 0.000667m, 4) : (decimal?)null,
                Currency = "USD"
            });
        }

        private static readonly TimeSpan WebhookTimestampTolerance = TimeSpan.FromMinutes(5);

        public async Task<InvokeResult> ValidateWebhookSignatureAsync(EntityHeader secretOwner, EntityHeader secretReader, string rawPayload, string signature, string timestamp, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(rawPayload))
            {
                return InvokeResult.FromError("HeyGen webhook payload is required.");
            }

            if (String.IsNullOrWhiteSpace(signature))
            {
                return InvokeResult.FromError("HeyGen webhook signature is required.");
            }

            if (String.IsNullOrWhiteSpace(timestamp))
            {
                return InvokeResult.FromError("HeyGen webhook timestamp is required.");
            }

            if (!Int64.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
            {
                return InvokeResult.FromError("HeyGen webhook timestamp is invalid.");
            }

            var webhookUtc = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            var timestampDelta = DateTimeOffset.UtcNow - webhookUtc;

            if (timestampDelta.Duration() > WebhookTimestampTolerance)
            {
                return InvokeResult.FromError("HeyGen webhook timestamp is outside the accepted window.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var registrationResult = await TryGetWebhookRegistrationAsync(secretOwner, secretReader);
            if (!registrationResult.Successful || registrationResult.Result == null)
            {
                return InvokeResult.FromError("HeyGen webhook registration could not be loaded.");
            }

            var expectedSignature = ComputeWebhookSignature(rawPayload, registrationResult.Result.SigningSecret);

            if (!FixedTimeEqualsHex(expectedSignature, signature))
            {
                return InvokeResult.FromError("HeyGen webhook signature is invalid.");
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult<HeyGenVideoDetails>> GetVideoAsync(string videoId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(videoId))
            {
                return InvokeResult<HeyGenVideoDetails>.FromError("HeyGen video ID is required.");
            }

            if (String.IsNullOrWhiteSpace(_settings.HeyGenApiKey))
            {
                return InvokeResult<HeyGenVideoDetails>.FromError("HeyGen API key has not been configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v3/videos/{Uri.EscapeDataString(videoId)}");

            request.Headers.Add("x-api-key", _settings.HeyGenApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<HeyGenVideoDetails>.FromError($"HeyGen video request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var videoResponse = JsonConvert.DeserializeObject<HeyGenVideoDetailsResponse>(responseContent);

            if (videoResponse?.Data == null)
            {
                return InvokeResult<HeyGenVideoDetails>.FromError("HeyGen video request completed without returning video data.");
            }

            if (String.IsNullOrWhiteSpace(videoResponse.Data.VideoId))
            {
                videoResponse.Data.VideoId = videoId;
            }

            return InvokeResult<HeyGenVideoDetails>.Create(videoResponse.Data);
        }

        private static string ComputeWebhookSignature(string rawPayload, string signingSecret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
            var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
            var signatureBytes = hmac.ComputeHash(payloadBytes);

            var result = new StringBuilder(signatureBytes.Length * 2);

            foreach (var value in signatureBytes)
            {
                result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        private static bool FixedTimeEqualsHex(string expected, string supplied)
        {
            if (String.IsNullOrWhiteSpace(expected) || String.IsNullOrWhiteSpace(supplied))
            {
                return false;
            }

            expected = expected.Trim();
            supplied = supplied.Trim();

            if (expected.Length != supplied.Length)
            {
                return false;
            }

            var difference = 0;

            for (var index = 0; index < expected.Length; index++)
            {
                difference |= Char.ToLowerInvariant(expected[index]) ^ Char.ToLowerInvariant(supplied[index]);
            }

            return difference == 0;
        }
    }
}
