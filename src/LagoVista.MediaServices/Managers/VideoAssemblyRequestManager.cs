using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using LagoVista.UserAdmin.Models.Orgs;
using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IVimeoVideoService _vimeoVideoService;
        private readonly IOrganizationLoaderRepo _organizationLoaderRepo;
        private readonly ISecureStorage _secureStorage;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger _adminLogger;
        private readonly IAppConfig _appConfig;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMediaLibraryRepo _mediaLibraryRepo;

        public VideoAssemblyRequestManager(IVideoCompositionRepo videoCompositionRepo, IMediaServicesManager mediaServicesManager, IVideoAssemblyMediaSourceResolver mediaSourceResolver, 
            IVideoProcessorStorageUrlService videoProcessorStorageUrlService, IVideoProcessorRequestStore videoProcessorRequestStore, IVideoProcessorCallbackRegistrationStore videoProcessorCallbackRegistrationStore, 
            IVideoProcessorLauncher videoProcessorLauncher, IVimeoVideoService vimeoVideoService, IOrganizationLoaderRepo organizationLoaderRepo, ISecureStorage secureStorage, 
            IMediaLibraryRepo mediaLibraryRepo, ICacheProvider cacheProvider, ICoreAppServices coreAppServices)
        {
            _videoCompositionRepo = videoCompositionRepo ?? throw new ArgumentNullException(nameof(videoCompositionRepo));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
            _mediaSourceResolver = mediaSourceResolver ?? throw new ArgumentNullException(nameof(mediaSourceResolver));
            _videoProcessorStorageUrlService = videoProcessorStorageUrlService ?? throw new ArgumentNullException(nameof(videoProcessorStorageUrlService));
            _videoProcessorRequestStore = videoProcessorRequestStore ?? throw new ArgumentNullException(nameof(videoProcessorRequestStore));
            _videoProcessorCallbackRegistrationStore = videoProcessorCallbackRegistrationStore ?? throw new ArgumentNullException(nameof(videoProcessorCallbackRegistrationStore));
            _videoProcessorLauncher = videoProcessorLauncher ?? throw new ArgumentNullException(nameof(videoProcessorLauncher));
            _vimeoVideoService = vimeoVideoService ?? throw new ArgumentNullException(nameof(vimeoVideoService));
            _organizationLoaderRepo = organizationLoaderRepo ?? throw new ArgumentNullException(nameof(organizationLoaderRepo));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
            _adminLogger = coreAppServices?.Logger ?? throw new ArgumentNullException(nameof(coreAppServices.Logger));
            _appConfig = coreAppServices?.AppConfig ?? throw new ArgumentNullException(nameof(coreAppServices.AppConfig));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _mediaLibraryRepo = mediaLibraryRepo ?? throw new ArgumentNullException(nameof(mediaLibraryRepo));
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
            composition.CurrentInputSha256 = composition.CalculateCurrentInputSha256();
            composition.ExecutionInputSha256 = composition.CurrentInputSha256;
            composition.SetStatus(VideoCompositionStatus.Preparing);
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

            var backgroundAudioSourceResult = await ResolveOptionalAssemblySourceAsync(
                composition.BackgroundAudioMediaResource,
                "composition background audio",
                org,
                user,
                cancellationToken);

            if (!backgroundAudioSourceResult.Successful)
            {
                return await ApplyPreparationFailureAsync(composition, backgroundAudioSourceResult.Errors[0].Message);
            }

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY OUTPUT MEDIA PREPARING] CompositionId={composition.Id}, ExistingMediaResourceId={composition.OutputMediaResource?.Id}");
            var outputMediaResource = await GetOrCreateOutputMediaResourceAsync(composition, org, user);
            if (outputMediaResource == null)
            {
                return await ApplyPreparationFailureAsync(composition, "Could not create the output media resource.");
            }

            if (composition.OutputMediaLibrary != null && !String.IsNullOrWhiteSpace(composition.OutputMediaLibrary.Id))
            {
                outputMediaResource.MediaLibrary = composition.OutputMediaLibrary;
            }
            else if (outputMediaResource.MediaLibrary == null)
            {
                var createLibraryResult = await GetOrCreateRawVideoLibraryAsync("publishedvideo", org, user);
                if (!createLibraryResult.Successful)
                {
                    return createLibraryResult.ToInvokeResult<VideoAssemblyPreparationResult>();
                }

                outputMediaResource.MediaLibrary = createLibraryResult.Result.ToEntityHeader();
                composition.OutputMediaLibrary = outputMediaResource.MediaLibrary;
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

            outputMediaResource.ExternalUrl = videoDestinationResult.Result.BlobUrl;
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
                OrganizationId = org.Id,
                BackgroundAudio = backgroundAudioSourceResult.Result == null
                    ? null
                    : new VideoAssemblyAudio
                    {
                        Source = backgroundAudioSourceResult.Result,
                        Volume = composition.BackgroundAudioVolume,
                        FadeInSeconds = composition.BackgroundAudioFadeInSeconds,
                        FadeOutSeconds = composition.BackgroundAudioFadeOutSeconds,
                        Loop = composition.LoopBackgroundAudio
                    },
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
            composition.SetStatus(VideoCompositionStatus.Queued);
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

        private async Task<InvokeResult<MediaLibrary>> GetOrCreateRawVideoLibraryAsync(string libraryKey, EntityHeader org, EntityHeader user)
        {

            var existingLibrary = await _mediaLibraryRepo.GetMediaLibraryByKeyAsync(org.Id, libraryKey);
            if (existingLibrary != null)
            {
                return InvokeResult<MediaLibrary>.Create(existingLibrary);
            }

            var lockKey = $"media-library:create:{org.Id}:{libraryKey}";
            var lockToken = Guid.NewGuid().ToId().Value;
            var lockAcquired = await _cacheProvider.AttemptAcquireLockAsync(lockKey, lockToken, TimeSpan.FromSeconds(15));

            if (!lockAcquired)
            {
                await Task.Delay(250);

                existingLibrary = await _mediaLibraryRepo.GetMediaLibraryByKeyAsync(org.Id, libraryKey);
                if (existingLibrary != null)
                {
                    return InvokeResult<MediaLibrary>.Create(existingLibrary);
                }

                return InvokeResult<MediaLibrary>.FromError("The video clips media library is currently being created. Please retry.");
            }

            try
            {
                existingLibrary = await _mediaLibraryRepo.GetMediaLibraryByKeyAsync(org.Id, libraryKey);
                if (existingLibrary != null)
                {
                    return InvokeResult<MediaLibrary>.Create(existingLibrary);
                }

                var now = UtcTimestamp.Now;
                var library = new MediaLibrary
                {
                    Id = Guid.NewGuid().ToId(),
                    Key = libraryKey,
                    Name = libraryKey == "rawvideo" ? "Video Clips" : "Published Video",
                    Description = libraryKey == "rawvideo" ? "Reusable raw video clips available for video compositions." : "Published and Produced Video",
                    OwnerOrganization = org,
                    CreatedBy = user,
                    LastUpdatedBy = user,
                    CreationDate = now,
                    LastUpdatedDate = now
                };

                await _mediaLibraryRepo.AddMediaLibraryAsync(library);
                return InvokeResult<MediaLibrary>.Create(library);
            }
            finally
            {
                await _cacheProvider.ReleaseLockAsync(lockKey, lockToken);
            }
        }

        public async Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareVimeoPublishRequestAsync(string compositionId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            _adminLogger.Trace($"{this.Tag()} [VIMEO PUBLISH PREPARE STARTED] CompositionId={compositionId}, OrgId={org?.Id}, Environment={_appConfig.Environment}");

            if (String.IsNullOrWhiteSpace(compositionId)) return InvokeResult<VideoAssemblyPreparationResult>.FromError("Video composition ID is required.");
            if (org == null || String.IsNullOrWhiteSpace(org.Id)) return InvokeResult<VideoAssemblyPreparationResult>.FromError("An organization is required.");

            var composition = await _videoCompositionRepo.GetVideoCompositionAsync(compositionId);
            if (composition == null) return InvokeResult<VideoAssemblyPreparationResult>.FromError($"Could not find video composition '{compositionId}'.");
            if (composition.OwnerOrganization == null || !String.Equals(composition.OwnerOrganization.Id, org.Id, StringComparison.OrdinalIgnoreCase)) return InvokeResult<VideoAssemblyPreparationResult>.FromError("The video composition does not belong to the active organization.");
            if (composition.Status?.Value != VideoCompositionStatus.ProcessingAtVimeo &&
                composition.Status?.Value != VideoCompositionStatus.Failed &&
                composition.Status?.Value != VideoCompositionStatus.Cancelled &&
                composition.Status?.Value != VideoCompositionStatus.Completed) return InvokeResult<VideoAssemblyPreparationResult>.FromError("The video composition must be completed before it can be published to Vimeo.");
            if (composition.OutputMediaResource == null || String.IsNullOrWhiteSpace(composition.OutputMediaResource.Id)) return InvokeResult<VideoAssemblyPreparationResult>.FromError("The video composition does not have an Azure output media resource.");

            cancellationToken.ThrowIfCancellationRequested();

            var azureMediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(composition.OutputMediaResource.Id, org, user);
            if (azureMediaResource == null) return InvokeResult<VideoAssemblyPreparationResult>.FromError($"Could not find Azure output media resource '{composition.OutputMediaResource.Id}'.");
            if (azureMediaResource.Status?.Value != MediaResourceStatus.Ready) return InvokeResult<VideoAssemblyPreparationResult>.FromError("The Azure output media resource is not ready for Vimeo publishing.");

            var sourceResult = await _mediaSourceResolver.ResolveAsync(azureMediaResource, org.Id, cancellationToken);
            if (!sourceResult.Successful) return sourceResult.ToInvokeResult<VideoAssemblyPreparationResult>();

            var publishedVideoSource = sourceResult.Result;

            publishedVideoSource.ContentType = !String.IsNullOrWhiteSpace(azureMediaResource.MimeType)
                ? azureMediaResource.MimeType
                : "video/mp4";

            MediaResource publishedMediaResource;
            if (composition.PublishedVideoMediaResource != null && !String.IsNullOrWhiteSpace(composition.PublishedVideoMediaResource.Id))
            {
                publishedMediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(composition.PublishedVideoMediaResource.Id, org, user);
                if (publishedMediaResource == null) return InvokeResult<VideoAssemblyPreparationResult>.FromError($"Could not find Vimeo media resource '{composition.PublishedVideoMediaResource.Id}'.");
            }
            else
            {
                var now = UtcTimestamp.Now;
                publishedMediaResource = new MediaResource
                {
                    Id = Guid.NewGuid().ToId(),
                    Name = String.IsNullOrWhiteSpace(composition.Name) ? "Published Vimeo Video" : composition.Name,
                    Key = $"vimeovideo{DateTime.UtcNow.Ticks}",
                    Description = composition.Description,
                    FileName = String.IsNullOrWhiteSpace(azureMediaResource.FileName)
                                ? $"{composition.Id}.mp4"
                                : Path.HasExtension(azureMediaResource.FileName)
                                    ? azureMediaResource.FileName
                                    : $"{azureMediaResource.FileName}.mp4",
                    IsFileUpload = false,
                    MimeType = "video/mp4",
                    ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Video),
                    Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending),
                    ExternalUrl = "TBD",
                    OwnerOrganization = org,
                    CreatedBy = user,
                    LastUpdatedBy = user,
                    CreationDate = now,
                    LastUpdatedDate = now,
                    SourceEntityType = nameof(MediaResource),
                    SourceEntity = azureMediaResource.ToEntityHeader()
                };

                var addResult = await _mediaServicesManager.AddMediaResourceRecordAsync(publishedMediaResource, org, user);
                if (!addResult.Successful) return addResult.ToInvokeResult<VideoAssemblyPreparationResult>();

                composition.PublishedVideoMediaResource = publishedMediaResource.ToEntityHeader();
            }

            if (publishedMediaResource.MediaLibrary == null)
            {
                var createLibraryResult = await GetOrCreateRawVideoLibraryAsync("publishedvideo", org, user);
                if(!createLibraryResult.Successful)
                {
                    return createLibraryResult.ToInvokeResult<VideoAssemblyPreparationResult>();
                }

                publishedMediaResource.MediaLibrary = createLibraryResult.Result.ToEntityHeader();
            }
            publishedMediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending);
            publishedMediaResource.LastUpdatedDate = UtcTimestamp.Now;
            publishedMediaResource.LastUpdatedBy = user;

            var updatePublishedResult = await _mediaServicesManager.UpdateMediaResourceRecordAsync(publishedMediaResource, org, user);
            if (!updatePublishedResult.Successful) return updatePublishedResult.ToInvokeResult<VideoAssemblyPreparationResult>();

            var requestId = Guid.NewGuid().ToId().Value;
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
                MediaResourceId = publishedMediaResource.Id,
                AccessTokenSha256 = ComputeSha256(callbackAccessToken),
                CreatedUtc = DateTime.UtcNow.ToString("o"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(90).ToString("o"),
                LastSequence = -1,
                IsCompleted = false
            };

            await _videoProcessorCallbackRegistrationStore.AddAsync(callbackRegistration, cancellationToken);

            var request = new VideoAssemblyRequest
            {
                RequestId = requestId,
                AttemptId = attemptId,
                ProductionId = composition.Id,
                OrganizationId = org.Id,
                PublishedVideoSource = publishedVideoSource,
                VimeoUpload = new VideoAssemblyVimeoUpload
                {
                    MediaResourceId = publishedMediaResource.Id,
                    SessionRequestUrl = "/api/media/videocomposition/vimeo/session",
                    SessionAccessToken = callbackAccessToken
                },
                Callback = new VideoAssemblyCallbackSettings
                {
                    Path = "/api/media/webhooks/video-assembly",
                    AccessToken = callbackAccessToken
                },
                ExecutionOptions = new VideoAssemblyExecutionOptions
                {
                    Operation = VideoAssemblyOperation.Publish,
                    UploadToAzure = false,
                    GenerateThumbnail = false,
                    UploadToVimeo = true,
                    SendCallbacks = true
                }
            };

            var storedRequestResult = await _videoProcessorRequestStore.SaveAsync(org.Id, request.JobType.ToString(), requestId, attemptId, request, cancellationToken);
            if (!storedRequestResult.Successful)
            {
                await _videoProcessorCallbackRegistrationStore.DeleteAsync(requestId, attemptId, cancellationToken);
                return storedRequestResult.ToInvokeResult<VideoAssemblyPreparationResult>();
            }

            composition.AssemblyRequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName;
            composition.AssemblyRequestBlobUrl = storedRequestResult.Result.BlobUrl;
            composition.AssemblyRequestUrl = storedRequestResult.Result.RequestUrl;
            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();
            composition.AssemblyState.RequestId = requestId;
            composition.AssemblyState.AttemptId = attemptId;
            composition.AssemblyState.ContractVersion = request.Version;
            composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Queued;
            composition.AssemblyState.PercentComplete = 5;
            composition.AssemblyState.Message = "Vimeo publishing request prepared and ready for launch.";
            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            composition.SetStatus(VideoCompositionStatus.ProcessingAtVimeo);
            composition.ErrorMessage = null;

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            if (_appConfig.Environment == Environments.LocalDevelopment || _appConfig.Environment == Environments.Local)
            {
                composition.AssemblyLaunchProvider = "local-manual";
                composition.AssemblyLaunchId = Guid.Empty.ToString().ToLower();
                composition.AssemblyLaunchNamespace = "local";
                composition.AssemblyLaunchJobName = "local-vimeo-publish";
                composition.AssemblyLaunchedUtc = UtcTimestamp.Now;
                composition.AssemblyState.Message = "Vimeo publishing request prepared for local manual execution.";
                _adminLogger.Trace($"{this.Tag()} [VIMEO PUBLISH MANUAL LAUNCH] CompositionId={composition.Id}, RequestId={requestId}, AttemptId={attemptId}, RequestUrl='{storedRequestResult.Result.RequestUrl}'");
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

                if (!launchResult.Successful) return launchResult.ToInvokeResult<VideoAssemblyPreparationResult>();

                composition.AssemblyLaunchProvider = launchResult.Result.Provider;
                composition.AssemblyLaunchId = launchResult.Result.LaunchId;
                composition.AssemblyLaunchNamespace = launchResult.Result.Namespace;
                composition.AssemblyLaunchJobName = launchResult.Result.JobName;
                composition.AssemblyLaunchedUtc = launchResult.Result.LaunchedUtc;
                composition.AssemblyState.Message = "Vimeo publishing processor launched.";
            }

            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            _adminLogger.Trace($"{this.Tag()} [VIMEO PUBLISH PREPARE COMPLETED] CompositionId={composition.Id}, AzureMediaResourceId={azureMediaResource.Id}, VimeoMediaResourceId={publishedMediaResource.Id}, RequestId={requestId}, AttemptId={attemptId}");

            return InvokeResult<VideoAssemblyPreparationResult>.Create(new VideoAssemblyPreparationResult
            {
                Composition = composition,
                OutputMediaResource = publishedMediaResource,
                Request = request,
                RequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName,
                RequestBlobUrl = storedRequestResult.Result.BlobUrl,
                RequestUrl = storedRequestResult.Result.RequestUrl
            });
        }

        public async Task<InvokeResult<VideoAssemblyVimeoSessionResponse>> CreateVimeoUploadSessionAsync(VideoAssemblyVimeoSessionRequest request, string accessToken, CancellationToken cancellationToken = default)
        {
            if (request == null) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("A Vimeo upload session request is required.");
            if (String.IsNullOrWhiteSpace(request.RequestId)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("RequestId is required.");
            if (String.IsNullOrWhiteSpace(request.AttemptId)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("AttemptId is required.");
            if (String.IsNullOrWhiteSpace(request.ProductionId)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("ProductionId is required.");
            if (request.OutputSizeBytes <= 0) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The Vimeo upload size must be greater than zero.");

            var registration = await _videoProcessorCallbackRegistrationStore.GetAsync(request.RequestId, request.AttemptId, cancellationToken);
            if (registration == null) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError($"Could not find processor registration for request '{request.RequestId}' attempt '{request.AttemptId}'.");
            if (!IsAccessTokenValid(accessToken, registration.AccessTokenSha256)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The Vimeo upload session bearer token is invalid.");
            if (!String.Equals(registration.ProductionId, request.ProductionId, StringComparison.OrdinalIgnoreCase)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The Vimeo upload session production ID does not match the registered composition.");
            if (!DateTime.TryParse(registration.ExpiresUtc, out var expiresUtc) || expiresUtc.ToUniversalTime() <= DateTime.UtcNow) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The Vimeo upload session registration has expired.");

            var composition = await _videoCompositionRepo.GetVideoCompositionAsync(request.ProductionId);
            if (composition == null) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError($"Could not find video composition '{request.ProductionId}'.");
            if (composition.OwnerOrganization == null || !String.Equals(composition.OwnerOrganization.Id, registration.OrganizationId, StringComparison.OrdinalIgnoreCase)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The video composition does not belong to the registered organization.");
            if (composition.PublishedVideoMediaResource == null || !String.Equals(composition.PublishedVideoMediaResource.Id, registration.MediaResourceId, StringComparison.OrdinalIgnoreCase)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The registered Vimeo media resource does not match the composition publishing resource.");

            var organization = await _organizationLoaderRepo.GetOrganizationAsync(registration.OrganizationId);
            if (organization == null) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError($"Could not load organization '{registration.OrganizationId}'.");
            if (!organization.VimeoEnabled) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("Vimeo publishing is not enabled for this organization.");
            if (String.IsNullOrWhiteSpace(organization.VimeoAccessTokenSecretId)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The organization does not have a Vimeo access token configured.");

            var user = composition.LastUpdatedBy ?? composition.CreatedBy;
            var tokenResult = await _secureStorage.GetSecretAsync(composition.OwnerOrganization, organization.VimeoAccessTokenSecretId, user);
            if (!tokenResult.Successful) return tokenResult.ToInvokeResult<VideoAssemblyVimeoSessionResponse>();
            if (String.IsNullOrWhiteSpace(tokenResult.Result)) return InvokeResult<VideoAssemblyVimeoSessionResponse>.FromError("The configured Vimeo access token is empty.");

            var privacy = organization.VimeoDefaultPrivacy?.Id;
            if (String.IsNullOrWhiteSpace(privacy)) privacy = Organization.Organization_VimeoUnlisted;

            var uploadResult = await _vimeoVideoService.CreateTusUploadAsync(tokenResult.Result, new VimeoTusUploadRequest
            {
                Name = composition.Name,
                Description = composition.Description,
                Upload = new VimeoTusUploadSource { Size = request.OutputSizeBytes },
                Privacy = new VimeoPrivacySettings { View = privacy }
            }, cancellationToken);

            if (!uploadResult.Successful) return uploadResult.ToInvokeResult<VideoAssemblyVimeoSessionResponse>();

            var videoUri = uploadResult.Result.Uri;
            var videoId = ResolveVimeoVideoId(videoUri);

            composition.VimeoVideoUri = videoUri;
            composition.VimeoVideoId = videoId;
            composition.VimeoVideoUrl = uploadResult.Result.Link;
            composition.SetStatus(VideoCompositionStatus.ProcessingAtVimeo);
            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();
            composition.AssemblyState.Stage = VideoCompositionAssemblyStage.UploadingToVimeo;
            composition.AssemblyState.Message = "Uploading approved video to Vimeo.";
            composition.AssemblyState.LastUpdatedUtc = UtcTimestamp.Now;
            composition.ErrorMessage = null;

            if (!String.IsNullOrWhiteSpace(organization.VimeoDefaultFolderUri))
            {
                var folderResult = await _vimeoVideoService.AddVideoToFolderAsync(videoUri, organization.VimeoDefaultFolderUri, tokenResult.Result, cancellationToken);
                if (!folderResult.Successful) return folderResult.ToInvokeResult<VideoAssemblyVimeoSessionResponse>();

                composition.VimeoFolderUri = organization.VimeoDefaultFolderUri;
                composition.VimeoFolderAssignedUtc = UtcTimestamp.Now;
            }

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            return InvokeResult<VideoAssemblyVimeoSessionResponse>.Create(new VideoAssemblyVimeoSessionResponse
            {
                UploadUrl = uploadResult.Result.Upload.UploadLink,
                VideoUri = videoUri,
                VideoId = videoId
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

                VideoAssemblySource backgroundSource = null;
                var effectiveBackground = block.Type == VideoCompositionBlockType.Video
                    ? block.BackgroundMediaResource ?? composition.BackgroundMediaResource
                    : null;

                if (effectiveBackground != null && !String.IsNullOrWhiteSpace(effectiveBackground.Id))
                {
                    _adminLogger.Trace($"{this.Tag()} [ASSEMBLY BACKGROUND RESOLVING] CompositionId={composition.Id}, BlockKey={block.Key}, MediaResourceId={effectiveBackground.Id}, IsOverride={block.BackgroundMediaResource != null}");

                    var backgroundMediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(effectiveBackground.Id, org, user);
                    if (backgroundMediaResource == null)
                    {
                        return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Could not find background media resource '{effectiveBackground.Id}' for block '{block.Key}'.");
                    }

                    if (backgroundMediaResource.Status?.Value != MediaResourceStatus.Ready)
                    {
                        return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Background media resource '{backgroundMediaResource.Name}' for block '{block.Key}' is not ready.");
                    }

                    var backgroundIsImage = backgroundMediaResource.MimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
                    var backgroundIsVideo = backgroundMediaResource.MimeType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;
                    if (!backgroundIsImage && !backgroundIsVideo)
                    {
                        return InvokeResult<List<VideoAssemblyBlock>>.FromError($"Background media resource '{backgroundMediaResource.Name}' for block '{block.Key}' must be an image or video.");
                    }

                    var backgroundResult = await _mediaSourceResolver.ResolveAsync(backgroundMediaResource, org.Id, cancellationToken);
                    if (!backgroundResult.Successful)
                    {
                        return backgroundResult.ToInvokeResult<List<VideoAssemblyBlock>>();
                    }

                    backgroundSource = backgroundResult.Result;
                    _adminLogger.Trace($"{this.Tag()} [ASSEMBLY BACKGROUND RESOLVED] CompositionId={composition.Id}, BlockKey={block.Key}, MediaResourceId={backgroundMediaResource.Id}, FileName={backgroundSource.FileName}, ContentType={backgroundSource.ContentType}");
                }

                var backgroundAudioResult = await ResolveOptionalAssemblySourceAsync(block.BackgroundAudioMediaResource, $"background audio for block '{block.Key}'", org, user, cancellationToken);
                if (!backgroundAudioResult.Successful)
                {
                    return backgroundAudioResult.ToInvokeResult<List<VideoAssemblyBlock>>();
                }

                var images = new List<VideoAssemblyImageOverlay>();
                foreach (var image in block.OverlayImages ?? new List<VideoCompositionBlockImage>())
                {
                    var imageResult = await ResolveOptionalAssemblySourceAsync(image.MediaResource, $"overlay image for block '{block.Key}'", org, user, cancellationToken);
                    if (!imageResult.Successful)
                    {
                        return imageResult.ToInvokeResult<List<VideoAssemblyBlock>>();
                    }

                    images.Add(new VideoAssemblyImageOverlay
                    {
                        Source = imageResult.Result,
                        Scale = image.Scale,
                        PositionX = image.PositionX,
                        PositionY = image.PositionY,
                        Opacity = image.Opacity,
                        DelaySeconds = image.DelaySeconds,
                        VisibleDurationSeconds = image.VisibleDurationSeconds,
                        FadeInSeconds = image.FadeInSeconds,
                        FadeOutSeconds = image.FadeOutSeconds
                    });
                }

                assemblyBlocks.Add(new VideoAssemblyBlock
                {
                    Key = block.Key,
                    Type = block.Type == VideoCompositionBlockType.Image ? VideoAssemblyBlockType.Image : VideoAssemblyBlockType.Video,
                    Source = sourceResult.Result,
                    Background = backgroundSource,
                    PresenterLayout = backgroundSource == null
                        ? null
                        : new VideoAssemblyPresenterLayout
                        {
                            Scale = block.PresenterScale,
                            PositionX = block.PresenterPositionX,
                            PositionY = block.PresenterPositionY
                        },
                    BackgroundAudio = backgroundAudioResult.Result == null
                        ? null
                        : new VideoAssemblyAudio
                        {
                            Source = backgroundAudioResult.Result,
                            Volume = block.BackgroundAudioVolume,
                            FadeInSeconds = block.BackgroundAudioFadeInSeconds,
                            FadeOutSeconds = block.BackgroundAudioFadeOutSeconds,
                            Loop = block.LoopBackgroundAudio
                        },
                    Images = images,
                    DurationSeconds = block.DurationSeconds,
                    FadeInSeconds = block.FadeInSeconds,
                    FadeOutSeconds = block.FadeOutSeconds,
                    Labels = (block.CompositionLabels ?? new List<VideoCompositionTextLabel>()).Select(label => CreateAssemblyLabel(composition, label)).ToList()
                });
            }

            return InvokeResult<List<VideoAssemblyBlock>>.Create(assemblyBlocks);
        }

        private async Task<InvokeResult<VideoAssemblySource>> ResolveOptionalAssemblySourceAsync(EntityHeader mediaResourceHeader, string description, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            if (mediaResourceHeader == null || String.IsNullOrWhiteSpace(mediaResourceHeader.Id))
            {
                return InvokeResult<VideoAssemblySource>.Create(null);
            }

            var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(mediaResourceHeader.Id, org, user);
            if (mediaResource == null)
            {
                return InvokeResult<VideoAssemblySource>.FromError($"Could not find {description} media resource '{mediaResourceHeader.Id}'.");
            }

            if (mediaResource.Status?.Value != MediaResourceStatus.Ready)
            {
                return InvokeResult<VideoAssemblySource>.FromError($"The {description} media resource '{mediaResource.Name}' is not ready.");
            }

            return await _mediaSourceResolver.ResolveAsync(mediaResource, org.Id, cancellationToken);
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
                ExternalUrl = null,
                MimeType = "video/mp4",
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.RawVideo),
                Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending),
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

        private static bool IsAccessTokenValid(string accessToken, string expectedSha256)
        {
            if (String.IsNullOrWhiteSpace(accessToken) || String.IsNullOrWhiteSpace(expectedSha256)) return false;

            var actualBytes = Encoding.ASCII.GetBytes(ComputeSha256(accessToken));
            var expectedBytes = Encoding.ASCII.GetBytes(expectedSha256.ToLowerInvariant());
            if (actualBytes.Length != expectedBytes.Length) return false;

            var difference = 0;
            for (var index = 0; index < actualBytes.Length; index++) difference |= actualBytes[index] ^ expectedBytes[index];
            return difference == 0;
        }

        private static string ResolveVimeoVideoId(string videoUri)
        {
            if (String.IsNullOrWhiteSpace(videoUri)) return null;

            var segments = videoUri.Trim('/').Split('/');
            return segments.Length == 0 ? null : segments[segments.Length - 1];
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

            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = user;
            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending);

            return pendingRevision;
        }

        private static VideoAssemblyTextLabel CreateAssemblyLabel(VideoComposition composition, VideoCompositionTextLabel label)
        {
            return new VideoAssemblyTextLabel
            {
                Text = ResolveCompositionLabelText(composition, label),
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

        private static string ResolveCompositionLabelText(VideoComposition composition, VideoCompositionTextLabel label)
        {
            if (label == null)
            {
                return String.Empty;
            }

            string boundValue;

            switch (label.Binding)
            {
                case VideoCompositionLabelBinding.Title:
                    boundValue = composition?.Title;
                    break;

                case VideoCompositionLabelBinding.Subtitle:
                    boundValue = composition?.Subtitle;
                    break;

                case VideoCompositionLabelBinding.CallToAction:
                    boundValue = composition?.CallToAction;
                    break;

                default:
                    boundValue = null;
                    break;
            }

            return String.IsNullOrWhiteSpace(boundValue)
                ? label.Text ?? String.Empty
                : boundValue.Trim();
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

            composition.SetStatus(VideoCompositionStatus.Failed);
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

            if (!String.IsNullOrWhiteSpace(composition.NotificationRunId))
            {
                await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, composition.NotificationRunId, "video-composition-updated", composition);
            }
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
