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
        private readonly VideoAssemblyCallbackClient _callbackClient;
        private readonly CancellationToken _cancellationToken;
        private Task _pendingCallback = Task.CompletedTask;
        private VideoAssemblyStage? _lastReportedStage;
        private long _sequence;

        public VideoAssemblyCallbackReporter(VideoAssemblyRequest request, VideoAssemblyCallbackClient callbackClient, CancellationToken cancellationToken)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _callbackClient = callbackClient ?? throw new ArgumentNullException(nameof(callbackClient));
            _cancellationToken = cancellationToken;
        }

        public void Report(VideoAssemblyProgress progress)
        {
            if (progress == null) return;
            WriteProgress(progress);
            if (_request.ExecutionOptions?.SendCallbacks != true) return;

            lock (_syncRoot)
            {
                if (_lastReportedStage == progress.Stage) return;
                _lastReportedStage = progress.Stage;
                var callback = CreateCallback(VideoAssemblyCallbackType.Progress, progress.Stage, progress.Message);
                callback.BytesCompleted = progress.BytesCompleted;
                callback.BytesTotal = progress.BytesTotal;
                callback.ProcessedDurationSeconds = progress.ProcessedDurationSeconds;
                callback.TotalDurationSeconds = progress.TotalDurationSeconds;
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
            callback.VimeoVideoUri = result?.VimeoVideoUri;
            callback.VimeoVideoId = result?.VimeoVideoId;
            callback.OutputSizeBytes = result?.OutputSizeBytes;
            callback.OutputDurationSeconds = result?.OutputDurationSeconds;
            callback.Sha256 = result?.Sha256;
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
            lock (_syncRoot) return _pendingCallback;
        }

        private VideoAssemblyCallback CreateCallback(VideoAssemblyCallbackType type, VideoAssemblyStage stage, string message)
        {
            return new VideoAssemblyCallback
            {
                RequestId = _request.RequestId,
                AttemptId = _request.AttemptId,
                ProductionId = _request.ProductionId,
                Sequence = Interlocked.Increment(ref _sequence),
                Type = type,
                Stage = stage,
                Message = message,
                TimestampUtc = DateTime.UtcNow.ToString("O")
            };
        }

        private async Task SendSafelyAsync(VideoAssemblyCallback callback)
        {
            try
            {
                await _callbackClient.SendAsync(_request, callback, _cancellationToken);
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
}
