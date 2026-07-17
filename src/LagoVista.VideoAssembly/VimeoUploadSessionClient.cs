using LagoVista.VideoAssembly.Contracts;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class VimeoUploadSessionClient
    {
        private readonly HttpClient _httpClient;
        private readonly VideoAssemblyOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public VimeoUploadSessionClient(HttpClient httpClient, VideoAssemblyOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<VideoAssemblyVimeoSessionResponse> CreateSessionAsync(VideoAssemblyRequest request, long outputSizeBytes, int outputDurationSeconds, string sha256, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.VimeoUpload == null) throw new InvalidOperationException("Vimeo upload settings are required.");
            if (String.IsNullOrWhiteSpace(request.VimeoUpload.SessionRequestUrl)) throw new InvalidOperationException("The Vimeo session request URL is required.");
            if (String.IsNullOrWhiteSpace(request.VimeoUpload.SessionAccessToken)) throw new InvalidOperationException("The Vimeo session access token is required.");

            var sessionRequest = new VideoAssemblyVimeoSessionRequest
            {
                RequestId = request.RequestId,
                AttemptId = request.AttemptId,
                ProductionId = request.ProductionId,
                OutputSizeBytes = outputSizeBytes,
                OutputDurationSeconds = outputDurationSeconds,
                Sha256 = sha256
            };

            var json = JsonSerializer.Serialize(sessionRequest, _jsonOptions);
            var sessionRequestUrl = ResolveSessionRequestUrl(request.VimeoUpload.SessionRequestUrl);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, sessionRequestUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.VimeoUpload.SessionAccessToken);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"The Vimeo upload session request failed with status {(int)response.StatusCode}: {responseContent}");

            var result = JsonSerializer.Deserialize<VideoAssemblyVimeoSessionResponse>(responseContent, _jsonOptions);
            if (result == null || String.IsNullOrWhiteSpace(result.UploadUrl)) throw new InvalidOperationException("The Vimeo upload session response did not contain an upload URL.");
            if (String.IsNullOrWhiteSpace(result.VideoUri)) throw new InvalidOperationException("The Vimeo upload session response did not contain a video URI.");
            return result;
        }

        private string ResolveSessionRequestUrl(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("The Vimeo session request path is required.");
            if (!path.StartsWith("/", StringComparison.Ordinal)) throw new InvalidOperationException("The Vimeo session request path must begin with '/'.");
            if (!Uri.TryCreate(_options.CallbackBaseUrl, UriKind.Absolute, out var baseUrl)) throw new InvalidOperationException("VideoAssembly.CallbackBaseUrl must be an absolute URL when a Vimeo session path is used.");

            return new Uri(baseUrl, path).ToString();
        }
    }
}
