using LagoVista.VideoAssembly.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class DownloadedVideoAssemblySource
    {
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; }
    }

    public sealed class VideoAssemblySourceDownloader
    {
        private readonly HttpClient _httpClient;

        public VideoAssemblySourceDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<DownloadedVideoAssemblySource> DownloadAsync(VideoAssemblySource source, string destinationPath, long maxFileBytes, VideoAssemblyStage stage, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (String.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            progress?.Report(new VideoAssemblyProgress { Stage = stage, Message = $"Downloading {source.FileName ?? "video source"}." });
            using var response = await _httpClient.GetAsync(source.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxFileBytes) throw new InvalidOperationException($"Source '{source.FileName}' declares a size of {contentLength.Value} bytes, exceeding the limit of {maxFileBytes} bytes.");

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            try
            {
                using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var buffer = new byte[131072];
                long bytesCompleted = 0;
                while (true)
                {
                    var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (bytesRead == 0) break;
                    bytesCompleted += bytesRead;
                    if (bytesCompleted > maxFileBytes) throw new InvalidOperationException($"Source '{source.FileName}' exceeded the limit of {maxFileBytes} bytes while downloading.");
                    await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    progress?.Report(new VideoAssemblyProgress { Stage = stage, PercentComplete = CalculatePercent(bytesCompleted, contentLength), Message = $"Downloading {source.FileName ?? "video source"}.", BytesCompleted = bytesCompleted, BytesTotal = contentLength });
                }

                await destinationStream.FlushAsync(cancellationToken);
                return new DownloadedVideoAssemblySource { FilePath = destinationPath, SizeBytes = bytesCompleted, ContentType = response.Content.Headers.ContentType?.MediaType ?? source.ContentType };
            }
            catch
            {
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                throw;
            }
        }

        private static int? CalculatePercent(long completed, long? total)
        {
            if (!total.HasValue || total.Value <= 0) return null;
            return (int)Math.Min(100, completed * 100L / total.Value);
        }
    }
}
