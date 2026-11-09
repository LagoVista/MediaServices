using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Read-only proof that a ChatGPT hosted-file URL can be fetched by MediaServices.
    /// Never stores the file or its temporary signed URL.
    /// </summary>
    public static class ChatGptFileProbe
    {
        private const long MaxBytes = 20L * 1024 * 1024;
        private static readonly string[] TrustedDomains =
            { "oaiusercontent.com", "openai.com", "chatgpt.com" };

        private static readonly HttpClient SharedClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None
        })
        {
            Timeout = TimeSpan.FromSeconds(40)
        };

        public static Task<InvokeResult<ChatGptFileProbeResult>> ProbeAsync(ChatGptFileReference file) =>
            ProbeAsync(file, SharedClient);

        // The supplied HttpClient lets tests exercise the real streaming path without a network call.
        public static async Task<InvokeResult<ChatGptFileProbeResult>> ProbeAsync(
            ChatGptFileReference file, HttpClient client)
        {
            if (file == null || String.IsNullOrWhiteSpace(file.FileId) ||
                !Uri.TryCreate(file.DownloadUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !String.IsNullOrEmpty(uri.UserInfo) ||
                uri.Port != 443 ||
                !TrustedDomains.Any(domain => uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)))
                return InvokeResult<ChatGptFileProbeResult>.FromError(
                    "A valid ChatGPT hosted file reference with an HTTPS download URL is required.");

            if (client == null)
                return InvokeResult<ChatGptFileProbeResult>.FromError("The HTTP client is unavailable.");

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    // Never follow redirects to another origin: the URL is a temporary credential.
                    if (response.StatusCode != HttpStatusCode.OK)
                        return InvokeResult<ChatGptFileProbeResult>.FromError(
                            "The hosted file could not be retrieved (HTTP " + (int)response.StatusCode + ").");

                    if (response.Content.Headers.ContentLength > MaxBytes)
                        return InvokeResult<ChatGptFileProbeResult>.FromError(
                            "The hosted file exceeds the 20 MiB probe limit.");

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                    {
                        var buffer = new byte[81920];
                        var signature = new byte[12];
                        var signatureLength = 0;
                        long size = 0;
                        int read;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            size += read;
                            if (size > MaxBytes)
                                return InvokeResult<ChatGptFileProbeResult>.FromError(
                                    "The hosted file exceeds the 20 MiB probe limit.");

                            if (signatureLength < signature.Length)
                            {
                                var length = Math.Min(signature.Length - signatureLength, read);
                                Array.Copy(buffer, 0, signature, signatureLength, length);
                                signatureLength += length;
                            }

                            sha.AppendData(buffer, 0, read);
                        }

                        var type = DetectImageType(signature, signatureLength);
                        if (type == null)
                            return InvokeResult<ChatGptFileProbeResult>.FromError(
                                "The hosted file is not a supported PNG, JPEG or WebP image.");

                        return InvokeResult<ChatGptFileProbeResult>.Create(new ChatGptFileProbeResult
                        {
                            FileId = file.FileId,
                            FileName = file.FileName,
                            ContentSize = size,
                            ContentSha256 = BitConverter.ToString(sha.GetHashAndReset()).Replace("-", "").ToLowerInvariant(),
                            DetectedMimeType = type
                        });
                    }
                }
            }
            catch (HttpRequestException)
            {
                return InvokeResult<ChatGptFileProbeResult>.FromError("Failed to retrieve the ChatGPT hosted file.");
            }
            catch (TaskCanceledException)
            {
                return InvokeResult<ChatGptFileProbeResult>.FromError("ChatGPT hosted file retrieval timed out.");
            }
            catch (IOException)
            {
                return InvokeResult<ChatGptFileProbeResult>.FromError("Failed while reading the ChatGPT hosted file.");
            }
        }

        private static string DetectImageType(byte[] signature, int length)
        {
            if (length >= 8 &&
                signature[0] == 137 && signature[1] == 80 && signature[2] == 78 &&
                signature[3] == 71 && signature[4] == 13 && signature[5] == 10 &&
                signature[6] == 26 && signature[7] == 10)
                return "image/png";

            if (length >= 3 &&
                signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF)
                return "image/jpeg";

            if (length >= 12 &&
                signature[0] == (byte)'R' && signature[1] == (byte)'I' &&
                signature[2] == (byte)'F' && signature[3] == (byte)'F' &&
                signature[8] == (byte)'W' && signature[9] == (byte)'E' &&
                signature[10] == (byte)'B' && signature[11] == (byte)'P')
                return "image/webp";

            return null;
        }
    }
}
