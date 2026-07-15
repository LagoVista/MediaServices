using System.Collections.Generic;

namespace LagoVista.VideoAssembly.Contracts
{
    public static class VideoAssemblyContractVersions
    {
        public const string Current = "2.0";
    }

    public enum VideoAssemblyBlockType
    {
        Video,
        Image
    }

    public enum VideoAssemblyTextAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VideoAssemblyStage
    {
        Queued,
        DownloadingMedia,
        InspectingMedia,
        RenderingLabels,
        NormalizingMedia,
        Encoding,
        UploadingToVimeo,
        Completed,
        Failed
    }

    public enum VideoAssemblyCallbackType
    {
        Started,
        Progress,
        Completed,
        Failed
    }

    public sealed class VideoAssemblyRequest
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public List<VideoAssemblyBlock> Blocks { get; set; } = new List<VideoAssemblyBlock>();
        public VideoAssemblyVimeoUpload VimeoUpload { get; set; }
        public VideoAssemblyCallbackSettings Callback { get; set; }
        public VideoAssemblyLimits Limits { get; set; } = new VideoAssemblyLimits();
        public VideoAssemblyExecutionOptions ExecutionOptions { get; set; } = new VideoAssemblyExecutionOptions();
    }

    public sealed class VideoAssemblyExecutionRequest
    {
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string RequestUrl { get; set; }
    }

    public sealed class VideoAssemblyBlock
    {
        public string Key { get; set; }
        public VideoAssemblyBlockType Type { get; set; }
        public VideoAssemblySource Source { get; set; }
        public double? DurationSeconds { get; set; }
        public double FadeInSeconds { get; set; }
        public double FadeOutSeconds { get; set; }
        public List<VideoAssemblyTextLabel> Labels { get; set; } = new List<VideoAssemblyTextLabel>();
    }

    public sealed class VideoAssemblySource
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public sealed class VideoAssemblyTextLabel
    {
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int FontSize { get; set; } = 48;
        public bool Bold { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public VideoAssemblyTextAlignment Alignment { get; set; } = VideoAssemblyTextAlignment.Left;
        public int? MaxWidth { get; set; }
        public double DelaySeconds { get; set; }
        public double? VisibleDurationSeconds { get; set; }
        public double FadeInSeconds { get; set; }
        public double FadeOutSeconds { get; set; }
    }

    public sealed class VideoAssemblyVimeoUpload
    {
        public string UploadUrl { get; set; }
        public string VideoUri { get; set; }
        public string VideoId { get; set; }
    }

    public sealed class VideoAssemblyCallbackSettings
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
    }

    public sealed class VideoAssemblyLimits
    {
        public long MaxSourceFileBytes { get; set; } = 4294967296;
        public long MaxTotalInputBytes { get; set; } = 4294967296;
        public long MaxOutputFileBytes { get; set; } = 4294967296;
        public int MaxOutputDurationSeconds { get; set; } = 600;
        public int MaxExecutionMinutes { get; set; } = 30;
        public int MaxBlocks { get; set; } = 50;
        public int MaxLabelsPerBlock { get; set; } = 20;
    }

    public sealed class VideoAssemblyExecutionOptions
    {
        public bool UploadToVimeo { get; set; } = true;
        public bool SendCallbacks { get; set; } = true;
        public bool PreserveOutputFile { get; set; }
    }

    public sealed class VideoAssemblyCallback
    {
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public long Sequence { get; set; }
        public VideoAssemblyCallbackType Type { get; set; }
        public VideoAssemblyStage Stage { get; set; }
        public int? PercentComplete { get; set; }
        public string Message { get; set; }
        public long? BytesCompleted { get; set; }
        public long? BytesTotal { get; set; }
        public int? ProcessedDurationSeconds { get; set; }
        public int? TotalDurationSeconds { get; set; }
        public string VimeoVideoUri { get; set; }
        public string VimeoVideoId { get; set; }
        public long? OutputSizeBytes { get; set; }
        public int? OutputDurationSeconds { get; set; }
        public string Sha256 { get; set; }
        public string ErrorMessage { get; set; }
        public string TimestampUtc { get; set; }
    }

    public sealed class VideoAssemblyResult
    {
        public bool Successful { get; set; }
        public string OutputFilePath { get; set; }
        public string VimeoVideoUri { get; set; }
        public string VimeoVideoId { get; set; }
        public long? OutputSizeBytes { get; set; }
        public int? OutputDurationSeconds { get; set; }
        public string Sha256 { get; set; }
        public string ErrorMessage { get; set; }
    }
}
