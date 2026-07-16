using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoAssemblyRequestManager : IVideoAssemblyRequestManager
    {
        private readonly IVideoCompositionRepo _videoCompositionRepo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IVideoAssemblyMediaSourceResolver _mediaSourceResolver;
        private readonly IVideoProcessorStorageUrlService _videoProcessorStorageUrlService;
        private readonly IVideoProcessorRequestStore _videoProcessorRequestStore;
        private readonly IVideoProcessorCallbackRegistrationStore _videoProcessorCallbackRegistrationStore;
        private readonly IVideoProcessorLauncher _videoProcessorLauncher;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger _adminLogger;
        private readonly IAppConfig _appConfig;

        public VideoAssemblyRequestManager(IVideoCompositionRepo videoCompositionRepo, IMediaServicesManager mediaServicesManager, IVideoAssemblyMediaSourceResolver mediaSourceResolver, IVideoProcessorStorageUrlService videoProcessorStorageUrlService, IVideoProcessorRequestStore videoProcessorRequestStore, IVideoProcessorCallbackRegistrationStore videoProcessorCallbackRegistrationStore, IVideoProcessorLauncher videoProcessorLauncher, ICoreAppServices coreAppServices)
        {
            _videoCompositionRepo = videoCompositionRepo ?? throw new ArgumentNullException(nameof(videoCompositionRepo));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
            _mediaSourceResolver = mediaSourceResolver ?? throw new ArgumentNullException(nameof(mediaSourceResolver));
            _videoProcessorStorageUrlService = videoProcessorStorageUrlService ?? throw new ArgumentNullException(nameof(videoProcessorStorageUrlService));
            _videoProcessorRequestStore = videoProcessorRequestStore ?? throw new ArgumentNullException(nameof(videoProcessorRequestStore));
            _videoProcessorCallbackRegistrationStore = videoProcessorCallbackRegistrationStore ?? throw new ArgumentNullException(nameof(videoProcessorCallbackRegistrationStore));
            _videoProcessorLauncher = videoProcessorLauncher ?? throw new ArgumentNullException(nameof(videoProcessorLauncher));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
            _adminLogger = coreAppServices?.Logger ?? throw new ArgumentNullException(nameof(coreAppServices.Logger));
            _appConfig = coreAppServices?.AppConfig ?? throw new ArgumentNullException(nameof(coreAppServices.AppConfig));
        }

        public async Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareAssemblyRequestAsync(string compositionId, double? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PREPARE STARTED] CompositionId={compositionId}, OrgId={org?.Id}, ThumbnailTimeSeconds={thumbnailTimeSeconds}, Environment={_appConfig.Environment}");

            if (String.IsNullOrWhiteSpace(compositionId))
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError("Video composition ID is required.");
            }

            if (org == null || String.IsNullOrWhiteSpace(org.Id))
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError("An organization is required.");
            }

            if (thumbnailTimeSeconds.HasValue && thumbnailTimeSeconds.Value < 0)
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError("Thumbnail time cannot be negative.");
            }

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY COMPOSITION LOADING] CompositionId={compositionId}, OrgId={org.Id}");
            var composition = await _videoCompositionRepo.GetVideoCompositionAsync(compositionId);
            if (composition == null)
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError($"Could not find video composition '{compositionId}'.");
            }

            if (composition.OwnerOrganization == null || !String.Equals(composition.OwnerOrganization.Id, org.Id, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError("The video composition does not belong to the active organization.");
            }

            if (composition.Blocks == null || composition.Blocks.Count == 0)
            {
                return InvokeResult<VideoAssemblyPreparationResult>.FromError("The video composition does not contain any blocks.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY COMPOSITION VALIDATED] CompositionId={composition.Id}, BlockCount={composition.Blocks.Count}, CurrentStatus={composition.Status?.Value}");

            composition.Status = EntityHeader<VideoCompositionStatus>.Create(VideoCompositionStatus.Preparing);
            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();
            composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Queued;
            composition.AssemblyState.PercentComplete = 1;
            composition.AssemblyState.Message = "Resolving video composition media sources.";
            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            composition.ErrorMessage = null;

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY BLOCK RESOLUTION STARTED] CompositionId={composition.Id}, BlockCount={composition.Blocks.Count}");
            var blocksResult = await CreateAssemblyBlocksAsync(composition, org, user, cancellationToken);
            if (!blocksResult.Successful)
            {
                return await ApplyPreparationFailureAsync(composition, blocksResult.Errors[0].Message);
            }

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY BLOCK RESOLUTION COMPLETED] CompositionId={composition.Id}, BlockCount={blocksResult.Result.Count}");
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY OUTPUT MEDIA PREPARING] CompositionId={composition.Id}, ExistingMediaResourceId={composition.OutputMediaResource?.Id}");
            var outputMediaResource = await GetOrCreateOutputMediaResourceAsync(composition, org, user);
            if (outputMediaResource == null)
            {
                return await ApplyPreparationFailureAsync(composition, "Could not create the output media resource.");
            }

            var pendingRevision = PreparePendingRevision(outputMediaResource, user);
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY VIDEO DESTINATION CREATING] CompositionId={composition.Id}, MediaResourceId={outputMediaResource.Id}, StorageReferenceName={pendingRevision.StorageReferenceName}");
            var videoDestinationResult = await _videoProcessorStorageUrlService.CreateWriteDestinationAsync(org.Id, pendingRevision.StorageReferenceName, "video/mp4", cancellationToken);
            if (!videoDestinationResult.Successful)
            {
                return await ApplyPreparationFailureAsync(composition, videoDestinationResult.Errors[0].Message);
            }

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY THUMBNAIL DESTINATION CREATING] CompositionId={composition.Id}, MediaResourceId={outputMediaResource.Id}, StorageReferenceName={pendingRevision.ThumbnailStorageReferenceName}");
            var thumbnailDestinationResult = await _videoProcessorStorageUrlService.CreateWriteDestinationAsync(org.Id, pendingRevision.ThumbnailStorageReferenceName, "image/jpeg", cancellationToken);
            if (!thumbnailDestinationResult.Successful)
            {
                return await ApplyPreparationFailureAsync(composition, thumbnailDestinationResult.Errors[0].Message);
            }

            outputMediaResource.Link = videoDestinationResult.Result.BlobUrl;
            outputMediaResource.StorageReferenceName = videoDestinationResult.Result.StorageReferenceName;
            outputMediaResource.ThumbnailUrl = thumbnailDestinationResult.Result.BlobUrl;
            outputMediaResource.ThumbnailStorageReferenceName = thumbnailDestinationResult.Result.StorageReferenceName;

            if (composition.OutputMediaResource == null || String.IsNullOrWhiteSpace(composition.OutputMediaResource.Id))
            {
                var addResult = await _mediaServicesManager.AddMediaResourceRecordAsync(outputMediaResource, org, user);
                if (!addResult.Successful)
                {
                    return await ApplyPreparationFailureAsync(composition, addResult.Errors[0].Message);
                }

                composition.OutputMediaResource = outputMediaResource.ToEntityHeader();
            }
            else
            {
                var updateResult = await _mediaServicesManager.UpdateMediaResourceRecordAsync(outputMediaResource, org, user);
                if (!updateResult.Successful)
                {
                    return await ApplyPreparationFailureAsync(composition, updateResult.Errors[0].Message);
                }
            }

            var requestId = String.IsNullOrWhiteSpace(composition.AssemblyState.RequestId) ? Guid.NewGuid().ToId().Value : composition.AssemblyState.RequestId;
            var attemptId = Guid.NewGuid().ToId().Value;
            var callbackAccessToken = CreateCallbackAccessToken();
            var callbackRegistration = new VideoProcessorCallbackRegistration
            {
                PartitionKey = requestId,
                RowKey = attemptId,
                OrganizationId = org.Id,
                JobType = VideoProcessorJobType.VideoAssembly.ToString(),
                RequestId = requestId,
                AttemptId = attemptId,
                ProductionId = composition.Id,
                MediaResourceId = outputMediaResource.Id,
                AccessTokenSha256 = ComputeSha256(callbackAccessToken),
                CreatedUtc = DateTime.UtcNow.ToString("o"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(90).ToString("o"),
                LastSequence = -1,
                IsCompleted = false
            };

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK REGISTERING] CompositionId={composition.Id}, MediaResourceId={outputMediaResource.Id}, RequestId={requestId}, AttemptId={attemptId}, ExpiresUtc={callbackRegistration.ExpiresUtc}");
            await _videoProcessorCallbackRegistrationStore.AddAsync(callbackRegistration, cancellationToken);
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK REGISTERED] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}");

            var request = new VideoAssemblyRequest
            {
                RequestId = requestId,
                AttemptId = attemptId,
                ProductionId = composition.Id,
                Blocks = blocksResult.Result,
                AzureVideoDestination = new VideoMediaImportDestination
                {
                    MediaResourceId = outputMediaResource.Id,
                    UploadUrl = videoDestinationResult.Result.UploadUrl,
                    StorageReferenceName = pendingRevision.StorageReferenceName,
                    FileName = pendingRevision.FileName,
                    ContentType = "video/mp4"
                },
                Callback = new VideoAssemblyCallbackSettings
                {
                    Path = "/api/media/webhooks/video-assembly",
                    AccessToken = callbackAccessToken
                },
                Thumbnail = new VideoMediaImportThumbnail
                {
                    Enabled = true,
                    TimeSeconds = thumbnailTimeSeconds,
                    Destination = new VideoMediaImportDestination
                    {
                        MediaResourceId = outputMediaResource.Id,
                        UploadUrl = thumbnailDestinationResult.Result.UploadUrl,
                        StorageReferenceName = pendingRevision.ThumbnailStorageReferenceName,
                        FileName = CreateThumbnailFileName(pendingRevision, outputMediaResource.Id),
                        ContentType = "image/jpeg"
                    }
                },
                ExecutionOptions = new VideoAssemblyExecutionOptions
                {
                    UploadToAzure = true,
                    GenerateThumbnail = true,
                    UploadToVimeo = false,
                    SendCallbacks = true
                }
            };

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY REQUEST STORING] CompositionId={composition.Id}, MediaResourceId={outputMediaResource.Id}, RequestId={requestId}, AttemptId={attemptId}, JobType={request.JobType}");
            var storedRequestResult = await _videoProcessorRequestStore.SaveAsync(org.Id, request.JobType.ToString(), requestId, attemptId, request, cancellationToken);
            if (!storedRequestResult.Successful)
            {
                await _videoProcessorCallbackRegistrationStore.DeleteAsync(requestId, attemptId, cancellationToken);
                return await ApplyPreparationFailureAsync(composition, storedRequestResult.Errors[0].Message);
            }

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY REQUEST STORED] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}, StorageReferenceName={storedRequestResult.Result.StorageReferenceName}");

            composition.AssemblyRequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName;
            composition.AssemblyRequestBlobUrl = storedRequestResult.Result.BlobUrl;
            composition.AssemblyRequestUrl = storedRequestResult.Result.RequestUrl;
            composition.AssemblyState.RequestId = requestId;
            composition.AssemblyState.AttemptId = attemptId;
            composition.AssemblyState.ContractVersion = request.Version;
            composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Queued;
            composition.AssemblyState.PercentComplete = 5;
            composition.AssemblyState.Message = "Video assembly request prepared and ready for launch.";
            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            composition.Status = EntityHeader<VideoCompositionStatus>.Create(VideoCompositionStatus.Queued);
            composition.SubmittedUtc = UtcTimestamp.Now;
            composition.ErrorMessage = null;

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PROCESSOR LAUNCH STARTED] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}, JobType={request.JobType}, Environment={_appConfig.Environment}");

            if (_appConfig.Environment == Environments.LocalDevelopment || _appConfig.Environment == Environments.Local)
            {
                composition.AssemblyLaunchProvider = "local-manual";
                composition.AssemblyLaunchId = Guid.Empty.ToString().ToLower();
                composition.AssemblyLaunchNamespace = "local";
                composition.AssemblyLaunchJobName = "local";
                composition.AssemblyLaunchedUtc = UtcTimestamp.Now;
                composition.AssemblyState.Message = "Video assembly request prepared for local manual execution.";

                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY MANUAL LAUNCH] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}, RequestUrl='{storedRequestResult.Result.RequestUrl}'");
            }
            else
            {
                var launchResult = await _videoProcessorLauncher.LaunchAsync(new VideoProcessorLaunchRequest
                {
                    JobType = request.JobType,
                    ProductionId = composition.Id,
                    RequestId = requestId,
                    AttemptId = attemptId,
                    RequestUrl = storedRequestResult.Result.RequestUrl
                }, cancellationToken);

                if (!launchResult.Successful)
                {
                    return await ApplyPreparationFailureAsync(composition, launchResult.Errors[0].Message);
                }

                composition.AssemblyLaunchProvider = launchResult.Result.Provider;
                composition.AssemblyLaunchId = launchResult.Result.LaunchId;
                composition.AssemblyLaunchNamespace = launchResult.Result.Namespace;
                composition.AssemblyLaunchJobName = launchResult.Result.JobName;
                composition.AssemblyLaunchedUtc = launchResult.Result.LaunchedUtc;
                composition.AssemblyState.Message = "Video assembly processor launched.";

                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PROCESSOR LAUNCHED] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}, Provider={launchResult.Result.Provider}, Namespace={launchResult.Result.Namespace}, JobName={launchResult.Result.JobName}, LaunchId={launchResult.Result.LaunchId}");
            }

            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PREPARE COMPLETED] CompositionId={composition.Id}, MediaResourceId={outputMediaResource.Id}, RequestId={requestId}, AttemptId={attemptId}, LaunchProvider={composition.AssemblyLaunchProvider}");

            return InvokeResult<VideoAssemblyPreparationResult>.Create(new VideoAssemblyPreparationResult
            {
                Composition = composition,
                OutputMediaResource = outputMediaResource,
                Request = request,
                RequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName,
                RequestBlobUrl = storedRequestResult.Result.BlobUrl,
                RequestUrl = storedRequestResult.Result.RequestUrl
            });
        }

        private async Task<InvokeResult<List<VideoAssemblyBlock>>> CreateAssemblyBlocksAsync(VideoComposition composition, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            var assemblyBlocks = new List<VideoAssemblyBlock>();

            foreach (var block in composition.Blocks.OrderBy(block => block.SortOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY BLOCK RESOLVING] CompositionId={composition.Id}, BlockKey={block.Key}, SortOrder={block.SortOrder}, BlockType={block.Type}, MediaResourceId={block.MediaResource?.Id}");

                if (block.MediaResource == null || String.IsNullOrWhiteSpace(block.MediaResource.Id))
                {
                    return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Video composition block '{block.Key}' does not reference a media resource.");
                }

                var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(block.MediaResource.Id, org, user);
                if (mediaResource == null)
                {
                    return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Could not find media resource '{block.MediaResource.Id}' for block '{block.Key}'.");
                }

                if (mediaResource.Status?.Value != MediaResourceStatus.Ready)
                {
                    return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Media resource '{mediaResource.Name}' for block '{block.Key}' is not ready.");
                }

                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY SOURCE URL CREATING] CompositionId={composition.Id}, BlockKey={block.Key}, MediaResourceId={mediaResource.Id}, IsFileUpload={mediaResource.IsFileUpload}, ResourceType={mediaResource.ResourceType?.Value}, StorageReferenceName={mediaResource.GetCurrentStorageReferenceName()}");
                var sourceResult = await _mediaSourceResolver.ResolveAsync(mediaResource, org.Id, cancellationToken);
                if (!sourceResult.Successful)
                {
                    return sourceResult.ToInvokeResult<List<VideoAssemblyBlock>>();
                }

                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY SOURCE URL CREATED] CompositionId={composition.Id}, BlockKey={block.Key}, MediaResourceId={mediaResource.Id}, FileName={sourceResult.Result.FileName}, ContentType={sourceResult.Result.ContentType}");

                assemblyBlocks.Add(new VideoAssemblyBlock
                {
                    Key = block.Key,
                    Type = block.Type == VideoCompositionBlockType.Image ? VideoAssemblyBlockType.Image : VideoAssemblyBlockType.Video,
                    Source = sourceResult.Result,
                    DurationSeconds = block.DurationSeconds,
                    FadeInSeconds = block.FadeInSeconds,
                    FadeOutSeconds = block.FadeOutSeconds,
                    Labels = (block.CompositionLabels ?? new List<VideoCompositionTextLabel>()).Select(CreateAssemblyLabel).ToList()
                });
            }

            return InvokeResult<List<VideoAssemblyBlock>>.Create(assemblyBlocks);
        }

        private async Task<MediaResource> GetOrCreateOutputMediaResourceAsync(VideoComposition composition, EntityHeader org, EntityHeader user)
        {
            if (composition.OutputMediaResource != null && !String.IsNullOrWhiteSpace(composition.OutputMediaResource.Id))
            {
                return await _mediaServicesManager.GetMediaResourceRecordAsync(composition.OutputMediaResource.Id, org, user);
            }

            var now = UtcTimestamp.Now;
            var mediaResource = new MediaResource
            {
                Id = Guid.NewGuid().ToId(),
                Name = String.IsNullOrWhiteSpace(composition.Name) ? "Assembled Video" : composition.Name,
                Key = $"assembledvideo{DateTime.UtcNow.Ticks}",
                Description = composition.Description,
                FileName = CreateVideoFileName(composition),
                IsFileUpload = false,
                Link = null,
                MimeType = "video/mp4",
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.RawVideo),
                Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending),
                ProcessingStartedUtc = now,
                OwnerOrganization = org,
                CreatedBy = user,
                LastUpdatedBy = user,
                CreationDate = now,
                LastUpdatedDate = now,
                SourceEntityType = nameof(VideoComposition),
                SourceEntity = composition.ToEntityHeader()
            };

            return mediaResource;
        }

        private static MediaResourceHistory PreparePendingRevision(MediaResource mediaResource, EntityHeader user)
        {
            var pendingRevision = mediaResource.GetPendingRevision();
            if (pendingRevision == null || pendingRevision.Status?.Value != MediaResourceStatus.Pending)
            {
                pendingRevision = new MediaResourceHistory
                {
                    Name = $"Video assembly {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    CreatedBy = user,
                    CreationDate = UtcTimestamp.Now,
                    StorageReferenceName = $"{Guid.NewGuid().ToId()}.mp4",
                    ThumbnailStorageReferenceName = $"{Guid.NewGuid().ToId()}.jpg",
                    FileName = String.IsNullOrWhiteSpace(mediaResource.FileName) ? $"{mediaResource.Id}.mp4" : mediaResource.FileName,
                    MimeType = "video/mp4",
                    Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending)
                };

                mediaResource.History.Add(pendingRevision);
                mediaResource.PendingRevision = pendingRevision.Id;
            }

            mediaResource.ProcessingStartedUtc = UtcTimestamp.Now;
            mediaResource.ProcessingCompletedUtc = null;
            mediaResource.ProcessingErrorMessage = null;
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = user;
            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending);

            return pendingRevision;
        }

        private static VideoAssemblyTextLabel CreateAssemblyLabel(VideoCompositionTextLabel label)
        {
            return new VideoAssemblyTextLabel
            {
                Text = label.Text,
                X = label.X,
                Y = label.Y,
                FontSize = label.FontSize,
                Bold = label.Bold,
                Color = label.Color,
                Alignment = label.Alignment == VideoCompositionTextAlignment.Center ? VideoAssemblyTextAlignment.Center : label.Alignment == VideoCompositionTextAlignment.Right ? VideoAssemblyTextAlignment.Right : VideoAssemblyTextAlignment.Left,
                MaxWidth = label.MaxWidth,
                DelaySeconds = label.DelaySeconds,
                VisibleDurationSeconds = label.VisibleDurationSeconds,
                FadeInSeconds = label.FadeInSeconds,
                FadeOutSeconds = label.FadeOutSeconds
            };
        }

        private static string CreateVideoFileName(VideoComposition composition)
        {
            var name = String.IsNullOrWhiteSpace(composition.Name) ? "assembled-video" : composition.Name;

            foreach (var invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidCharacter, '-');
            }

            return name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.mp4";
        }

        private static string CreateThumbnailFileName(MediaResourceHistory revision, string mediaResourceId)
        {
            if (!String.IsNullOrWhiteSpace(revision.FileName))
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(revision.FileName)}-thumbnail.jpg";
            }

            return $"{mediaResourceId}-thumbnail.jpg";
        }

        private async Task<InvokeResult<VideoAssemblyPreparationResult>> ApplyPreparationFailureAsync(VideoComposition composition, string message)
        {
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PREPARE FAILED] CompositionId={composition?.Id}, RequestId={composition?.AssemblyState?.RequestId}, AttemptId={composition?.AssemblyState?.AttemptId}, Message={message}");

            composition.Status = EntityHeader<VideoCompositionStatus>.Create(VideoCompositionStatus.Failed);
            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();
            composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Failed;
            composition.AssemblyState.Message = message;
            composition.AssemblyState.ErrorMessage = message;
            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            composition.ErrorMessage = message;

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            return InvokeResult<VideoAssemblyPreparationResult>.FromError(message);
        }

        private async Task PublishVideoCompositionUpdatedAsync(VideoComposition composition)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, composition.Id, "video-composition-updated", composition);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, composition.OwnerOrganization.Id, "video-composition-updated", composition);
        }

        private static string CreateCallbackAccessToken()
        {
            var tokenBytes = new byte[32];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(tokenBytes);
            }

            return Convert.ToBase64String(tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string ComputeSha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(hashBytes.Length * 2);

                foreach (var hashByte in hashBytes)
                {
                    builder.Append(hashByte.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
