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
        UploadingToAzure,
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

    public enum VideoProcessorJobType
    {
        VideoAssembly = 1,
        VideoMediaImport = 2
    }

    public enum VideoProcessorOutputArtifactType
    {
        Video = 1,
        Thumbnail = 2,
        Caption = 3,
        Other = 99
    }

    public enum VideoMediaImportStage
    {
        Queued,
        DownloadingSource,
        InspectingSource,
        GeneratingThumbnail,
        UploadingVideo,
        UploadingThumbnail,
        Completed,
        Failed
    }

    public sealed class VideoAssemblyRequest
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public VideoProcessorJobType JobType { get; set; } = VideoProcessorJobType.VideoAssembly;
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public List<VideoAssemblyBlock> Blocks { get; set; } = new List<VideoAssemblyBlock>();
        public VideoMediaImportDestination AzureVideoDestination { get; set; }
        public VideoAssemblyVimeoUpload VimeoUpload { get; set; }
        public VideoAssemblyCallbackSettings Callback { get; set; }
        public VideoAssemblyLimits Limits { get; set; } = new VideoAssemblyLimits();
        public VideoAssemblyExecutionOptions ExecutionOptions { get; set; } = new VideoAssemblyExecutionOptions();
    }

    public sealed class VideoProcessorExecutionRequest
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public VideoProcessorJobType JobType { get; set; }
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string RequestUrl { get; set; }
    }

    public sealed class VideoAssemblyExecutionRequest
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public VideoProcessorJobType JobType { get; set; } = VideoProcessorJobType.VideoAssembly;
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
        public string MediaResourceId { get; set; }
        public string UploadUrl { get; set; }
        public string SessionRequestUrl { get; set; }
        public string SessionAccessToken { get; set; }
        public string VideoUri { get; set; }
        public string VideoId { get; set; }
    }

    public sealed class VideoAssemblyVimeoSessionRequest
    {
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public long OutputSizeBytes { get; set; }
        public int OutputDurationSeconds { get; set; }
        public string Sha256 { get; set; }
    }

    public sealed class VideoAssemblyVimeoSessionResponse
    {
        public string UploadUrl { get; set; }
        public string VideoUri { get; set; }
        public string VideoId { get; set; }
    }

    public class VideoProcessorCallbackSettings
    {
        public string Url { get; set; }
        public string Path { get; set; }
        public string AccessToken { get; set; }
    }

    public sealed class VideoAssemblyCallbackSettings : VideoProcessorCallbackSettings
    {
    }

    public sealed class VideoMediaImportRequest
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public VideoProcessorJobType JobType { get; set; } = VideoProcessorJobType.VideoMediaImport;
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public string MediaResourceId { get; set; }
        public VideoAssemblySource Source { get; set; }
        public VideoMediaImportDestination VideoDestination { get; set; }
        public VideoMediaImportThumbnail Thumbnail { get; set; } = new VideoMediaImportThumbnail();
        public VideoProcessorCallbackSettings Callback { get; set; }
        public VideoMediaImportLimits Limits { get; set; } = new VideoMediaImportLimits();
        public VideoMediaImportExecutionOptions ExecutionOptions { get; set; } = new VideoMediaImportExecutionOptions();
    }

    public sealed class VideoMediaImportDestination
    {
        public string MediaResourceId { get; set; }
        public string UploadUrl { get; set; }
        public string StorageReferenceName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public sealed class VideoMediaImportThumbnail
    {
        public bool Enabled { get; set; } = true;
        public double? TimeSeconds { get; set; }
        public VideoMediaImportDestination Destination { get; set; }
    }

    public sealed class VideoMediaImportLimits
    {
        public long MaxSourceFileBytes { get; set; } = 4294967296;
        public int MaxExecutionMinutes { get; set; } = 30;
    }

    public sealed class VideoMediaImportExecutionOptions
    {
        public bool GenerateThumbnail { get; set; } = true;
        public bool SendCallbacks { get; set; } = true;
        public bool PreserveDownloadedFile { get; set; }
    }

    public sealed class VideoProcessorJobCallback
    {
        public string Version { get; set; } = VideoAssemblyContractVersions.Current;
        public VideoProcessorJobType JobType { get; set; }
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public string MediaResourceId { get; set; }
        public long Sequence { get; set; }
        public VideoAssemblyCallbackType Type { get; set; }
        public string Stage { get; set; }
        public int? PercentComplete { get; set; }
        public string Message { get; set; }
        public long? BytesCompleted { get; set; }
        public long? BytesTotal { get; set; }
        public List<VideoProcessorOutputArtifact> Outputs { get; set; } = new List<VideoProcessorOutputArtifact>();
        public string ErrorMessage { get; set; }
        public string TimestampUtc { get; set; }
    }

    public sealed class VideoProcessorOutputArtifact
    {
        public VideoProcessorOutputArtifactType Type { get; set; }
        public string MediaResourceId { get; set; }
        public string StorageReferenceName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public int? DurationSeconds { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Sha256 { get; set; }
        public string ExternalUri { get; set; }
        public string ExternalId { get; set; }
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
        public bool UploadToAzure { get; set; } = true;
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
        public List<VideoProcessorOutputArtifact> Outputs { get; set; } = new List<VideoProcessorOutputArtifact>();
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
        public List<VideoProcessorOutputArtifact> Outputs { get; set; } = new List<VideoProcessorOutputArtifact>();
        public string VimeoVideoUri { get; set; }
        public string VimeoVideoId { get; set; }
        public long? OutputSizeBytes { get; set; }
        public int? OutputDurationSeconds { get; set; }
        public string Sha256 { get; set; }
        public string ErrorMessage { get; set; }
    }
}
