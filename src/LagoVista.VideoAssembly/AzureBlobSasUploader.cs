using LagoVista.VideoAssembly.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class AzureBlobSasUploader
    {
        private readonly HttpClient _httpClient;

        public AzureBlobSasUploader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<long> UploadAsync(string filePath, VideoMediaImportDestination destination, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("The file to upload could not be found.", filePath);
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (String.IsNullOrWhiteSpace(destination.UploadUrl)) throw new InvalidOperationException("The Azure Blob SAS upload URL is required.");

            var fileInfo = new FileInfo(filePath);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var content = new StreamContent(fileStream, 131072);
            content.Headers.ContentLength = fileInfo.Length;
            content.Headers.ContentType = new MediaTypeHeaderValue(String.IsNullOrWhiteSpace(destination.ContentType) ? "application/octet-stream" : destination.ContentType);

            using var request = new HttpRequestMessage(HttpMethod.Put, destination.UploadUrl);
            request.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
            request.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Azure Blob upload failed with status {(int)response.StatusCode}: {responseContent}");
            return fileInfo.Length;
        }
    }
}
