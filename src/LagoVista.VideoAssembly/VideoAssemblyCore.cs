using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public interface IVideoAssemblyService
    {
        Task<VideoAssemblyResult> AssembleAsync(VideoAssemblyRequest request, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken = default);
    }

    public sealed class VideoAssemblyOptions
    {
        public string FfmpegPath { get; set; } = "ffmpeg";
        public string FfprobePath { get; set; } = "ffprobe";
        public string WorkspaceRoot { get; set; }
        public bool PreserveFailedWorkspace { get; set; }
        public int HttpTimeoutMinutes { get; set; } = 15;
        public int CallbackTimeoutSeconds { get; set; } = 30;
        public string CallbackBaseUrl { get; set; }
        public int CallbackMaxAttempts { get; set; } = 3;
        public int CallbackRetryDelaySeconds { get; set; } = 2;
        public string FontFamily { get; set; } = "Manrope";
        public string FontDirectory { get; set; }
        public int TusChunkSizeBytes { get; set; } = 16777216;
    }

    public sealed class VideoAssemblyProgress
    {
        public VideoAssemblyStage Stage { get; set; }
        public int? PercentComplete { get; set; }
        public string Message { get; set; }
        public long? BytesCompleted { get; set; }
        public long? BytesTotal { get; set; }
        public int? ProcessedDurationSeconds { get; set; }
        public int? TotalDurationSeconds { get; set; }
    }

    public sealed class VideoAssemblyRequestValidator
    {
        private static readonly Regex ColorExpression = new Regex("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        public IReadOnlyList<string> Validate(VideoAssemblyRequest request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("The video assembly request is required.");
                return errors;
            }

            if (!String.Equals(request.Version, VideoAssemblyContractVersions.Current, StringComparison.Ordinal)) errors.Add($"Unsupported contract version '{request.Version}'.");
            if (String.IsNullOrWhiteSpace(request.RequestId)) errors.Add("RequestId is required.");
            if (String.IsNullOrWhiteSpace(request.AttemptId)) errors.Add("AttemptId is required.");
            if (String.IsNullOrWhiteSpace(request.ProductionId)) errors.Add("ProductionId is required.");

            if (request.Limits == null) errors.Add("Limits are required.");
            else ValidateLimits(request.Limits, errors);

            if (request.Blocks == null || request.Blocks.Count == 0) errors.Add("At least one video assembly block is required.");
            else if (request.Limits != null && request.Blocks.Count > request.Limits.MaxBlocks) errors.Add($"The request contains {request.Blocks.Count} blocks, exceeding the limit of {request.Limits.MaxBlocks}.");
            else
            {
                for (var index = 0; index < request.Blocks.Count; index++) ValidateBlock(request.Blocks[index], index, request.Limits, errors);
            }

            var executionOptions = request.ExecutionOptions ?? new VideoAssemblyExecutionOptions();
            if (executionOptions.UploadToVimeo)
            {
                if (request.VimeoUpload == null)
                {
                    errors.Add("VimeoUpload is required when UploadToVimeo is enabled.");
                }
                else if (!String.IsNullOrWhiteSpace(request.VimeoUpload.UploadUrl))
                {
                    ValidateUrl(errors, request.VimeoUpload.UploadUrl, "VimeoUpload.UploadUrl");
                }
                else
                {
                    ValidateUrl(errors, request.VimeoUpload.SessionRequestUrl, "VimeoUpload.SessionRequestUrl");
                    if (String.IsNullOrWhiteSpace(request.VimeoUpload.SessionAccessToken)) errors.Add("VimeoUpload.SessionAccessToken is required when VimeoUpload.UploadUrl is not supplied.");
                }
            }

            if (executionOptions.SendCallbacks)
            {
                if (request.Callback == null) errors.Add("Callback is required when SendCallbacks is enabled.");
                else
                {
                    if (String.IsNullOrWhiteSpace(request.Callback.Url) && String.IsNullOrWhiteSpace(request.Callback.Path)) errors.Add("Callback.Url or Callback.Path is required when SendCallbacks is enabled.");
                    if (!String.IsNullOrWhiteSpace(request.Callback.Url)) ValidateUrl(errors, request.Callback.Url, "Callback.Url");
                    if (!String.IsNullOrWhiteSpace(request.Callback.Path) && !request.Callback.Path.StartsWith("/", StringComparison.Ordinal)) errors.Add("Callback.Path must begin with '/'.");
                    if (String.IsNullOrWhiteSpace(request.Callback.AccessToken)) errors.Add("Callback.AccessToken is required when SendCallbacks is enabled.");
                }
            }

            return errors;
        }

        private static void ValidateBlock(VideoAssemblyBlock block, int index, VideoAssemblyLimits limits, List<string> errors)
        {
            var prefix = $"Blocks[{index}]";
            if (block == null)
            {
                errors.Add($"{prefix} is required.");
                return;
            }

            if (String.IsNullOrWhiteSpace(block.Key)) errors.Add($"{prefix}.Key is required.");
            if (block.Source == null) errors.Add($"{prefix}.Source is required.");
            else ValidateUrl(errors, block.Source.Url, $"{prefix}.Source.Url");

            if (block.Type == VideoAssemblyBlockType.Image && (!block.DurationSeconds.HasValue || block.DurationSeconds.Value <= 0)) errors.Add($"{prefix}.DurationSeconds must be greater than zero for image blocks.");
            if (block.Type == VideoAssemblyBlockType.Video && block.DurationSeconds.HasValue) errors.Add($"{prefix}.DurationSeconds must be omitted for video blocks.");
            if (block.FadeInSeconds < 0) errors.Add($"{prefix}.FadeInSeconds cannot be negative.");
            if (block.FadeOutSeconds < 0) errors.Add($"{prefix}.FadeOutSeconds cannot be negative.");

            var labels = block.Labels ?? new List<VideoAssemblyTextLabel>();
            if (limits != null && labels.Count > limits.MaxLabelsPerBlock) errors.Add($"{prefix} contains {labels.Count} labels, exceeding the limit of {limits.MaxLabelsPerBlock}.");
            for (var labelIndex = 0; labelIndex < labels.Count; labelIndex++) ValidateLabel(labels[labelIndex], prefix, labelIndex, errors);
        }

        private static void ValidateLabel(VideoAssemblyTextLabel label, string blockPrefix, int index, List<string> errors)
        {
            var prefix = $"{blockPrefix}.Labels[{index}]";
            if (label == null)
            {
                errors.Add($"{prefix} is required.");
                return;
            }

            if (String.IsNullOrWhiteSpace(label.Text)) errors.Add($"{prefix}.Text is required.");
            if (label.X < 0 || label.X > 1919) errors.Add($"{prefix}.X must be between 0 and 1919.");
            if (label.Y < 0 || label.Y > 1079) errors.Add($"{prefix}.Y must be between 0 and 1079.");
            if (label.FontSize < 8 || label.FontSize > 240) errors.Add($"{prefix}.FontSize must be between 8 and 240.");
            if (!ColorExpression.IsMatch(label.Color ?? String.Empty)) errors.Add($"{prefix}.Color must use #RRGGBB format.");
            if (label.MaxWidth.HasValue && label.MaxWidth.Value <= 0) errors.Add($"{prefix}.MaxWidth must be greater than zero when supplied.");
            if (label.DelaySeconds < 0) errors.Add($"{prefix}.DelaySeconds cannot be negative.");
            if (label.VisibleDurationSeconds.HasValue && label.VisibleDurationSeconds.Value <= 0) errors.Add($"{prefix}.VisibleDurationSeconds must be greater than zero when supplied.");
            if (label.FadeInSeconds < 0) errors.Add($"{prefix}.FadeInSeconds cannot be negative.");
            if (label.FadeOutSeconds < 0) errors.Add($"{prefix}.FadeOutSeconds cannot be negative.");
        }

        private static void ValidateLimits(VideoAssemblyLimits limits, List<string> errors)
        {
            if (limits.MaxSourceFileBytes <= 0) errors.Add("Limits.MaxSourceFileBytes must be greater than zero.");
            if (limits.MaxTotalInputBytes <= 0) errors.Add("Limits.MaxTotalInputBytes must be greater than zero.");
            if (limits.MaxOutputFileBytes <= 0) errors.Add("Limits.MaxOutputFileBytes must be greater than zero.");
            if (limits.MaxOutputDurationSeconds <= 0) errors.Add("Limits.MaxOutputDurationSeconds must be greater than zero.");
            if (limits.MaxExecutionMinutes <= 0) errors.Add("Limits.MaxExecutionMinutes must be greater than zero.");
            if (limits.MaxBlocks <= 0) errors.Add("Limits.MaxBlocks must be greater than zero.");
            if (limits.MaxLabelsPerBlock <= 0) errors.Add("Limits.MaxLabelsPerBlock must be greater than zero.");
        }

        private static void ValidateUrl(List<string> errors, string value, string name)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) errors.Add($"{name} must be an absolute HTTP or HTTPS URL.");
        }
    }
}
