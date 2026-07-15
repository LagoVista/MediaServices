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
    public sealed class VideoProcessorCallbackClient
    {
        private readonly HttpClient _httpClient;
        private readonly VideoAssemblyOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public VideoProcessorCallbackClient(HttpClient httpClient, VideoAssemblyOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task SendAsync(VideoProcessorCallbackSettings settings, VideoProcessorJobCallback callback, CancellationToken cancellationToken = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var callbackUrl = ResolveCallbackUrl(settings);
            var attempts = Math.Max(1, _options.CallbackMaxAttempts);
            Exception lastException = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    var json = JsonSerializer.Serialize(callback, _jsonOptions);
                    using var request = new HttpRequestMessage(HttpMethod.Post, callbackUrl);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (response.IsSuccessStatusCode) return;

                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    lastException = new InvalidOperationException($"Video processor callback failed with status {(int)response.StatusCode}: {responseContent}");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                if (attempt < attempts) await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.CallbackRetryDelaySeconds)), cancellationToken);
            }

            throw new InvalidOperationException($"Video processor callback failed after {attempts} attempt(s).", lastException);
        }

        private string ResolveCallbackUrl(VideoProcessorCallbackSettings settings)
        {
            if (Uri.TryCreate(settings.Url, UriKind.Absolute, out var absoluteUrl)) return absoluteUrl.ToString();
            if (String.IsNullOrWhiteSpace(settings.Path)) throw new InvalidOperationException("Callback.Url or Callback.Path is required.");
            if (!Uri.TryCreate(_options.CallbackBaseUrl, UriKind.Absolute, out var baseUrl)) throw new InvalidOperationException("VideoAssembly.CallbackBaseUrl must be an absolute URL when Callback.Path is used.");
            return new Uri(baseUrl, settings.Path).ToString();
        }
    }
}
