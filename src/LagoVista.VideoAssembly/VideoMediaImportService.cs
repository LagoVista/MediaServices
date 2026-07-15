using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoMediaImportResult
    {
        public bool Successful { get; set; }
        public List<VideoProcessorOutputArtifact> Outputs { get; set; } = new List<VideoProcessorOutputArtifact>();
        public string ErrorMessage { get; set; }
    }

    public sealed class VideoMediaImportService
    {
        private readonly HttpClient _httpClient;
        private readonly FfprobeMediaInspectionService _inspectionService;
        private readonly VideoThumbnailExtractor _thumbnailExtractor;
        private readonly AzureBlobSasUploader _azureUploader;
        private readonly VideoProcessorCallbackClient _callbackClient;
        private readonly VideoProcessorNotificationPublisher _notificationPublisher;
        private readonly VideoAssemblyOptions _options;
        private readonly object _notificationSyncRoot = new object();
        private Task _pendingUploadNotification = Task.CompletedTask;
        private long _sequence;

        public VideoMediaImportService(HttpClient httpClient, FfprobeMediaInspectionService inspectionService, VideoThumbnailExtractor thumbnailExtractor, AzureBlobSasUploader azureUploader, VideoProcessorCallbackClient callbackClient, VideoProcessorNotificationPublisher notificationPublisher, VideoAssemblyOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));
            _thumbnailExtractor = thumbnailExtractor ?? throw new ArgumentNullException(nameof(thumbnailExtractor));
            _azureUploader = azureUploader ?? throw new ArgumentNullException(nameof(azureUploader));
            _callbackClient = callbackClient ?? throw new ArgumentNullException(nameof(callbackClient));
            _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<VideoMediaImportResult> ExecuteAsync(VideoMediaImportRequest request, CancellationToken cancellationToken = default)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(request.Limits.MaxExecutionMinutes));

            var workspaceRoot = String.IsNullOrWhiteSpace(_options.WorkspaceRoot) ? Path.Combine(Path.GetTempPath(), "lago-video-assembly") : _options.WorkspaceRoot;
            var workspacePath = Path.Combine(workspaceRoot, Sanitize(request.RequestId), Sanitize(request.AttemptId), "media-import");
            var sourcePath = Path.Combine(workspacePath, String.IsNullOrWhiteSpace(request.Source.FileName) ? "source.mp4" : Sanitize(request.Source.FileName));
            var thumbnailPath = Path.Combine(workspacePath, String.IsNullOrWhiteSpace(request.Thumbnail?.Destination?.FileName) ? "thumbnail.jpg" : Sanitize(request.Thumbnail.Destination.FileName));
            Directory.CreateDirectory(workspacePath);

            var outputs = new List<VideoProcessorOutputArtifact>();
            var currentStage = VideoMediaImportStage.Queued;

            try
            {
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Started, currentStage, "Video media import started.", outputs, null, timeout.Token);

                currentStage = VideoMediaImportStage.DownloadingSource;
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Progress, currentStage, "Downloading source video.", outputs, null, timeout.Token);
                var sourceSize = await DownloadAsync(request.Source.Url, sourcePath, request.Limits.MaxSourceFileBytes, timeout.Token);

                currentStage = VideoMediaImportStage.InspectingSource;
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Progress, currentStage, "Inspecting source video.", outputs, null, timeout.Token);
                var inspection = await _inspectionService.InspectAsync(sourcePath, timeout.Token);
                var videoSha256 = await CalculateSha256Async(sourcePath, timeout.Token);

                currentStage = VideoMediaImportStage.UploadingVideo;
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Progress, currentStage, "Uploading source video to Azure.", outputs, null, timeout.Token);
                await _azureUploader.UploadAsync(sourcePath, request.VideoDestination, timeout.Token, CreateUploadProgress(request, currentStage, "Uploading source video to Azure."));
                await FlushUploadNotificationsAsync();

                outputs.Add(new VideoProcessorOutputArtifact
                {
                    Type = VideoProcessorOutputArtifactType.Video,
                    MediaResourceId = request.MediaResourceId,
                    StorageReferenceName = request.VideoDestination.StorageReferenceName,
                    FileName = request.VideoDestination.FileName,
                    ContentType = request.VideoDestination.ContentType,
                    SizeBytes = sourceSize,
                    DurationSeconds = (int)Math.Round(inspection.DurationSeconds),
                    Width = inspection.Width,
                    Height = inspection.Height,
                    Sha256 = videoSha256
                });

                var generateThumbnail = request.ExecutionOptions?.GenerateThumbnail != false && request.Thumbnail?.Enabled != false;
                if (generateThumbnail)
                {
                    currentStage = VideoMediaImportStage.GeneratingThumbnail;
                    await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Progress, currentStage, "Generating video thumbnail.", outputs, null, timeout.Token);
                    var thumbnailTime = request.Thumbnail.TimeSeconds ?? 1.0;
                    await _thumbnailExtractor.ExtractAsync(sourcePath, thumbnailPath, thumbnailTime, inspection.DurationSeconds, timeout.Token);
                    var thumbnailInspection = await _inspectionService.InspectAsync(thumbnailPath, timeout.Token);
                    var thumbnailSha256 = await CalculateSha256Async(thumbnailPath, timeout.Token);

                    currentStage = VideoMediaImportStage.UploadingThumbnail;
                    await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Progress, currentStage, "Uploading video thumbnail to Azure.", outputs, null, timeout.Token);
                    var thumbnailSize = await _azureUploader.UploadAsync(thumbnailPath, request.Thumbnail.Destination, timeout.Token, CreateUploadProgress(request, currentStage, "Uploading video thumbnail to Azure."));
                    await FlushUploadNotificationsAsync();

                    outputs.Add(new VideoProcessorOutputArtifact
                    {
                        Type = VideoProcessorOutputArtifactType.Thumbnail,
                        MediaResourceId = request.MediaResourceId,
                        StorageReferenceName = request.Thumbnail.Destination.StorageReferenceName,
                        FileName = request.Thumbnail.Destination.FileName,
                        ContentType = request.Thumbnail.Destination.ContentType,
                        SizeBytes = thumbnailSize,
                        Width = thumbnailInspection.Width,
                        Height = thumbnailInspection.Height,
                        Sha256 = thumbnailSha256
                    });
                }

                currentStage = VideoMediaImportStage.Completed;
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Completed, currentStage, "Video media import completed.", outputs, null, timeout.Token);
                return new VideoMediaImportResult { Successful = true, Outputs = outputs };
            }
            catch (Exception ex)
            {
                await SendCallbackSafelyAsync(request, VideoAssemblyCallbackType.Failed, VideoMediaImportStage.Failed, "Video media import failed.", outputs, ex.Message, CancellationToken.None);
                return new VideoMediaImportResult { Successful = false, Outputs = outputs, ErrorMessage = ex.Message };
            }
            finally
            {
                if (request.ExecutionOptions?.PreserveDownloadedFile != true && Directory.Exists(workspacePath))
                {
                    try
                    {
                        Directory.Delete(workspacePath, true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task<long> DownloadAsync(string sourceUrl, string destinationPath, long maxFileBytes, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxFileBytes) throw new InvalidOperationException($"Source video declares a size of {contentLength.Value} bytes, exceeding the limit of {maxFileBytes} bytes.");

            using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[131072];
            long totalBytes = 0;

            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0) break;
                totalBytes += bytesRead;
                if (totalBytes > maxFileBytes) throw new InvalidOperationException($"Source video exceeded the limit of {maxFileBytes} bytes while downloading.");
                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            await destinationStream.FlushAsync(cancellationToken);
            return totalBytes;
        }

        private IProgress<AzureBlobUploadProgress> CreateUploadProgress(VideoMediaImportRequest request, VideoMediaImportStage stage, string message)
        {
            return new InlineProgress<AzureBlobUploadProgress>(upload =>
            {
                Console.WriteLine($"[{stage}] {upload.PercentComplete}% {upload.BytesCompleted}/{upload.BytesTotal} bytes {message}");

                var liveProgress = new VideoProcessorLiveProgress
                {
                    JobType = VideoProcessorJobType.VideoMediaImport,
                    RequestId = request.RequestId,
                    AttemptId = request.AttemptId,
                    ProductionId = request.ProductionId,
                    MediaResourceId = request.MediaResourceId,
                    Stage = stage.ToString(),
                    PercentComplete = upload.PercentComplete,
                    Message = message,
                    BytesCompleted = upload.BytesCompleted,
                    BytesTotal = upload.BytesTotal,
                    TimestampUtc = DateTime.UtcNow.ToString("O")
                };

                lock (_notificationSyncRoot)
                {
                    _pendingUploadNotification = _pendingUploadNotification.ContinueWith(_ => _notificationPublisher.TryPublishAsync(request.ProductionId, message, liveProgress, CancellationToken.None), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
                }
            });
        }

        private Task FlushUploadNotificationsAsync()
        {
            lock (_notificationSyncRoot) return _pendingUploadNotification;
        }

        private async Task SendCallbackSafelyAsync(VideoMediaImportRequest request, VideoAssemblyCallbackType type, VideoMediaImportStage stage, string message, List<VideoProcessorOutputArtifact> outputs, string errorMessage, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[{stage}] {message}");

            await _notificationPublisher.TryPublishAsync(request.ProductionId, message, new VideoProcessorLiveProgress
            {
                JobType = VideoProcessorJobType.VideoMediaImport,
                RequestId = request.RequestId,
                AttemptId = request.AttemptId,
                ProductionId = request.ProductionId,
                MediaResourceId = request.MediaResourceId,
                Stage = stage.ToString(),
                Message = message,
                TimestampUtc = DateTime.UtcNow.ToString("O")
            }, cancellationToken);

            if (request.ExecutionOptions?.SendCallbacks != true) return;

            try
            {
                await _callbackClient.SendAsync(request.Callback, new VideoProcessorJobCallback
                {
                    Version = request.Version,
                    JobType = VideoProcessorJobType.VideoMediaImport,
                    RequestId = request.RequestId,
                    AttemptId = request.AttemptId,
                    ProductionId = request.ProductionId,
                    MediaResourceId = request.MediaResourceId,
                    Sequence = Interlocked.Increment(ref _sequence),
                    Type = type,
                    Stage = stage.ToString(),
                    Message = message,
                    Outputs = new List<VideoProcessorOutputArtifact>(outputs),
                    ErrorMessage = errorMessage,
                    TimestampUtc = DateTime.UtcNow.ToString("O")
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Callback delivery failed: {ex.Message}");
            }
        }

        private static async Task<string> CalculateSha256Async(string filePath, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string Sanitize(string value)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars()) value = value.Replace(invalidCharacter, '_');
            return value;
        }
    }
}
