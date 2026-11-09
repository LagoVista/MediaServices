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

        /// <summary>
        /// Native file transfer payload kept inside MediaServices, never returned to the model.
        /// </summary>
        public sealed class DownloadedImage
        {
            public byte[] Bytes { get; set; }
            public string FileName { get; set; }
            public string FileId { get; set; }
            public string ContentType { get; set; }
            public string Sha256Hex { get; set; }
        }

        public static Task<InvokeResult<ChatGptFileProbeResult>> ProbeAsync(ChatGptFileReference file) =>
            ProbeAsync(file, SharedClient);

        // The injected client lets tests exercise the actual download path without external access.
        public static async Task<InvokeResult<ChatGptFileProbeResult>> ProbeAsync(
            ChatGptFileReference file, HttpClient client)
        {
            var downloaded = await DownloadAsync(file, client);
            if (!downloaded.Successful)
                return InvokeResult<ChatGptFileProbeResult>.FromInvokeResult(downloaded.ToInvokeResult());

            var image = downloaded.Result;
            return InvokeResult<ChatGptFileProbeResult>.Create(new ChatGptFileProbeResult
            {
                FileId = image.FileId,
                FileName = image.FileName,
                ContentSize = image.Bytes.LongLength,
                ContentSha256 = image.Sha256Hex,
                DetectedMimeType = image.ContentType
            });
        }

        public static Task<InvokeResult<DownloadedImage>> DownloadAsync(ChatGptFileReference file) =>
            DownloadAsync(file, SharedClient);

        public static async Task<InvokeResult<DownloadedImage>> DownloadAsync(
            ChatGptFileReference file, HttpClient client)
        {
            // Never emit a signed URL or its query string in diagnostics.
            if (file == null)
                return InvokeResult<DownloadedImage>.FromError(
                    "ChatGPT did not supply a structured native file reference.");
            if (String.IsNullOrWhiteSpace(file.FileId))
                return InvokeResult<DownloadedImage>.FromError(
                    "ChatGPT native file reference is missing file_id.");
            if (String.IsNullOrWhiteSpace(file.DownloadUrl))
                return InvokeResult<DownloadedImage>.FromError(
                    "ChatGPT native file reference is missing download_url.");
            if (!Uri.TryCreate(file.DownloadUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !String.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443)
                return InvokeResult<DownloadedImage>.FromError(
                    "ChatGPT native file reference has an invalid HTTPS download URL.");
            if (!String.Equals(uri.Host, "oaisdmntprcentralus.blob.core.windows.net", StringComparison.OrdinalIgnoreCase) &&
                !TrustedDomains.Any(domain => uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)))
                return InvokeResult<DownloadedImage>.FromError(
                    "ChatGPT file host is not allowlisted: " + uri.Host + ".");
            if (client == null)
                return InvokeResult<DownloadedImage>.FromError("The HTTP client is unavailable.");

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    // The signed URL must not redirect to a different origin.
                    if (response.StatusCode != HttpStatusCode.OK)
                        return InvokeResult<DownloadedImage>.FromError(
                            "The hosted file could not be retrieved (HTTP " + (int)response.StatusCode + ").");
                    if (response.Content.Headers.ContentLength > MaxBytes)
                        return InvokeResult<DownloadedImage>.FromError(
                            "The hosted file exceeds the 20 MiB limit.");

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var output = new MemoryStream())
                    using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                    {
                        var buffer = new byte[81920];
                        var signature = new byte[12];
                        var signatureLength = 0;
                        long total = 0;
                        int read;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            total += read;
                            if (total > MaxBytes)
                                return InvokeResult<DownloadedImage>.FromError(
                                    "The hosted file exceeds the 20 MiB limit.");

                            if (signatureLength < signature.Length)
                            {
                                var length = Math.Min(signature.Length - signatureLength, read);
                                Array.Copy(buffer, 0, signature, signatureLength, length);
                                signatureLength += length;
                            }

                            sha.AppendData(buffer, 0, read);
                            output.Write(buffer, 0, read);
                        }

                        var contentType = DetectImageType(signature, signatureLength);
                        if (contentType == null)
                            return InvokeResult<DownloadedImage>.FromError(
                                "The hosted file is not a supported PNG, JPEG or WebP image.");

                        var extension = contentType == "image/png" ? ".png" :
                            contentType == "image/jpeg" ? ".jpg" : ".webp";
                        var fileName = Path.GetFileName(file.FileName);
                        if (String.IsNullOrWhiteSpace(fileName))
                            fileName = "chatgpt-image" + extension;
                        else if (String.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                            fileName += extension;

                        return InvokeResult<DownloadedImage>.Create(new DownloadedImage
                        {
                            Bytes = output.ToArray(),
                            FileId = file.FileId,
                            FileName = fileName,
                            ContentType = contentType,
                            Sha256Hex = BitConverter.ToString(sha.GetHashAndReset()).Replace("-", "").ToLowerInvariant()
                        });
                    }
                }
            }
            catch (HttpRequestException)
            {
                return InvokeResult<DownloadedImage>.FromError("Failed to retrieve the ChatGPT hosted file.");
            }
            catch (TaskCanceledException)
            {
                return InvokeResult<DownloadedImage>.FromError("ChatGPT hosted file retrieval timed out.");
            }
            catch (IOException)
            {
                return InvokeResult<DownloadedImage>.FromError("Failed while reading the ChatGPT hosted file.");
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
