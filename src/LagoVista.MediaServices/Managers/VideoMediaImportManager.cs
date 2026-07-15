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
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoMediaImportManager : IVideoMediaImportManager
    {
        private readonly IVideoProductionRepo _videoProductionRepo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoMediaImportManager(IVideoProductionRepo videoProductionRepo, IMediaServicesManager mediaServicesManager, IHeyGenVideoService heyGenVideoService, ICoreAppServices coreAppServices)
        {
            _videoProductionRepo = videoProductionRepo ?? throw new ArgumentNullException(nameof(videoProductionRepo));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
            _heyGenVideoService = heyGenVideoService ?? throw new ArgumentNullException(nameof(heyGenVideoService));
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

                var addResult = await _mediaServicesManager.AddMediaResourceRecordAsync(mediaResource, org, user);
                if (!addResult.Successful)
                {
                    await ApplyPreparationFailureAsync(production, addResult.Errors[0].Message);
                    return addResult.ToInvokeResult<VideoMediaImportPreparationResult>();
                }

                production.FinalVideoMediaResource = mediaResource.ToEntityHeader();
            }

            var requestId = String.IsNullOrWhiteSpace(production.ProviderVideoImportRequestId) ? Guid.NewGuid().ToId().Value : production.ProviderVideoImportRequestId;

            production.ProviderVideoImportRequestId = requestId;
            production.ProviderVideoImportMessage = "Media resource created. Preparing secure video and thumbnail upload destinations.";
            production.ProviderVideoImportPercentComplete = 5;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;
            production.ErrorMessage = null;

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var request = new VideoMediaImportRequest
            {
                RequestId = requestId,
                ProductionId = production.Id,
                MediaResourceId = mediaResource.Id,
                Source = new VideoAssemblySource
                {
                    Url = providerResult.Result.VideoUrl,
                    FileName = mediaResource.FileName,
                    ContentType = String.IsNullOrWhiteSpace(mediaResource.MimeType) ? "video/mp4" : mediaResource.MimeType
                },
                VideoDestination = new VideoMediaImportDestination
                {
                    StorageReferenceName = mediaResource.StorageReferenceName,
                    FileName = mediaResource.FileName,
                    ContentType = String.IsNullOrWhiteSpace(mediaResource.MimeType) ? "video/mp4" : mediaResource.MimeType
                },
                Thumbnail = new VideoMediaImportThumbnail
                {
                    Enabled = !String.IsNullOrWhiteSpace(mediaResource.ThumbnailStorageReferenceName),
                    TimeSeconds = thumbnailTimeSeconds,
                    Destination = String.IsNullOrWhiteSpace(mediaResource.ThumbnailStorageReferenceName)
                      ? null
                      : new VideoMediaImportDestination
                      {
                          StorageReferenceName = mediaResource.ThumbnailStorageReferenceName,
                          FileName = CreateThumbnailFileName(mediaResource),
                          ContentType = "image/jpeg"
                      }
                }
            };

            return InvokeResult<VideoMediaImportPreparationResult>.Create(new VideoMediaImportPreparationResult
            {
                Production = production,
                MediaResource = mediaResource,
                Request = request
            });
        }

        private static string CreateThumbnailFileName(MediaResource mediaResource)
        {
            if (!String.IsNullOrWhiteSpace(mediaResource.FileName))
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(mediaResource.FileName)}-thumbnail.jpg";
            }

            return $"{mediaResource.Id}-thumbnail.jpg";
        }

        public async Task<InvokeResult<VideoProduction>> ApplyVideoMediaImportCallbackAsync(VideoMediaImportCallback callback, CancellationToken cancellationToken = default)
        {
            if (callback == null)
            {
                return InvokeResult<VideoProduction>.FromError("Video media import callback is required.");
            }

            if (String.IsNullOrWhiteSpace(callback.ProductionId))
            {
                return InvokeResult<VideoProduction>.FromError("Video production ID is required.");
            }

            var production = await _videoProductionRepo.GetVideoProductionAsync(callback.ProductionId);
            if (production == null)
            {
                return InvokeResult<VideoProduction>.FromError($"Could not find video production '{callback.ProductionId}'.");
            }

            if (!String.IsNullOrWhiteSpace(production.ProviderVideoImportRequestId) && !String.Equals(production.ProviderVideoImportRequestId, callback.RequestId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError($"Import request '{callback.RequestId}' does not match the active request '{production.ProviderVideoImportRequestId}'.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            production.ProviderVideoImportStage = callback.Stage;
            production.ProviderVideoImportMessage = callback.Message;
            production.ProviderVideoImportPercentComplete = callback.PercentComplete;
            production.ProviderVideoImportBytesCompleted = callback.BytesCompleted;
            production.ProviderVideoImportBytesTotal = callback.BytesTotal;
            production.ProviderVideoImportLastUpdatedUtc = UtcTimestamp.Now;

            if (!String.IsNullOrWhiteSpace(callback.ErrorMessage))
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
                production.ErrorMessage = callback.ErrorMessage;
            }
            else if (!String.IsNullOrWhiteSpace(callback.CompletedUtc))
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderVideoReady);
                production.ProviderVideoImportCompletedUtc = callback.CompletedUtc;
                production.ProviderVideoImportPercentComplete = 100;
                production.ProviderVideoImportMessage = String.IsNullOrWhiteSpace(callback.Message) ? "Generated video is ready in the media library." : callback.Message;
                production.ErrorMessage = null;

                await UpdateCompletedMediaResourceAsync(production, callback);
            }
            else
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ImportingProviderVideo);
                production.ErrorMessage = null;
            }

            await _videoProductionRepo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private async Task UpdateCompletedMediaResourceAsync(VideoProduction production, VideoMediaImportCallback callback)
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

            mediaResource.ContentSize = callback.ContentSize ?? mediaResource.ContentSize;
            mediaResource.DurationSeconds = callback.DurationSeconds ?? mediaResource.DurationSeconds;
            mediaResource.Width = callback.Width ?? mediaResource.Width;
            mediaResource.Height = callback.Height ?? mediaResource.Height;
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = production.LastUpdatedBy ?? production.CreatedBy;

            await _mediaServicesManager.UpdateMediaResourceRecordAsync(mediaResource, production.OwnerOrganization, mediaResource.LastUpdatedBy);
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
                Key = $"video{DateTime.UtcNow.Ticks}",
                Description = production.Description,
                FileName = fileName,
                IsFileUpload = true,
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

            mediaResource.SetContentType("video/mp4", id);
            mediaResource.ThumbnailStorageReferenceName = $"{Guid.NewGuid().ToId()}.jpg";
            mediaResource.ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Video);
            mediaResource.MimeType = "video/mp4";

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
