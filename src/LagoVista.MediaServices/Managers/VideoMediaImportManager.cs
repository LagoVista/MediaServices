using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using LagoVista.VideoAssembly.Contracts;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoMediaImportManager : IVideoMediaImportManager
    {
        private readonly IVideoProductionRepo _videoProductionRepo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly IVideoProcessorStorageUrlService _videoProcessorStorageUrlService;
        private readonly IVideoProcessorRequestStore _videoProcessorRequestStore;
        private readonly IVideoProcessorCallbackRegistrationStore _videoProcessorCallbackRegistrationStore;
        private readonly IVideoProcessorLauncher _videoProcessorLauncher;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoMediaImportManager(IVideoProductionRepo videoProductionRepo, IMediaServicesManager mediaServicesManager, IHeyGenVideoService heyGenVideoService, IVideoProcessorStorageUrlService videoProcessorStorageUrlService, IVideoProcessorRequestStore videoProcessorRequestStore, IVideoProcessorCallbackRegistrationStore videoProcessorCallbackRegistrationStore, IVideoProcessorLauncher videoProcessorLauncher, ICoreAppServices coreAppServices)
        {
            _videoProductionRepo = videoProductionRepo ?? throw new ArgumentNullException(nameof(videoProductionRepo));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
            _heyGenVideoService = heyGenVideoService ?? throw new ArgumentNullException(nameof(heyGenVideoService));
            _videoProcessorStorageUrlService = videoProcessorStorageUrlService ?? throw new ArgumentNullException(nameof(videoProcessorStorageUrlService));
            _videoProcessorRequestStore = videoProcessorRequestStore ?? throw new ArgumentNullException(nameof(videoProcessorRequestStore));
            _videoProcessorCallbackRegistrationStore = videoProcessorCallbackRegistrationStore ?? throw new ArgumentNullException(nameof(videoProcessorCallbackRegistrationStore));
            _videoProcessorLauncher = videoProcessorLauncher ?? throw new ArgumentNullException(nameof(videoProcessorLauncher));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult<VideoMediaImportPreparationResult>> EnsureProviderVideoImportAsync(string productionId, double? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(productionId))
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("Video production ID is required.");
            }

            var production = await _videoProductionRepo.GetVideoProductionAsync(productionId);
            if (production == null)
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError($"Could not find video production '{productionId}'.");
            }

            if (production.OwnerOrganization == null || !String.Equals(production.OwnerOrganization.Id, org?.Id, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("The video production does not belong to the active organization.");
            }

            if (production.Status?.Value == VideoProductionStatus.ProviderVideoReady && production.FinalVideoMediaResource != null && !String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                var completedMediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(production.FinalVideoMediaResource.Id, org, user);
                if (completedMediaResource == null)
                {
                    return InvokeResult<VideoMediaImportPreparationResult>.FromError($"Could not find the completed output media resource '{production.FinalVideoMediaResource.Id}'.");
                }

                return InvokeResult<VideoMediaImportPreparationResult>.Create(new VideoMediaImportPreparationResult
                {
                    Production = production,
                    MediaResource = completedMediaResource,
                    Request = null
                });
            }

            return await PrepareProviderVideoImportAsync(productionId, thumbnailTimeSeconds, org, user, cancellationToken);
        }

        public async Task<InvokeResult<VideoMediaImportPreparationResult>> PrepareProviderVideoImportAsync(string productionId, double? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(productionId))
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("Video production ID is required.");
            }

            if (thumbnailTimeSeconds.HasValue && thumbnailTimeSeconds.Value < 0)
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("Thumbnail time cannot be negative.");
            }

            var production = await _videoProductionRepo.GetVideoProductionAsync(productionId);
            if (production == null)
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError($"Could not find video production '{productionId}'.");
            }

            if (production.OwnerOrganization == null || !String.Equals(production.OwnerOrganization.Id, org?.Id, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("The video production does not belong to the active organization.");
            }

            if (String.IsNullOrWhiteSpace(production.ProviderVideoId))
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError("The video production does not have a provider video ID.");
            }

            if (production.Status?.Value != VideoProductionStatus.ProviderCompleted && production.Status?.Value != VideoProductionStatus.ImportingProviderVideo && production.Status?.Value != VideoProductionStatus.ProviderVideoReady)
            {
                return InvokeResult<VideoMediaImportPreparationResult>.FromError($"The provider video is not ready for import. Current status: '{production.Status?.Text}'.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ImportingProviderVideo);
            production.ProviderVideoImportStartedUtc = production.ProviderVideoImportStartedUtc ?? UtcTimestamp.Now;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;
            production.ProviderVideoImportMessage = "Retrieving completed video details from HeyGen.";
            production.ProviderVideoImportPercentComplete = 1;
            production.ErrorMessage = null;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var providerResult = await _heyGenVideoService.GetVideoAsync(production.ProviderVideoId, cancellationToken);
            if (!providerResult.Successful)
            {
                await ApplyPreparationFailureAsync(production, providerResult.Errors[0].Message);
                return providerResult.ToInvokeResult<VideoMediaImportPreparationResult>();
            }

            if (!String.Equals(providerResult.Result.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                var message = $"HeyGen video '{production.ProviderVideoId}' is not complete. Current status: '{providerResult.Result.Status}'.";
                await ApplyPreparationFailureAsync(production, message);
                return InvokeResult<VideoMediaImportPreparationResult>.FromError(message);
            }

            if (String.IsNullOrWhiteSpace(providerResult.Result.VideoUrl))
            {
                const string message = "HeyGen reported that the video is complete but did not return a video URL.";
                await ApplyPreparationFailureAsync(production, message);
                return InvokeResult<VideoMediaImportPreparationResult>.FromError(message);
            }

            production.ProviderVideoUrl = providerResult.Result.VideoUrl;
            production.ProviderThumbnailUrl = providerResult.Result.ThumbnailUrl ?? production.ProviderThumbnailUrl;
            production.ProviderCaptionUrl = providerResult.Result.CaptionUrl ?? production.ProviderCaptionUrl;
            production.ProviderVideoImportMessage = "Creating the media resource for the generated video.";
            production.ProviderVideoImportPercentComplete = 3;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            MediaResource mediaResource;
            var isNewMediaResource = false;

            if (production.FinalVideoMediaResource != null && !String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(production.FinalVideoMediaResource.Id, org, user);
                if (mediaResource == null)
                {
                    return InvokeResult<VideoMediaImportPreparationResult>.FromError($"Could not find the existing output media resource '{production.FinalVideoMediaResource.Id}'.");
                }
            }
            else
            {
                mediaResource = CreateOutputMediaResource(production, org, user);
                isNewMediaResource = true;
            }

            var pendingRevision = PreparePendingRevision(mediaResource, user);
            var videoContentType = String.IsNullOrWhiteSpace(pendingRevision.MimeType) ? "video/mp4" : pendingRevision.MimeType;
            var videoWriteDestinationResult = await _videoProcessorStorageUrlService.CreateWriteDestinationAsync(org.Id, pendingRevision.StorageReferenceName, videoContentType, cancellationToken);
            if (!videoWriteDestinationResult.Successful)
            {
                await ApplyPreparationFailureAsync(production, videoWriteDestinationResult.Errors[0].Message);
                return videoWriteDestinationResult.ToInvokeResult<VideoMediaImportPreparationResult>();
            }

            var generateThumbnail = !String.IsNullOrWhiteSpace(pendingRevision.ThumbnailStorageReferenceName);
            VideoProcessorStorageDestination thumbnailWriteDestination = null;

            if (generateThumbnail)
            {
                var thumbnailWriteDestinationResult = await _videoProcessorStorageUrlService.CreateWriteDestinationAsync(org.Id, pendingRevision.ThumbnailStorageReferenceName, "image/jpeg", cancellationToken);
                if (!thumbnailWriteDestinationResult.Successful)
                {
                    await ApplyPreparationFailureAsync(production, thumbnailWriteDestinationResult.Errors[0].Message);
                    return thumbnailWriteDestinationResult.ToInvokeResult<VideoMediaImportPreparationResult>();
                }

                thumbnailWriteDestination = thumbnailWriteDestinationResult.Result;
            }

            mediaResource.Link = videoWriteDestinationResult.Result.BlobUrl;
            mediaResource.ThumbnailUrl = thumbnailWriteDestination?.BlobUrl;

            if (isNewMediaResource)
            {
                var addResult = await _mediaServicesManager.AddMediaResourceRecordAsync(mediaResource, org, user);
                if (!addResult.Successful)
                {
                    await ApplyPreparationFailureAsync(production, addResult.Errors[0].Message);
                    return addResult.ToInvokeResult<VideoMediaImportPreparationResult>();
                }

                production.FinalVideoMediaResource = mediaResource.ToEntityHeader();
            }
            else
            {
                var updateResult = await _mediaServicesManager.UpdateMediaResourceRecordAsync(mediaResource, org, user);
                if (!updateResult.Successful)
                {
                    await ApplyPreparationFailureAsync(production, updateResult.Errors[0].Message);
                    return updateResult.ToInvokeResult<VideoMediaImportPreparationResult>();
                }
            }

            var requestId = String.IsNullOrWhiteSpace(production.ProviderVideoImportRequestId) ? Guid.NewGuid().ToId().Value : production.ProviderVideoImportRequestId;
            var attemptId = Guid.NewGuid().ToId().Value;

            production.ProviderVideoImportRequestId = requestId;
            production.ProviderVideoImportAttemptId = attemptId;
            production.ProviderVideoImportMessage = "Raw video resource created. Preparing secure video and thumbnail upload destinations.";
            production.ProviderVideoImportPercentComplete = 5;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;
            production.ErrorMessage = null;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);


          
            var callbackAccessToken = CreateCallbackAccessToken();
            var callbackRegistration = new VideoProcessorCallbackRegistration
            {
                PartitionKey = requestId,
                RowKey = attemptId,
                OrganizationId = org.Id,
                JobType = VideoProcessorJobType.VideoMediaImport.ToString(),
                RequestId = requestId,
                AttemptId = attemptId,
                ProductionId = production.Id,
                MediaResourceId = mediaResource.Id,
                AccessTokenSha256 = ComputeSha256(callbackAccessToken),
                CreatedUtc = DateTime.UtcNow.ToString("o"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(90).ToString("o"),
                LastSequence = -1,
                IsCompleted = false
            };

            await _videoProcessorCallbackRegistrationStore.AddAsync(callbackRegistration, cancellationToken);

            var request = new VideoMediaImportRequest
            {
                RequestId = requestId,
                AttemptId = attemptId,
                ProductionId = production.Id,
                MediaResourceId = mediaResource.Id,
                Source = new VideoAssemblySource
                {
                    Url = providerResult.Result.VideoUrl,
                    FileName = pendingRevision.FileName,
                    ContentType = String.IsNullOrWhiteSpace(pendingRevision.MimeType) ? "video/mp4" : pendingRevision.MimeType
                },
                VideoDestination = new VideoMediaImportDestination
                {
                    UploadUrl = videoWriteDestinationResult.Result.UploadUrl,
                    MediaResourceId = mediaResource.Id,
                    StorageReferenceName = pendingRevision.StorageReferenceName,
                    FileName = pendingRevision.FileName,
                    ContentType = String.IsNullOrWhiteSpace(pendingRevision.MimeType) ? "video/mp4" : pendingRevision.MimeType
                },
                Callback = new VideoProcessorCallbackSettings
                {
                    Path = "/api/media/webhooks/video-processor",
                    AccessToken = callbackAccessToken
                },
                Thumbnail = new VideoMediaImportThumbnail
                {
                    Enabled = generateThumbnail,
                    TimeSeconds = thumbnailTimeSeconds,
                    Destination = !generateThumbnail
                        ? null
                        : new VideoMediaImportDestination
                        {
                            UploadUrl = thumbnailWriteDestination.UploadUrl,
                            MediaResourceId = mediaResource.Id,
                            StorageReferenceName = pendingRevision.ThumbnailStorageReferenceName,
                            FileName = CreateThumbnailFileName(pendingRevision, mediaResource.Id),
                            ContentType = "image/jpeg"
                        }
                },
                ExecutionOptions = new VideoMediaImportExecutionOptions
                {
                    GenerateThumbnail = generateThumbnail,
                    SendCallbacks = true
                }
            };

            var storedRequestResult = await _videoProcessorRequestStore.SaveAsync(org.Id, request.JobType.ToString(), requestId, attemptId, request, cancellationToken);
            if (!storedRequestResult.Successful)
            {
                await _videoProcessorCallbackRegistrationStore.DeleteAsync(requestId, attemptId, cancellationToken);
                await ApplyPreparationFailureAsync(production, storedRequestResult.Errors[0].Message);
                return storedRequestResult.ToInvokeResult<VideoMediaImportPreparationResult>();
            }

            production.ProviderVideoImportRequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName;
            production.ProviderVideoImportRequestBlobUrl = storedRequestResult.Result.BlobUrl;
            production.ProviderVideoImportRequestUrl = storedRequestResult.Result.RequestUrl;
            production.ProviderVideoImportMessage = "Video import request prepared and ready for launch.";
            production.ProviderVideoImportPercentComplete = 7;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var launchResult = await _videoProcessorLauncher.LaunchAsync(new VideoProcessorLaunchRequest
            {
                JobType = request.JobType,
                ProductionId = production.Id,
                RequestId = requestId,
                AttemptId = attemptId,
                RequestUrl = storedRequestResult.Result.RequestUrl
            }, cancellationToken);

            if (!launchResult.Successful)
            {
                await ApplyPreparationFailureAsync(production, launchResult.Errors[0].Message);
                return launchResult.ToInvokeResult<VideoMediaImportPreparationResult>();
            }

            production.ProviderVideoImportLaunchProvider = launchResult.Result.Provider;
            production.ProviderVideoImportLaunchId = launchResult.Result.LaunchId;
            production.ProviderVideoImportLaunchNamespace = launchResult.Result.Namespace;
            production.ProviderVideoImportLaunchJobName = launchResult.Result.JobName;
            production.ProviderVideoImportLaunchedUtc = launchResult.Result.LaunchedUtc;
            production.ProviderVideoImportMessage = "Video import processor launched.";
            production.ProviderVideoImportPercentComplete = 8;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoMediaImportPreparationResult>.Create(new VideoMediaImportPreparationResult
            {
                Production = production,
                MediaResource = mediaResource,
                Request = request,
                AttemptId = attemptId,
                RequestStorageReferenceName = storedRequestResult.Result.StorageReferenceName,
                RequestBlobUrl = storedRequestResult.Result.BlobUrl,
                RequestUrl = storedRequestResult.Result.RequestUrl
            });
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

        private static MediaResourceHistory PreparePendingRevision(MediaResource mediaResource, EntityHeader user)
        {
            var pendingRevision = mediaResource.GetPendingRevision();
            if (pendingRevision == null || pendingRevision.Status?.Value != MediaResourceStatus.Pending)
            {
                pendingRevision = new MediaResourceHistory
                {
                    Name = $"Raw video import {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
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

            if (String.IsNullOrWhiteSpace(mediaResource.CurrentRevision))
            {
                mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Pending);
            }

            return pendingRevision;
        }

        private static string CreateThumbnailFileName(MediaResourceHistory revision, string mediaResourceId)
        {
            if (!String.IsNullOrWhiteSpace(revision.FileName))
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(revision.FileName)}-thumbnail.jpg";
            }

            return $"{mediaResourceId}-thumbnail.jpg";
        }

        public async Task<InvokeResult<VideoProduction>> ApplyVideoProcessorCallbackAsync(VideoProcessorJobCallback callback, string accessToken, CancellationToken cancellationToken = default)
        {
            if (callback == null)
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback is required.");
            }

            if (String.IsNullOrWhiteSpace(callback.RequestId) || String.IsNullOrWhiteSpace(callback.AttemptId))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback request ID and attempt ID are required.");
            }

            if (String.IsNullOrWhiteSpace(callback.ProductionId) || String.IsNullOrWhiteSpace(callback.MediaResourceId))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback production ID and media resource ID are required.");
            }

            if (String.IsNullOrWhiteSpace(accessToken))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback bearer token is required.");
            }

            var registration = await _videoProcessorCallbackRegistrationStore.GetAsync(callback.RequestId, callback.AttemptId, cancellationToken);
            if (registration == null)
            {
                return InvokeResult<VideoProduction>.FromError($"Could not find callback registration for request '{callback.RequestId}' attempt '{callback.AttemptId}'.");
            }

            if (!IsCallbackAccessTokenValid(accessToken, registration.AccessTokenSha256))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback bearer token is invalid.");
            }

            if (!DateTime.TryParse(registration.ExpiresUtc, out var expiresUtc) || expiresUtc.ToUniversalTime() <= DateTime.UtcNow)
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback registration has expired.");
            }

            if (registration.IsCompleted)
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback registration is already complete.");
            }

            if (!String.Equals(registration.JobType, callback.JobType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError($"Callback job type '{callback.JobType}' does not match registered job type '{registration.JobType}'.");
            }

            if (!String.Equals(registration.ProductionId, callback.ProductionId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback production ID does not match the registered production.");
            }

            if (!String.Equals(registration.MediaResourceId, callback.MediaResourceId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback media resource ID does not match the registered media resource.");
            }

            if (callback.Sequence <= registration.LastSequence)
            {
                return InvokeResult<VideoProduction>.FromError($"Video processor callback sequence '{callback.Sequence}' is not newer than the last accepted sequence '{registration.LastSequence}'.");
            }

            if (callback.JobType != VideoProcessorJobType.VideoMediaImport)
            {
                return InvokeResult<VideoProduction>.FromError($"Video processor job type '{callback.JobType}' is not supported by this callback handler.");
            }

            var production = await _videoProductionRepo.GetVideoProductionAsync(callback.ProductionId);
            if (production == null)
            {
                return InvokeResult<VideoProduction>.FromError($"Could not find video production '{callback.ProductionId}'.");
            }

            if (!String.Equals(production.ProviderVideoImportRequestId, callback.RequestId, StringComparison.OrdinalIgnoreCase) || !String.Equals(production.ProviderVideoImportAttemptId, callback.AttemptId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError("Video processor callback does not match the active import request and attempt.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var callbackTimestamp = String.IsNullOrWhiteSpace(callback.TimestampUtc) ? UtcTimestamp.Now.Value : callback.TimestampUtc;

            production.ProviderVideoImportStage = callback.Stage;
            production.ProviderVideoImportMessage = callback.Message;
            production.ProviderVideoImportPercentComplete = callback.PercentComplete;
            production.ProviderVideoImportBytesCompleted = callback.BytesCompleted;
            production.ProviderVideoImportBytesTotal = callback.BytesTotal;
            production.ProviderVideoImportLastUpdatedUtc = callbackTimestamp;

            switch (callback.Type)
            {
                case VideoAssemblyCallbackType.Completed:
                    production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderVideoReady);
                    production.ProviderVideoImportCompletedUtc = callbackTimestamp;
                    production.ProviderVideoImportPercentComplete = 100;
                    production.ProviderVideoImportMessage = String.IsNullOrWhiteSpace(callback.Message) ? "Generated video is ready in the media library." : callback.Message;
                    production.ErrorMessage = null;
                    await UpdateCompletedMediaResourceAsync(production, callback);
                    break;

                case VideoAssemblyCallbackType.Failed:
                    production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
                    production.ErrorMessage = String.IsNullOrWhiteSpace(callback.ErrorMessage) ? callback.Message : callback.ErrorMessage;
                    break;

                default:
                    production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ImportingProviderVideo);
                    production.ErrorMessage = null;
                    break;
            }

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            registration.LastSequence = callback.Sequence;
            registration.LastCallbackUtc = callbackTimestamp;

            if (callback.Type == VideoAssemblyCallbackType.Completed || callback.Type == VideoAssemblyCallbackType.Failed)
            {
                registration.IsCompleted = true;
                registration.CompletedUtc = callbackTimestamp;
            }

            await _videoProcessorCallbackRegistrationStore.UpdateAsync(registration, cancellationToken);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private async Task UpdateCompletedMediaResourceAsync(VideoProduction production, VideoProcessorJobCallback callback)
        {
            if (production.FinalVideoMediaResource == null || String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                return;
            }

            var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(production.FinalVideoMediaResource.Id, production.OwnerOrganization, production.LastUpdatedBy ?? production.CreatedBy);
            if (mediaResource == null)
            {
                return;
            }

            var videoOutput = callback.Outputs?.FirstOrDefault(output => output.Type == VideoProcessorOutputArtifactType.Video);
            var thumbnailOutput = callback.Outputs?.FirstOrDefault(output => output.Type == VideoProcessorOutputArtifactType.Thumbnail);

            if (videoOutput != null)
            {
                mediaResource.ContentSize = videoOutput.SizeBytes ?? mediaResource.ContentSize;
                mediaResource.DurationSeconds = videoOutput.DurationSeconds ?? mediaResource.DurationSeconds;
                mediaResource.Width = videoOutput.Width ?? mediaResource.Width;
                mediaResource.Height = videoOutput.Height ?? mediaResource.Height;
            }

            if (thumbnailOutput != null && !String.IsNullOrWhiteSpace(thumbnailOutput.ExternalUri))
            {
                mediaResource.ThumbnailUrl = thumbnailOutput.ExternalUri;
            }

            mediaResource.ProcessingCompletedUtc = String.IsNullOrWhiteSpace(callback.TimestampUtc) ? UtcTimestamp.Now.Value : callback.TimestampUtc;
            mediaResource.ProcessingErrorMessage = null;
            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready);
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = production.LastUpdatedBy ?? production.CreatedBy;

            await _mediaServicesManager.UpdateMediaResourceRecordAsync(mediaResource, production.OwnerOrganization, mediaResource.LastUpdatedBy);
        }

        private static bool IsCallbackAccessTokenValid(string accessToken, string expectedSha256)
        {
            if (String.IsNullOrWhiteSpace(accessToken) || String.IsNullOrWhiteSpace(expectedSha256))
            {
                return false;
            }

            var actualSha256 = ComputeSha256(accessToken);
            var actualBytes = Encoding.ASCII.GetBytes(actualSha256);
            var expectedBytes = Encoding.ASCII.GetBytes(expectedSha256.ToLowerInvariant());

            if (actualBytes.Length != expectedBytes.Length)
            {
                return false;
            }

            var difference = 0;

            for (var index = 0; index < actualBytes.Length; index++)
            {
                difference |= actualBytes[index] ^ expectedBytes[index];
            }

            return difference == 0;
        }

        private static MediaResource CreateOutputMediaResource(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            var id = Guid.NewGuid().ToId();
            var now = UtcTimestamp.Now;
            var fileName = CreateVideoFileName(production);
            var mediaResource = new MediaResource
            {
                Id = id,
                Name = String.IsNullOrWhiteSpace(production.VideoName) ? production.Name : production.VideoName,
                Key = $"rawvideo{DateTime.UtcNow.Ticks}",
                Description = production.Description,
                FileName = fileName,
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
                SourceEntityType = nameof(VideoProduction),
                SourceEntity = production.ToEntityHeader(),
                OriginalUrl = production.ProviderVideoUrl,
                DurationSeconds = production.ActualDurationSeconds
            };

            return mediaResource;
        }

        private static string CreateVideoFileName(VideoProduction production)
        {
            var name = String.IsNullOrWhiteSpace(production.VideoName) ? production.Name : production.VideoName;
            if (String.IsNullOrWhiteSpace(name))
            {
                return $"generated-video-{DateTime.UtcNow:yyyyMMddHHmmss}.mp4";
            }

            foreach (var invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidCharacter, '-');
            }

            return name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.mp4";
        }

        private async Task ApplyPreparationFailureAsync(VideoProduction production, string message)
        {
            production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
            production.ProviderVideoImportMessage = message;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;
            production.ErrorMessage = message;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);
        }

        private async Task PublishVideoProductionUpdatedAsync(VideoProduction production)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, production.Id, "video-production-updated", production);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, production.OwnerOrganization.Id, "video-production-updated", production);
        }
    }
}
