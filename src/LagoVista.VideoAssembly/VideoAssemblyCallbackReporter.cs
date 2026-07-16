using LagoVista.VideoAssembly.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoAssemblyCallbackReporter : IProgress<VideoAssemblyProgress>
    {
        private readonly object _syncRoot = new object();
        private readonly VideoAssemblyRequest _request;
        private readonly VideoProcessorCallbackClient _callbackClient;
        private readonly VideoProcessorNotificationPublisher _notificationPublisher;
        private readonly CancellationToken _cancellationToken;
        private Task _pendingCallback = Task.CompletedTask;
        private Task _pendingNotification = Task.CompletedTask;
        private VideoAssemblyStage? _lastReportedStage;
        private long _sequence;

        public VideoAssemblyCallbackReporter(VideoAssemblyRequest request, VideoProcessorCallbackClient callbackClient, VideoProcessorNotificationPublisher notificationPublisher, CancellationToken cancellationToken)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _callbackClient = callbackClient ?? throw new ArgumentNullException(nameof(callbackClient));
            _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
            _cancellationToken = cancellationToken;
        }

        public void Report(VideoAssemblyProgress progress)
        {
            if (progress == null) return;
            WriteProgress(progress);

            lock (_syncRoot)
            {
                var notificationProgress = new VideoProcessorLiveProgress
                {
                    JobType = VideoProcessorJobType.VideoAssembly,
                    RequestId = _request.RequestId,
                    AttemptId = _request.AttemptId,
                    ProductionId = _request.ProductionId,
                    MediaResourceId = _request.AzureVideoDestination?.MediaResourceId,
                    Stage = progress.Stage.ToString(),
                    PercentComplete = progress.PercentComplete,
                    Message = progress.Message,
                    BytesCompleted = progress.BytesCompleted,
                    BytesTotal = progress.BytesTotal,
                    TimestampUtc = DateTime.UtcNow.ToString("O")
                };

                _pendingNotification = _pendingNotification.ContinueWith(_ => _notificationPublisher.TryPublishAsync(_request.ProductionId, progress.Message, notificationProgress, _cancellationToken), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();

                if (_request.ExecutionOptions?.SendCallbacks != true) return;
                if (_lastReportedStage == progress.Stage) return;
                _lastReportedStage = progress.Stage;
                var callback = CreateCallback(VideoAssemblyCallbackType.Progress, progress.Stage, progress.Message);
                callback.PercentComplete = progress.PercentComplete;
                callback.BytesCompleted = progress.BytesCompleted;
                callback.BytesTotal = progress.BytesTotal;
                _pendingCallback = _pendingCallback.ContinueWith(_ => SendSafelyAsync(callback), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            }
        }

        public Task SendStartedAsync()
        {
            return SendSafelyAsync(CreateCallback(VideoAssemblyCallbackType.Started, VideoAssemblyStage.Queued, "Video assembly worker started."));
        }

        public async Task SendCompletedAsync(VideoAssemblyResult result)
        {
            await FlushAsync();
            var callback = CreateCallback(VideoAssemblyCallbackType.Completed, VideoAssemblyStage.Completed, "Video assembly completed.");
            callback.PercentComplete = 100;
            callback.Outputs = result?.Outputs ?? new System.Collections.Generic.List<VideoProcessorOutputArtifact>();
            await SendSafelyAsync(callback);
        }

        public async Task SendFailedAsync(string errorMessage, VideoAssemblyStage stage = VideoAssemblyStage.Failed)
        {
            await FlushAsync();
            var callback = CreateCallback(VideoAssemblyCallbackType.Failed, stage, "Video assembly failed.");
            callback.ErrorMessage = errorMessage;
            await SendSafelyAsync(callback);
        }

        public Task FlushAsync()
        {
            lock (_syncRoot) return Task.WhenAll(_pendingCallback, _pendingNotification);
        }

        private VideoProcessorJobCallback CreateCallback(VideoAssemblyCallbackType type, VideoAssemblyStage stage, string message)
        {
            return new VideoProcessorJobCallback
            {
                Version = _request.Version,
                JobType = VideoProcessorJobType.VideoAssembly,
                RequestId = _request.RequestId,
                AttemptId = _request.AttemptId,
                ProductionId = _request.ProductionId,
                MediaResourceId = _request.AzureVideoDestination?.MediaResourceId,
                Sequence = Interlocked.Increment(ref _sequence),
                Type = type,
                Stage = stage.ToString(),
                Message = message,
                TimestampUtc = DateTime.UtcNow.ToString("O")
            };
        }

        private async Task SendSafelyAsync(VideoProcessorJobCallback callback)
        {
            try
            {
                await _callbackClient.SendAsync(_request.Callback, callback, _cancellationToken);
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Callback delivery failed: {ex.Message}");
            }
        }

        private static void WriteProgress(VideoAssemblyProgress progress)
        {
            var percent = progress.PercentComplete.HasValue ? $" {progress.PercentComplete.Value}%" : String.Empty;
            var bytes = progress.BytesCompleted.HasValue ? $" {progress.BytesCompleted.Value}/{progress.BytesTotal?.ToString() ?? "?"} bytes" : String.Empty;
            Console.WriteLine($"[{progress.Stage}]{percent}{bytes} {progress.Message}");
        }
    }

    public sealed class VideoProcessorLiveProgress
    {
        public VideoProcessorJobType JobType { get; set; }
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public string MediaResourceId { get; set; }
        public string Stage { get; set; }
        public int? PercentComplete { get; set; }
        public string Message { get; set; }
        public long? BytesCompleted { get; set; }
        public long? BytesTotal { get; set; }
        public string TimestampUtc { get; set; }
    }
}
