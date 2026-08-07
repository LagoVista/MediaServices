using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class FfmpegVideoAssemblyService : IVideoAssemblyService
    {
        private const string BaseVideoFilter = "scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:black,fps=30,format=yuv420p";
        private readonly VideoAssemblyWorkspaceFactory _workspaceFactory;
        private readonly VideoAssemblySourceDownloader _sourceDownloader;
        private readonly FfprobeMediaInspectionService _inspectionService;
        private readonly ProcessRunner _processRunner;
        private readonly AssSubtitleDocumentBuilder _subtitleBuilder;
        private readonly AzureBlobSasUploader _azureBlobSasUploader;
        private readonly VideoThumbnailExtractor _thumbnailExtractor;
        private readonly VimeoUploadSessionClient _vimeoUploadSessionClient;
        private readonly TusVideoUploader _tusVideoUploader;
        private readonly VideoAssemblyOptions _options;

        public FfmpegVideoAssemblyService(VideoAssemblyWorkspaceFactory workspaceFactory, VideoAssemblySourceDownloader sourceDownloader, FfprobeMediaInspectionService inspectionService, ProcessRunner processRunner, AssSubtitleDocumentBuilder subtitleBuilder, AzureBlobSasUploader azureBlobSasUploader, VideoThumbnailExtractor thumbnailExtractor, VimeoUploadSessionClient vimeoUploadSessionClient, TusVideoUploader tusVideoUploader, VideoAssemblyOptions options)
        {
            _workspaceFactory = workspaceFactory ?? throw new ArgumentNullException(nameof(workspaceFactory));
            _sourceDownloader = sourceDownloader ?? throw new ArgumentNullException(nameof(sourceDownloader));
            _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _subtitleBuilder = subtitleBuilder ?? throw new ArgumentNullException(nameof(subtitleBuilder));
            _azureBlobSasUploader = azureBlobSasUploader ?? throw new ArgumentNullException(nameof(azureBlobSasUploader));
            _thumbnailExtractor = thumbnailExtractor ?? throw new ArgumentNullException(nameof(thumbnailExtractor));
            _vimeoUploadSessionClient = vimeoUploadSessionClient ?? throw new ArgumentNullException(nameof(vimeoUploadSessionClient));
            _tusVideoUploader = tusVideoUploader ?? throw new ArgumentNullException(nameof(tusVideoUploader));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<VideoAssemblyResult> AssembleAsync(VideoAssemblyRequest request, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken = default)
        {
            if (request.ExecutionOptions?.Operation == VideoAssemblyOperation.Publish) return await PublishAsync(request, progress, cancellationToken);

            using var executionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            executionTimeout.CancelAfter(TimeSpan.FromMinutes(request.Limits.MaxExecutionMinutes));
            using var workspace = _workspaceFactory.Create(request);
            workspace.Preserve = request.ExecutionOptions?.PreserveOutputFile == true;

            try
            {
                var segments = new List<VideoAssemblySegment>();
                long totalInputBytes = 0;
                double totalDurationSeconds = 0;

                for (var index = 0; index < request.Blocks.Count; index++)
                {
                    var segment = await DownloadAndInspectAsync(request.Blocks[index], index, workspace, request, progress, executionTimeout.Token);
                    totalInputBytes += segment.SourceSizeBytes;
                    totalDurationSeconds += segment.DurationSeconds;

                    if (totalInputBytes > request.Limits.MaxTotalInputBytes) throw new InvalidOperationException($"Total input size of {totalInputBytes} bytes exceeds the limit of {request.Limits.MaxTotalInputBytes} bytes.");
                    if (totalDurationSeconds > request.Limits.MaxOutputDurationSeconds) throw new InvalidOperationException($"Total requested duration of {totalDurationSeconds:F1} seconds exceeds the limit of {request.Limits.MaxOutputDurationSeconds} seconds.");

                    segments.Add(segment);
                }

                string compositionBackgroundAudioPath = null;

                if (request.BackgroundAudio?.Source != null)
                {
                    if (request.BackgroundAudio.Volume < 0 || request.BackgroundAudio.Volume > 1) throw new InvalidOperationException("Composition background audio volume must be between zero and one.");
                    if (request.BackgroundAudio.FadeInSeconds < 0 || request.BackgroundAudio.FadeOutSeconds < 0) throw new InvalidOperationException("Composition background audio fades cannot be negative.");

                    compositionBackgroundAudioPath = workspace.CompositionBackgroundAudioPath;
                    var downloadedAudio = await _sourceDownloader.DownloadAsync(request.BackgroundAudio.Source, compositionBackgroundAudioPath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, executionTimeout.Token);
                    totalInputBytes += downloadedAudio.SizeBytes;

                    if (totalInputBytes > request.Limits.MaxTotalInputBytes) throw new InvalidOperationException($"Total input size of {totalInputBytes} bytes exceeds the limit of {request.Limits.MaxTotalInputBytes} bytes.");
                }

                foreach (var segment in segments) await NormalizeAsync(request.OrganizationId, segment, progress, executionTimeout.Token);
                await ConcatenateAsync(request.OrganizationId, segments, workspace, progress, executionTimeout.Token);

                if (!String.IsNullOrWhiteSpace(compositionBackgroundAudioPath))
                {
                    await MixCompositionBackgroundAudioAsync(request.OrganizationId, request.BackgroundAudio, compositionBackgroundAudioPath, totalDurationSeconds, workspace, progress, executionTimeout.Token);
                }

                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.InspectingMedia, Message = "Inspecting assembled output." });
                var outputInspection = await _inspectionService.InspectAsync(workspace.OutputPath, executionTimeout.Token);
                if (outputInspection.SizeBytes > request.Limits.MaxOutputFileBytes) throw new InvalidOperationException($"Output size of {outputInspection.SizeBytes} bytes exceeds the limit of {request.Limits.MaxOutputFileBytes} bytes.");
                if (outputInspection.DurationSeconds > request.Limits.MaxOutputDurationSeconds) throw new InvalidOperationException($"Output duration of {outputInspection.DurationSeconds:F1} seconds exceeds the limit of {request.Limits.MaxOutputDurationSeconds} seconds.");

                var sha256 = await CalculateSha256Async(workspace.OutputPath, executionTimeout.Token);
                var outputDurationSeconds = (int)Math.Round(outputInspection.DurationSeconds);
                var outputs = new List<VideoProcessorOutputArtifact>();
                string vimeoVideoUri = null;
                string vimeoVideoId = null;

                if (request.ExecutionOptions?.UploadToAzure == true)
                {
                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToAzure, PercentComplete = 0, Message = "Uploading assembled video to Azure.", BytesCompleted = 0, BytesTotal = outputInspection.SizeBytes });
                    var azureVideoProgress = new InlineProgress<AzureBlobUploadProgress>(upload => progress?.Report(new VideoAssemblyProgress
                    {
                        OrganizationId = request.OrganizationId,
                        Stage = VideoAssemblyStage.UploadingToAzure,
                        PercentComplete = upload.PercentComplete,
                        Message = "Uploading assembled video to Azure.",
                        BytesCompleted = upload.BytesCompleted,
                        BytesTotal = upload.BytesTotal
                    }));
                    await _azureBlobSasUploader.UploadAsync(workspace.OutputPath, request.AzureVideoDestination, executionTimeout.Token, azureVideoProgress);

                    outputs.Add(new VideoProcessorOutputArtifact
                    {
                        Type = VideoProcessorOutputArtifactType.Video,
                        MediaResourceId = request.AzureVideoDestination.MediaResourceId,
                        StorageReferenceName = request.AzureVideoDestination.StorageReferenceName,
                        FileName = request.AzureVideoDestination.FileName,
                        ContentType = request.AzureVideoDestination.ContentType,
                        SizeBytes = outputInspection.SizeBytes,
                        DurationSeconds = outputDurationSeconds,
                        Width = outputInspection.Width,
                        Height = outputInspection.Height,
                        Sha256 = sha256
                    });

                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToAzure, PercentComplete = 100, Message = "Assembled video uploaded to Azure.", BytesCompleted = outputInspection.SizeBytes, BytesTotal = outputInspection.SizeBytes });
                }

                if (request.ExecutionOptions?.GenerateThumbnail == true)
                {
                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.GeneratingThumbnail, PercentComplete = 0, Message = "Generating assembled video thumbnail." });

                    var thumbnailTimeSeconds = request.Thumbnail.TimeSeconds ?? 1.0;
                    await _thumbnailExtractor.ExtractAsync(workspace.OutputPath, workspace.ThumbnailPath, thumbnailTimeSeconds, outputInspection.DurationSeconds, executionTimeout.Token);

                    var thumbnailInspection = await _inspectionService.InspectAsync(workspace.ThumbnailPath, executionTimeout.Token);
                    var thumbnailSha256 = await CalculateSha256Async(workspace.ThumbnailPath, executionTimeout.Token);
                    var thumbnailFileInfo = new FileInfo(workspace.ThumbnailPath);

                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.GeneratingThumbnail, PercentComplete = 100, Message = "Assembled video thumbnail generated." });
                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingThumbnail, PercentComplete = 0, Message = "Uploading assembled video thumbnail to Azure.", BytesCompleted = 0, BytesTotal = thumbnailFileInfo.Length });

                    var thumbnailUploadProgress = new InlineProgress<AzureBlobUploadProgress>(upload => progress?.Report(new VideoAssemblyProgress
                    {
                        OrganizationId = request.OrganizationId,
                        Stage = VideoAssemblyStage.UploadingThumbnail,
                        PercentComplete = upload.PercentComplete,
                        Message = "Uploading assembled video thumbnail to Azure.",
                        BytesCompleted = upload.BytesCompleted,
                        BytesTotal = upload.BytesTotal
                    }));
                    await _azureBlobSasUploader.UploadAsync(workspace.ThumbnailPath, request.Thumbnail.Destination, executionTimeout.Token, thumbnailUploadProgress);

                    outputs.Add(new VideoProcessorOutputArtifact
                    {
                        Type = VideoProcessorOutputArtifactType.Thumbnail,
                        MediaResourceId = request.Thumbnail.Destination.MediaResourceId,
                        StorageReferenceName = request.Thumbnail.Destination.StorageReferenceName,
                        FileName = request.Thumbnail.Destination.FileName,
                        ContentType = request.Thumbnail.Destination.ContentType,
                        SizeBytes = thumbnailFileInfo.Length,
                        Width = thumbnailInspection.Width,
                        Height = thumbnailInspection.Height,
                        Sha256 = thumbnailSha256
                    });

                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingThumbnail, PercentComplete = 100, Message = "Assembled video thumbnail uploaded to Azure.", BytesCompleted = thumbnailFileInfo.Length, BytesTotal = thumbnailFileInfo.Length });
                }

                if (request.ExecutionOptions?.UploadToVimeo == true)
                {
                    var uploadUrl = request.VimeoUpload.UploadUrl;
                    vimeoVideoUri = request.VimeoUpload.VideoUri;
                    vimeoVideoId = request.VimeoUpload.VideoId;

                    if (String.IsNullOrWhiteSpace(uploadUrl))
                    {
                        progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToVimeo, Message = "Requesting Vimeo upload session." });
                        var session = await _vimeoUploadSessionClient.CreateSessionAsync(request, outputInspection.SizeBytes, outputDurationSeconds, sha256, executionTimeout.Token);
                        uploadUrl = session.UploadUrl;
                        vimeoVideoUri = session.VideoUri;
                        vimeoVideoId = session.VideoId;
                    }

                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToVimeo, PercentComplete = 0, Message = "Uploading assembled video to Vimeo.", BytesCompleted = 0, BytesTotal = outputInspection.SizeBytes });
                    await _tusVideoUploader.UploadAsync(uploadUrl, workspace.OutputPath, progress, executionTimeout.Token);

                    outputs.Add(new VideoProcessorOutputArtifact
                    {
                        Type = VideoProcessorOutputArtifactType.Video,
                        MediaResourceId = request.VimeoUpload.MediaResourceId,
                        ContentType = "video/mp4",
                        SizeBytes = outputInspection.SizeBytes,
                        DurationSeconds = outputDurationSeconds,
                        Width = outputInspection.Width,
                        Height = outputInspection.Height,
                        Sha256 = sha256,
                        ExternalUri = vimeoVideoUri,
                        ExternalId = vimeoVideoId
                    });
                }

                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.Completed, PercentComplete = 100, Message = "Video assembly and configured uploads completed." });
                return new VideoAssemblyResult
                {
                    Successful = true,
                    OutputFilePath = workspace.OutputPath,
                    Outputs = outputs,
                    VimeoVideoUri = vimeoVideoUri,
                    VimeoVideoId = vimeoVideoId,
                    OutputSizeBytes = outputInspection.SizeBytes,
                    OutputDurationSeconds = outputDurationSeconds,
                    Sha256 = sha256
                };
            }
            catch (Exception ex)
            {
                if (_options.PreserveFailedWorkspace) workspace.Preserve = true;
                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.Failed, Message = ex.Message });
                return new VideoAssemblyResult { Successful = false, OutputFilePath = workspace.Preserve ? workspace.OutputPath : null, ErrorMessage = ex.Message };
            }
        }

        private async Task<VideoAssemblyResult> PublishAsync(VideoAssemblyRequest request, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken)
        {
            using var executionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            executionTimeout.CancelAfter(TimeSpan.FromMinutes(request.Limits.MaxExecutionMinutes));
            using var workspace = _workspaceFactory.Create(request);
            workspace.Preserve = request.ExecutionOptions?.PreserveOutputFile == true;

            try
            {
                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.DownloadingMedia, PercentComplete = 0, Message = "Downloading approved Azure video for Vimeo publishing." });
                var downloaded = await _sourceDownloader.DownloadAsync(request.PublishedVideoSource, workspace.OutputPath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, executionTimeout.Token);

                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.InspectingMedia, Message = "Inspecting approved video before Vimeo publishing." });
                var outputInspection = await _inspectionService.InspectAsync(workspace.OutputPath, executionTimeout.Token);
                if (outputInspection.SizeBytes > request.Limits.MaxOutputFileBytes) throw new InvalidOperationException($"Published video size of {outputInspection.SizeBytes} bytes exceeds the limit of {request.Limits.MaxOutputFileBytes} bytes.");
                if (outputInspection.DurationSeconds > request.Limits.MaxOutputDurationSeconds) throw new InvalidOperationException($"Published video duration of {outputInspection.DurationSeconds:F1} seconds exceeds the limit of {request.Limits.MaxOutputDurationSeconds} seconds.");

                var sha256 = await CalculateSha256Async(workspace.OutputPath, executionTimeout.Token);
                var outputDurationSeconds = (int)Math.Round(outputInspection.DurationSeconds);
                var uploadUrl = request.VimeoUpload.UploadUrl;
                var vimeoVideoUri = request.VimeoUpload.VideoUri;
                var vimeoVideoId = request.VimeoUpload.VideoId;

                if (String.IsNullOrWhiteSpace(uploadUrl))
                {
                    progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToVimeo, Message = "Requesting Vimeo upload session." });
                    var session = await _vimeoUploadSessionClient.CreateSessionAsync(request, outputInspection.SizeBytes, outputDurationSeconds, sha256, executionTimeout.Token);
                    uploadUrl = session.UploadUrl;
                    vimeoVideoUri = session.VideoUri;
                    vimeoVideoId = session.VideoId;
                }

                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.UploadingToVimeo, PercentComplete = 0, Message = "Uploading approved video to Vimeo.", BytesCompleted = 0, BytesTotal = downloaded.SizeBytes });
                await _tusVideoUploader.UploadAsync(uploadUrl, workspace.OutputPath, progress, executionTimeout.Token);

                var output = new VideoProcessorOutputArtifact
                {
                    Type = VideoProcessorOutputArtifactType.Video,
                    MediaResourceId = request.VimeoUpload.MediaResourceId,
                    FileName = request.PublishedVideoSource.FileName,
                    ContentType = request.PublishedVideoSource.ContentType,
                    SizeBytes = outputInspection.SizeBytes,
                    DurationSeconds = outputDurationSeconds,
                    Width = outputInspection.Width,
                    Height = outputInspection.Height,
                    Sha256 = sha256,
                    ExternalUri = vimeoVideoUri,
                    ExternalId = vimeoVideoId
                };

                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.Completed, PercentComplete = 100, Message = "Approved video uploaded to Vimeo." });

                return new VideoAssemblyResult
                {
                    Successful = true,
                    OutputFilePath = workspace.OutputPath,
                    Outputs = new List<VideoProcessorOutputArtifact> { output },
                    VimeoVideoUri = vimeoVideoUri,
                    VimeoVideoId = vimeoVideoId,
                    OutputSizeBytes = outputInspection.SizeBytes,
                    OutputDurationSeconds = outputDurationSeconds,
                    Sha256 = sha256
                };
            }
            catch (Exception ex)
            {
                if (_options.PreserveFailedWorkspace) workspace.Preserve = true;
                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.Failed, Message = ex.Message });
                return new VideoAssemblyResult { Successful = false, OutputFilePath = workspace.Preserve ? workspace.OutputPath : null, ErrorMessage = ex.Message };
            }
        }

        private async Task<VideoAssemblySegment> DownloadAndInspectAsync(VideoAssemblyBlock block, int index, VideoAssemblyWorkspace workspace, VideoAssemblyRequest request, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken)
        {
            var sourcePath = workspace.GetSourcePath(index, block.Type);
            var backgroundPath = block.Background == null ? null : workspace.GetBackgroundPath(index);
            var backgroundAudioPath = block.BackgroundAudio?.Source == null ? null : workspace.GetBackgroundAudioPath(index);
            var overlayImagePaths = new List<string>();
            var normalizedPath = workspace.GetNormalizedPath(index);
            var subtitlePath = workspace.GetSubtitlePath(index);
            var downloaded = await _sourceDownloader.DownloadAsync(block.Source, sourcePath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, cancellationToken);
            long sourceSizeBytes = downloaded.SizeBytes;

            if (block.Background != null)
            {
                if (block.Type != VideoAssemblyBlockType.Video) throw new InvalidOperationException($"Background compositing is only supported for video block '{block.Key}'.");
                if (block.PresenterLayout == null) throw new InvalidOperationException($"Video block '{block.Key}' must provide presenter layout when a background is configured.");
                if (block.PresenterLayout.Scale <= 0) throw new InvalidOperationException($"Video block '{block.Key}' presenter scale must be greater than zero.");
                if (block.PresenterLayout.PositionX < 0 || block.PresenterLayout.PositionX > 1) throw new InvalidOperationException($"Video block '{block.Key}' presenter X position must be between zero and one.");
                if (block.PresenterLayout.PositionY < 0 || block.PresenterLayout.PositionY > 1) throw new InvalidOperationException($"Video block '{block.Key}' presenter Y position must be between zero and one.");

                var downloadedBackground = await _sourceDownloader.DownloadAsync(block.Background, backgroundPath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, cancellationToken);
                sourceSizeBytes += downloadedBackground.SizeBytes;
            }

            if (block.BackgroundAudio?.Source != null)
            {
                if (block.BackgroundAudio.Volume < 0 || block.BackgroundAudio.Volume > 1) throw new InvalidOperationException($"Background audio volume for block '{block.Key}' must be between zero and one.");
                if (block.BackgroundAudio.FadeInSeconds < 0 || block.BackgroundAudio.FadeOutSeconds < 0) throw new InvalidOperationException($"Background audio fades for block '{block.Key}' cannot be negative.");
                var downloadedAudio = await _sourceDownloader.DownloadAsync(block.BackgroundAudio.Source, backgroundAudioPath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, cancellationToken);
                sourceSizeBytes += downloadedAudio.SizeBytes;
            }

            for (var imageIndex = 0; imageIndex < (block.Images?.Count ?? 0); imageIndex++)
            {
                var image = block.Images[imageIndex];
                if (image.Source == null) throw new InvalidOperationException($"Overlay image {imageIndex + 1} on block '{block.Key}' does not have a source.");
                if (image.Scale <= 0) throw new InvalidOperationException($"Overlay image {imageIndex + 1} on block '{block.Key}' must have a scale greater than zero.");
                if (image.PositionX < 0 || image.PositionX > 1 || image.PositionY < 0 || image.PositionY > 1) throw new InvalidOperationException($"Overlay image {imageIndex + 1} on block '{block.Key}' must use positions between zero and one.");
                if (image.Opacity < 0 || image.Opacity > 1) throw new InvalidOperationException($"Overlay image {imageIndex + 1} on block '{block.Key}' opacity must be between zero and one.");

                var imagePath = workspace.GetOverlayImagePath(index, imageIndex);
                var downloadedImage = await _sourceDownloader.DownloadAsync(image.Source, imagePath, request.Limits.MaxSourceFileBytes, VideoAssemblyStage.DownloadingMedia, progress, cancellationToken);
                overlayImagePaths.Add(imagePath);
                sourceSizeBytes += downloadedImage.SizeBytes;
            }

            MediaInspectionResult inspection = null;
            double durationSeconds;

            if (block.Type == VideoAssemblyBlockType.Video)
            {
                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.InspectingMedia, Message = $"Inspecting block '{block.Key}'." });
                inspection = await _inspectionService.InspectAsync(sourcePath, cancellationToken);
                if (inspection.DurationSeconds <= 0) throw new InvalidOperationException($"Video block '{block.Key}' has no measurable duration.");
                durationSeconds = inspection.DurationSeconds;
            }
            else
            {
                durationSeconds = block.DurationSeconds.Value;
            }

            if ((block.Labels?.Count ?? 0) > 0)
            {
                progress?.Report(new VideoAssemblyProgress { OrganizationId = request.OrganizationId, Stage = VideoAssemblyStage.RenderingLabels, Message = $"Preparing labels for block '{block.Key}'." });
                await File.WriteAllTextAsync(subtitlePath, _subtitleBuilder.Build(block, durationSeconds), cancellationToken);
            }

            return new VideoAssemblySegment
            {
                Block = block,
                SourcePath = sourcePath,
                BackgroundPath = backgroundPath,
                BackgroundAudioPath = backgroundAudioPath,
                OverlayImagePaths = overlayImagePaths,
                BackgroundIsImage = block.Background?.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true,
                NormalizedPath = normalizedPath,
                SubtitlePath = subtitlePath,
                SourceSizeBytes = sourceSizeBytes,
                DurationSeconds = durationSeconds,
                Inspection = inspection
            };
        }

        private async Task NormalizeAsync(string organizationId, VideoAssemblySegment segment, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken)
        {
            progress?.Report(new VideoAssemblyProgress { OrganizationId = organizationId, Stage = VideoAssemblyStage.NormalizingMedia, Message = $"Normalizing block '{segment.Block.Key}'.", TotalDurationSeconds = (int)Math.Ceiling(segment.DurationSeconds) });
            var processResult = await _processRunner.RunAsync(_options.FfmpegPath, BuildNormalizeArguments(segment), line => ReportFfmpegProgress(organizationId, line, segment, progress), cancellationToken: cancellationToken);
            if (processResult.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed while normalizing block '{segment.Block.Key}'. {processResult.StandardError}");
            if (!File.Exists(segment.NormalizedPath)) throw new InvalidOperationException($"FFmpeg did not create normalized block '{segment.Block.Key}'.");
        }

        private async Task ConcatenateAsync(string organizationId, IReadOnlyList<VideoAssemblySegment> segments, VideoAssemblyWorkspace workspace, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken)
        {
            await File.WriteAllLinesAsync(workspace.ConcatManifestPath, segments.Select(segment => $"file '{EscapeConcatPath(segment.NormalizedPath)}'").ToList(), cancellationToken);
            progress?.Report(new VideoAssemblyProgress { OrganizationId = organizationId, Stage = VideoAssemblyStage.Encoding, Message = $"Concatenating {segments.Count} normalized block(s)." });

            var arguments = $"-y -f concat -safe 0 -i {ProcessRunner.Quote(workspace.ConcatManifestPath)} -c copy -movflags +faststart {ProcessRunner.Quote(workspace.OutputPath)}";
            var processResult = await _processRunner.RunAsync(_options.FfmpegPath, arguments, cancellationToken: cancellationToken);
            if (processResult.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed while concatenating normalized blocks. {processResult.StandardError}");
            if (!File.Exists(workspace.OutputPath)) throw new InvalidOperationException("FFmpeg did not create the assembled output file.");
        }

        private async Task MixCompositionBackgroundAudioAsync(string organizationId, VideoAssemblyAudio audio, string audioPath, double durationSeconds, VideoAssemblyWorkspace workspace, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken)
        {
            progress?.Report(new VideoAssemblyProgress
            {
                OrganizationId = organizationId,
                Stage = VideoAssemblyStage.Encoding,
                Message = "Mixing composition background audio across the complete video.",
                TotalDurationSeconds = (int)Math.Ceiling(durationSeconds)
            });

            var inputs = audio.Loop
                ? $"-i {ProcessRunner.Quote(workspace.OutputPath)} -stream_loop -1 -i {ProcessRunner.Quote(audioPath)}"
                : $"-i {ProcessRunner.Quote(workspace.OutputPath)} -i {ProcessRunner.Quote(audioPath)}";

            var backgroundFilters = new List<string>
            {
                "aresample=48000",
                $"volume={FormatSeconds(audio.Volume)}",
                $"atrim=0:{FormatSeconds(durationSeconds)}",
                "asetpts=N/SR/TB"
            };

            if (audio.FadeInSeconds > 0)
            {
                backgroundFilters.Add($"afade=t=in:st=0:d={FormatSeconds(Math.Min(audio.FadeInSeconds, durationSeconds))}");
            }

            if (audio.FadeOutSeconds > 0)
            {
                var fadeDuration = Math.Min(audio.FadeOutSeconds, durationSeconds);
                backgroundFilters.Add($"afade=t=out:st={FormatSeconds(Math.Max(0, durationSeconds - fadeDuration))}:d={FormatSeconds(fadeDuration)}");
            }

            var filter = $"[0:a]aresample=48000[primarya];[1:a]{String.Join(",", backgroundFilters)}[backgrounda];[primarya][backgrounda]amix=inputs=2:duration=first:dropout_transition=0[aout]";
            var arguments = $"-y {inputs} -filter_complex \"{filter}\" -map 0:v:0 -map \"[aout]\" -c:v copy -c:a aac -ar 48000 -ac 2 -t {FormatSeconds(durationSeconds)} -movflags +faststart -progress pipe:1 -nostats {ProcessRunner.Quote(workspace.MixedOutputPath)}";

            var processResult = await _processRunner.RunAsync(_options.FfmpegPath, arguments, cancellationToken: cancellationToken);
            if (processResult.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed while mixing composition background audio. {processResult.StandardError}");
            if (!File.Exists(workspace.MixedOutputPath)) throw new InvalidOperationException("FFmpeg did not create the composition background audio mix.");

            File.Delete(workspace.OutputPath);
            File.Move(workspace.MixedOutputPath, workspace.OutputPath);
        }

        private string BuildNormalizeArguments(VideoAssemblySegment segment)
        {
            if (!String.IsNullOrWhiteSpace(segment.BackgroundPath) || !String.IsNullOrWhiteSpace(segment.BackgroundAudioPath) || segment.OverlayImagePaths.Count > 0)
            {
                return BuildCompositeArguments(segment);
            }

            var videoFilter = BuildVideoFilter(segment);
            var audioFilter = BuildAudioFilter(segment);
            var outputOptions = $"-vf \"{videoFilter}\" -af \"{audioFilter}\" -c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p -r 30 -c:a aac -ar 48000 -ac 2 -t {FormatSeconds(segment.DurationSeconds)} -movflags +faststart -progress pipe:1 -nostats";

            if (segment.Block.Type == VideoAssemblyBlockType.Image) return $"-y -loop 1 -framerate 30 -i {ProcessRunner.Quote(segment.SourcePath)} -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=48000 -map 0:v:0 -map 1:a:0 {outputOptions} {ProcessRunner.Quote(segment.NormalizedPath)}";
            if (segment.Inspection.HasAudio) return $"-y -i {ProcessRunner.Quote(segment.SourcePath)} -map 0:v:0 -map 0:a:0 {outputOptions} {ProcessRunner.Quote(segment.NormalizedPath)}";
            return $"-y -i {ProcessRunner.Quote(segment.SourcePath)} -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=48000 -map 0:v:0 -map 1:a:0 {outputOptions} -shortest {ProcessRunner.Quote(segment.NormalizedPath)}";
        }

        private string BuildCompositeArguments(VideoAssemblySegment segment)
        {
            var inputs = new List<string>();
            inputs.Add(segment.Block.Type == VideoAssemblyBlockType.Image
                ? $"-loop 1 -framerate 30 -i {ProcessRunner.Quote(segment.SourcePath)}"
                : $"-c:v libvpx-vp9 -i {ProcessRunner.Quote(segment.SourcePath)}");

            var nextInput = 1;
            int? backgroundInput = null;
            if (!String.IsNullOrWhiteSpace(segment.BackgroundPath))
            {
                backgroundInput = nextInput++;
                inputs.Add(segment.BackgroundIsImage
                    ? $"-loop 1 -framerate 30 -i {ProcessRunner.Quote(segment.BackgroundPath)}"
                    : $"-stream_loop -1 -i {ProcessRunner.Quote(segment.BackgroundPath)}");
            }

            var imageInputs = new List<int>();
            foreach (var imagePath in segment.OverlayImagePaths)
            {
                imageInputs.Add(nextInput++);
                inputs.Add($"-loop 1 -framerate 30 -i {ProcessRunner.Quote(imagePath)}");
            }

            var primaryAudioInput = segment.Block.Type == VideoAssemblyBlockType.Video && segment.Inspection.HasAudio ? 0 : nextInput++;
            if (primaryAudioInput != 0)
            {
                inputs.Add("-f lavfi -i anullsrc=channel_layout=stereo:sample_rate=48000");
            }

            int? backgroundAudioInput = null;
            if (!String.IsNullOrWhiteSpace(segment.BackgroundAudioPath))
            {
                backgroundAudioInput = nextInput++;
                inputs.Add(segment.Block.BackgroundAudio.Loop
                    ? $"-stream_loop -1 -i {ProcessRunner.Quote(segment.BackgroundAudioPath)}"
                    : $"-i {ProcessRunner.Quote(segment.BackgroundAudioPath)}");
            }

            var filter = BuildCompositeFilter(segment, backgroundInput, imageInputs, primaryAudioInput, backgroundAudioInput);
            return $"-y {String.Join(" ", inputs)} -filter_complex \"{filter}\" -map \"[vout]\" -map \"[aout]\" -c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p -r 30 -c:a aac -ar 48000 -ac 2 -t {FormatSeconds(segment.DurationSeconds)} -movflags +faststart -progress pipe:1 -nostats -shortest {ProcessRunner.Quote(segment.NormalizedPath)}";
        }

        private string BuildCompositeFilter(VideoAssemblySegment segment, int? backgroundInput, IReadOnlyList<int> imageInputs, int primaryAudioInput, int? backgroundAudioInput)
        {
            var filters = new List<string>();
            string currentVideo;

            if (backgroundInput.HasValue)
            {
                var layout = segment.Block.PresenterLayout ?? new VideoAssemblyPresenterLayout();
                filters.Add($"[{backgroundInput.Value}:v]scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,setsar=1,fps=30[background]");
                var presenterWidth = Math.Max(2, (int)Math.Round(1920 * layout.Scale / 2) * 2);
                filters.Add($"[0:v]format=rgba,scale={presenterWidth}:-2[presenter]");
                filters.Add($"[background][presenter]overlay=x='(W-w)*{FormatSeconds(layout.PositionX)}':y='(H-h)*{FormatSeconds(layout.PositionY)}':format=auto[video0]");
                currentVideo = "video0";
            }
            else
            {
                filters.Add($"[0:v]{BaseVideoFilter}[video0]");
                currentVideo = "video0";
            }

            for (var imageIndex = 0; imageIndex < imageInputs.Count; imageIndex++)
            {
                var image = segment.Block.Images[imageIndex];
                var width = Math.Max(2, (int)Math.Round(1920 * image.Scale / 2) * 2);
                var visibleDuration = Math.Min(image.VisibleDurationSeconds ?? Math.Max(0, segment.DurationSeconds - image.DelaySeconds), Math.Max(0, segment.DurationSeconds - image.DelaySeconds));
                var imageFilters = new List<string> { $"format=rgba,scale={width}:-2", $"colorchannelmixer=aa={FormatSeconds(image.Opacity)}" };
                if (image.FadeInSeconds > 0) imageFilters.Add($"fade=t=in:st={FormatSeconds(image.DelaySeconds)}:d={FormatSeconds(Math.Min(image.FadeInSeconds, visibleDuration))}:alpha=1");
                if (image.FadeOutSeconds > 0)
                {
                    var fadeDuration = Math.Min(image.FadeOutSeconds, visibleDuration);
                    imageFilters.Add($"fade=t=out:st={FormatSeconds(image.DelaySeconds + Math.Max(0, visibleDuration - fadeDuration))}:d={FormatSeconds(fadeDuration)}:alpha=1");
                }

                filters.Add($"[{imageInputs[imageIndex]}:v]{String.Join(",", imageFilters)}[image{imageIndex}]");
                var nextVideo = $"video{imageIndex + 1}";
                filters.Add($"[{currentVideo}][image{imageIndex}]overlay=x='(W-w)*{FormatSeconds(image.PositionX)}':y='(H-h)*{FormatSeconds(image.PositionY)}':enable='between(t,{FormatSeconds(image.DelaySeconds)},{FormatSeconds(image.DelaySeconds + visibleDuration)})':format=auto[{nextVideo}]");
                currentVideo = nextVideo;
            }

            var outputFilters = new List<string> { $"[{currentVideo}]format=yuv420p" };
            if (segment.Block.FadeInSeconds > 0) outputFilters.Add($"fade=t=in:st=0:d={FormatSeconds(Math.Min(segment.Block.FadeInSeconds, segment.DurationSeconds))}");
            if (segment.Block.FadeOutSeconds > 0)
            {
                var fadeDuration = Math.Min(segment.Block.FadeOutSeconds, segment.DurationSeconds);
                outputFilters.Add($"fade=t=out:st={FormatSeconds(Math.Max(0, segment.DurationSeconds - fadeDuration))}:d={FormatSeconds(fadeDuration)}");
            }

            if (File.Exists(segment.SubtitlePath))
            {
                var fontDirectory = String.IsNullOrWhiteSpace(_options.FontDirectory) ? String.Empty : $":fontsdir='{EscapeFilterPath(_options.FontDirectory)}'";
                outputFilters.Add($"subtitles=filename='{EscapeFilterPath(segment.SubtitlePath)}'{fontDirectory}");
            }

            filters.Add($"{String.Join(",", outputFilters)}[vout]");
            filters.Add($"[{primaryAudioInput}:a]{BuildAudioFilter(segment)}[primarya]");

            if (backgroundAudioInput.HasValue)
            {
                var audio = segment.Block.BackgroundAudio;
                var backgroundFilters = new List<string>
                {
                    "aresample=48000",
                    $"volume={FormatSeconds(audio.Volume)}",
                    $"atrim=0:{FormatSeconds(segment.DurationSeconds)}"
                };

                if (audio.FadeInSeconds > 0) backgroundFilters.Add($"afade=t=in:st=0:d={FormatSeconds(Math.Min(audio.FadeInSeconds, segment.DurationSeconds))}");
                if (audio.FadeOutSeconds > 0)
                {
                    var fadeDuration = Math.Min(audio.FadeOutSeconds, segment.DurationSeconds);
                    backgroundFilters.Add($"afade=t=out:st={FormatSeconds(Math.Max(0, segment.DurationSeconds - fadeDuration))}:d={FormatSeconds(fadeDuration)}");
                }

                filters.Add($"[{backgroundAudioInput.Value}:a]{String.Join(",", backgroundFilters)}[backgrounda]");
                filters.Add("[primarya][backgrounda]amix=inputs=2:duration=first:dropout_transition=0[aout]");
            }
            else
            {
                filters.Add("[primarya]anull[aout]");
            }

            return String.Join(";", filters);
        }

        private string BuildVideoFilter(VideoAssemblySegment segment)
        {
            var filters = new List<string> { BaseVideoFilter };
            if (segment.Block.FadeInSeconds > 0) filters.Add($"fade=t=in:st=0:d={FormatSeconds(Math.Min(segment.Block.FadeInSeconds, segment.DurationSeconds))}");

            if (segment.Block.FadeOutSeconds > 0)
            {
                var fadeDuration = Math.Min(segment.Block.FadeOutSeconds, segment.DurationSeconds);
                filters.Add($"fade=t=out:st={FormatSeconds(Math.Max(0, segment.DurationSeconds - fadeDuration))}:d={FormatSeconds(fadeDuration)}");
            }

            if (File.Exists(segment.SubtitlePath))
            {
                var fontDirectory = String.IsNullOrWhiteSpace(_options.FontDirectory) ? String.Empty : $":fontsdir='{EscapeFilterPath(_options.FontDirectory)}'";
                filters.Add($"subtitles=filename='{EscapeFilterPath(segment.SubtitlePath)}'{fontDirectory}");
            }

            return String.Join(",", filters);
        }

        private static string BuildAudioFilter(VideoAssemblySegment segment)
        {
            var filters = new List<string> { "aresample=48000" };
            if (segment.Block.FadeInSeconds > 0) filters.Add($"afade=t=in:st=0:d={FormatSeconds(Math.Min(segment.Block.FadeInSeconds, segment.DurationSeconds))}");

            if (segment.Block.FadeOutSeconds > 0)
            {
                var fadeDuration = Math.Min(segment.Block.FadeOutSeconds, segment.DurationSeconds);
                filters.Add($"afade=t=out:st={FormatSeconds(Math.Max(0, segment.DurationSeconds - fadeDuration))}:d={FormatSeconds(fadeDuration)}");
            }

            return String.Join(",", filters);
        }

        private static void ReportFfmpegProgress(string organizationId, string line, VideoAssemblySegment segment, IProgress<VideoAssemblyProgress> progress)
        {
            if (!line.StartsWith("out_time_ms=", StringComparison.OrdinalIgnoreCase)) return;
            if (!Int64.TryParse(line.Substring("out_time_ms=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds)) return;

            var processedSeconds = (int)Math.Max(0, microseconds / 1000000L);
            var totalSeconds = (int)Math.Max(1, Math.Ceiling(segment.DurationSeconds));
            progress?.Report(new VideoAssemblyProgress { OrganizationId = organizationId, Stage = VideoAssemblyStage.NormalizingMedia, PercentComplete = Math.Min(100, processedSeconds * 100 / totalSeconds), Message = $"Normalizing block '{segment.Block.Key}'.", ProcessedDurationSeconds = processedSeconds, TotalDurationSeconds = totalSeconds });
        }

        private static string EscapeFilterPath(string path)
        {
            return path.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
        }

        private static string EscapeConcatPath(string path)
        {
            return path.Replace("'", "'\\''").Replace("\\", "/");
        }

        private static string FormatSeconds(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static async Task<string> CalculateSha256Async(string filePath, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private sealed class VideoAssemblySegment
        {
            public VideoAssemblyBlock Block { get; set; }
            public string SourcePath { get; set; }
            public string BackgroundPath { get; set; }
            public string BackgroundAudioPath { get; set; }
            public List<string> OverlayImagePaths { get; set; } = new List<string>();
            public bool BackgroundIsImage { get; set; }
            public string NormalizedPath { get; set; }
            public string SubtitlePath { get; set; }
            public long SourceSizeBytes { get; set; }
            public double DurationSeconds { get; set; }
            public MediaInspectionResult Inspection { get; set; }
        }
    }
}
