using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Services
{
    public class VimeoVideoService : IVimeoVideoService
    {
        private readonly HttpClient _httpClient;
        private readonly IAdminLogger _adminLogger;



        public VimeoVideoService(IHttpClientFactory httpClientFactory, IAdminLogger adminLogger)
        {
            if (httpClientFactory == null)
            {
                throw new ArgumentNullException(nameof(httpClientFactory));
            }

            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://api.vimeo.com/");
        }

        public async Task<InvokeResult<VimeoVideo>> CreatePullUploadAsync(string accessToken, VimeoPullUploadRequest uploadRequest, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(accessToken))
            {
                return InvokeResult<VimeoVideo>.FromError("The Vimeo access token is required.");
            }

            if (uploadRequest == null)
            {
                return InvokeResult<VimeoVideo>.FromError("The Vimeo upload request is required.");
            }

            var json = JsonConvert.SerializeObject(uploadRequest);

            using var request = new HttpRequestMessage(HttpMethod.Post, "me/videos");

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken.Trim());
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.vimeo.*+json;version=3.4");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUri = request.RequestUri;
            _adminLogger.Trace($"{this.Tag()} {requestUri} [{accessToken.Substring(0, 4)}****{accessToken.Substring(accessToken.Length - 4)}]");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<VimeoVideo>.FromError($"Vimeo pull upload failed with status {(int)response.StatusCode}: {content}");
            }

            var video = JsonConvert.DeserializeObject<VimeoVideo>(content);

            if (video == null || String.IsNullOrWhiteSpace(video.Uri))
            {
                return InvokeResult<VimeoVideo>.FromError("Vimeo accepted the upload request but did not return a video URI.");
            }

            return InvokeResult<VimeoVideo>.Create(video);
        }

        public async Task<InvokeResult> AddVideoToFolderAsync(string videoUri, string folderUri, string accessToken, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(videoUri))
            {
                return InvokeResult.FromError("The Vimeo video URI is required.");
            }

            if (String.IsNullOrWhiteSpace(folderUri))
            {
                return InvokeResult.FromError("The Vimeo folder URI is required.");
            }

            if (String.IsNullOrWhiteSpace(accessToken))
            {
                return InvokeResult.FromError("The Vimeo access token is required.");
            }

            var normalizedVideoUri = videoUri.TrimEnd('/');
            var normalizedFolderUri = folderUri.TrimEnd('/');

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{normalizedFolderUri}{normalizedVideoUri}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                return InvokeResult.FromError($"Vimeo could not add the video to folder '{normalizedFolderUri}'. Status: {(int)response.StatusCode}. Response: {content}");
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult<VimeoVideo>> GetVideoAsync(string accessToken, string videoUri, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(accessToken))
            {
                return InvokeResult<VimeoVideo>.FromError("Vimeo access token is required.");
            }

            if (String.IsNullOrWhiteSpace(videoUri))
            {
                return InvokeResult<VimeoVideo>.FromError("Vimeo video URI is required.");
            }

            var requestUri = videoUri.TrimStart('/');

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);

            ApplyHeaders(httpRequest, accessToken);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return InvokeResult<VimeoVideo>.FromError($"Vimeo video status request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            var video = JsonConvert.DeserializeObject<VimeoVideo>(responseContent);

            if (String.IsNullOrWhiteSpace(video?.Uri))
            {
                return InvokeResult<VimeoVideo>.FromError("Vimeo video status request completed without returning video data.");
            }

            return InvokeResult<VimeoVideo>.Create(video);
        }

        private static void ApplyHeaders(HttpRequestMessage request, string accessToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.vimeo.*+json"));
            request.Headers.TryAddWithoutValidation("Accept-Version", "3.4");
        }
    }
}