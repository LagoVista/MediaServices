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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoAssemblyCallbackHandler : IVideoAssemblyCallbackHandler
    {
        private readonly IVideoCompositionRepo _videoCompositionRepo;
        private readonly IMediaServicesRepo _mediaServicesRepo;
        private readonly IVideoProcessorCallbackRegistrationStore _callbackRegistrationStore;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger _adminLogger;

        public VideoAssemblyCallbackHandler(IVideoCompositionRepo videoCompositionRepo, IMediaServicesRepo mediaServicesRepo, IVideoProcessorCallbackRegistrationStore callbackRegistrationStore, ICoreAppServices coreAppServices)
        {
            _videoCompositionRepo = videoCompositionRepo ?? throw new ArgumentNullException(nameof(videoCompositionRepo));
            _mediaServicesRepo = mediaServicesRepo ?? throw new ArgumentNullException(nameof(mediaServicesRepo));
            _callbackRegistrationStore = callbackRegistrationStore ?? throw new ArgumentNullException(nameof(callbackRegistrationStore));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
            _adminLogger = coreAppServices?.Logger ?? throw new ArgumentNullException(nameof(coreAppServices.Logger));
        }

        public async Task<InvokeResult<VideoComposition>> ApplyAsync(VideoProcessorJobCallback callback, string accessToken, CancellationToken cancellationToken = default)
        {
            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK RECEIVED] JobType={callback?.JobType}, CompositionId={callback?.ProductionId}, MediaResourceId={callback?.MediaResourceId}, RequestId={callback?.RequestId}, AttemptId={callback?.AttemptId}, Sequence={callback?.Sequence}, Type={callback?.Type}, Stage={callback?.Stage}");

            if (callback == null)
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback is required.");
            }

            if (callback.JobType != VideoProcessorJobType.VideoAssembly)
            {
                return InvokeResult<VideoComposition>.FromError($"Video processor job type '{callback.JobType}' is not supported by the assembly callback handler.");
            }

            if (String.IsNullOrWhiteSpace(callback.RequestId) || String.IsNullOrWhiteSpace(callback.AttemptId))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback request ID and attempt ID are required.");
            }

            if (String.IsNullOrWhiteSpace(callback.ProductionId) || String.IsNullOrWhiteSpace(callback.MediaResourceId))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback composition ID and media resource ID are required.");
            }

            if (String.IsNullOrWhiteSpace(accessToken))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback bearer token is required.");
            }

            var registration = await _callbackRegistrationStore.GetAsync(callback.RequestId, callback.AttemptId, cancellationToken);
            if (registration == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find callback registration for request '{callback.RequestId}' attempt '{callback.AttemptId}'.");
            }

            if (!IsCallbackAccessTokenValid(accessToken, registration.AccessTokenSha256))
            {
                _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK REJECTED] RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Reason=Invalid access token.");
                return InvokeResult<VideoComposition>.FromError("Video assembly callback bearer token is invalid.");
            }

            if (!DateTime.TryParse(registration.ExpiresUtc, out var expiresUtc) || expiresUtc.ToUniversalTime() <= DateTime.UtcNow)
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback registration has expired.");
            }

            if (registration.IsCompleted)
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback registration is already complete.");
            }

            if (!String.Equals(registration.JobType, callback.JobType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoComposition>.FromError($"Callback job type '{callback.JobType}' does not match registered job type '{registration.JobType}'.");
            }

            if (!String.Equals(registration.ProductionId, callback.ProductionId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback composition ID does not match the registered composition.");
            }

            if (!String.Equals(registration.MediaResourceId, callback.MediaResourceId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback media resource ID does not match the registered output media resource.");
            }

            if (callback.Sequence <= registration.LastSequence)
            {
                return InvokeResult<VideoComposition>.FromError($"Video assembly callback sequence '{callback.Sequence}' is not newer than the last accepted sequence '{registration.LastSequence}'.");
            }

            var composition = await _videoCompositionRepo.GetVideoCompositionAsync(callback.ProductionId);
            if (composition == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find video composition '{callback.ProductionId}'.");
            }

            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();

            if (!String.Equals(composition.AssemblyState.RequestId, callback.RequestId, StringComparison.OrdinalIgnoreCase) || !String.Equals(composition.AssemblyState.AttemptId, callback.AttemptId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoComposition>.FromError("Video assembly callback does not match the active request and attempt.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var callbackTimestamp = String.IsNullOrWhiteSpace(callback.TimestampUtc) ? UtcTimestamp.Now.Value : callback.TimestampUtc;
            var isVimeoPublish = composition.PublishedVideoMediaResource != null && String.Equals(composition.PublishedVideoMediaResource.Id, callback.MediaResourceId, StringComparison.OrdinalIgnoreCase);

            composition.AssemblyState.Stage = MapStage(callback.Stage);
            composition.AssemblyState.PercentComplete = callback.PercentComplete;
            composition.AssemblyState.Message = callback.Message;
            composition.AssemblyState.BytesCompleted = callback.BytesCompleted;
            composition.AssemblyState.BytesTotal = callback.BytesTotal;
            composition.AssemblyState.LastSequence = callback.Sequence;
            composition.AssemblyState.LastUpdatedUtc = callbackTimestamp;

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK VALIDATED] CompositionId={composition.Id}, RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Sequence={callback.Sequence}, Type={callback.Type}, Stage={callback.Stage}, PercentComplete={callback.PercentComplete}");

            switch (callback.Type)
            {
                case VideoAssemblyCallbackType.Started:
                    composition.SetStatus(isVimeoPublish ? VideoCompositionStatus.ProcessingAtVimeo : VideoCompositionStatus.Assembling);
                    composition.AssemblyState.StartedUtc = composition.AssemblyState.StartedUtc ?? callbackTimestamp;
                    composition.ErrorMessage = null;
                    break;

                case VideoAssemblyCallbackType.Progress:
                    composition.SetStatus(isVimeoPublish ? VideoCompositionStatus.ProcessingAtVimeo : IsUploadStage(callback.Stage) ? VideoCompositionStatus.Uploading : VideoCompositionStatus.Assembling);
                    composition.ErrorMessage = null;
                    break;

                case VideoAssemblyCallbackType.Completed:
                    _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PROCESSOR COMPLETED] CompositionId={composition.Id}, MediaResourceId={callback.MediaResourceId}, RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Sequence={callback.Sequence}, OutputCount={callback.Outputs?.Count ?? 0}, IsVimeoPublish={isVimeoPublish}");
                    var mediaResourceUpdateResult = isVimeoPublish ? await UpdateCompletedVimeoMediaResourceAsync(composition, callback) : await UpdateCompletedMediaResourceAsync(composition, callback);
                    if (!mediaResourceUpdateResult.Successful)
                    {
                        _adminLogger.Trace($"{this.Tag()} [ASSEMBLY COMPLETION REJECTED] CompositionId={composition.Id}, MediaResourceId={callback.MediaResourceId}, RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Message={mediaResourceUpdateResult.Errors[0].Message}");
                        return mediaResourceUpdateResult.ToInvokeResult<VideoComposition>();
                    }



                    var completedVideoOutput = mediaResourceUpdateResult.Result;
                    composition.OutputInputSha256 = composition.ExecutionInputSha256;
                    composition.IsReady = true;
                    composition.CompletedUtc = UtcTimestamp.Now;
                    composition.ErrorMessage = null;
                    composition.SetStatus(VideoCompositionStatus.Completed);
                    composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Completed;
                    composition.AssemblyState.PercentComplete = 100;
                    composition.AssemblyState.CompletedUtc = callbackTimestamp;
                    composition.AssemblyState.OutputSizeBytes = completedVideoOutput.SizeBytes.Value;
                    composition.AssemblyState.OutputDurationSeconds = completedVideoOutput.DurationSeconds.Value;
                    composition.AssemblyState.OutputSha256 = completedVideoOutput.Sha256;
                    if (!isVimeoPublish) composition.CompletedUtc = callbackTimestamp;
                    composition.ErrorMessage = null;
                    break;

                case VideoAssemblyCallbackType.Failed:
                    _adminLogger.Trace($"{this.Tag()} [ASSEMBLY PROCESSOR FAILED] CompositionId={composition.Id}, MediaResourceId={callback.MediaResourceId}, RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Sequence={callback.Sequence}, Error={callback.ErrorMessage ?? callback.Message}, IsVimeoPublish={isVimeoPublish}");
                    composition.SetStatus(VideoCompositionStatus.Failed);
                    composition.AssemblyState.Stage = VideoCompositionAssemblyStage.Failed;
                    composition.AssemblyState.ErrorMessage = String.IsNullOrWhiteSpace(callback.ErrorMessage) ? callback.Message : callback.ErrorMessage;
                    composition.ErrorMessage = composition.AssemblyState.ErrorMessage;
                    if (isVimeoPublish) await UpdateFailedVimeoMediaResourceAsync(composition, callback.MediaResourceId, composition.AssemblyState.ErrorMessage, callbackTimestamp);
                    break;
            }

            await _videoCompositionRepo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            registration.LastSequence = callback.Sequence;
            registration.LastCallbackUtc = callbackTimestamp;

            if (callback.Type == VideoAssemblyCallbackType.Completed || callback.Type == VideoAssemblyCallbackType.Failed)
            {
                registration.IsCompleted = true;
                registration.CompletedUtc = callbackTimestamp;
            }

            await _callbackRegistrationStore.UpdateAsync(registration, cancellationToken);

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY CALLBACK APPLIED] CompositionId={composition.Id}, RequestId={callback.RequestId}, AttemptId={callback.AttemptId}, Sequence={callback.Sequence}, Status={composition.Status?.Value}, RegistrationCompleted={registration.IsCompleted}");

            return InvokeResult<VideoComposition>.Create(composition);
        }

        private async Task<InvokeResult<VideoProcessorOutputArtifact>> UpdateCompletedMediaResourceAsync(VideoComposition composition, VideoProcessorJobCallback callback)
        {
            if (composition.OutputMediaResource == null || String.IsNullOrWhiteSpace(composition.OutputMediaResource.Id))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video composition does not have an output media resource.");
            }

            var mediaResource = await _mediaServicesRepo.GetMediaResourceRecordAsync(composition.OutputMediaResource.Id);
            if (mediaResource == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError($"Could not find output media resource '{composition.OutputMediaResource.Id}'.");
            }

            var videoOutput = callback.Outputs?.FirstOrDefault(output => output.Type == VideoProcessorOutputArtifactType.Video);
            if (videoOutput == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video assembly callback did not contain a video output artifact.");
            }

            if (String.IsNullOrWhiteSpace(videoOutput.StorageReferenceName))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video output artifact did not contain a storage reference name.");
            }

            if (!videoOutput.SizeBytes.HasValue || videoOutput.SizeBytes.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video output artifact did not contain a valid file size.");
            }

            if (!videoOutput.DurationSeconds.HasValue || videoOutput.DurationSeconds.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video output artifact did not contain a valid duration.");
            }

            if (!videoOutput.Width.HasValue || videoOutput.Width.Value <= 0 || !videoOutput.Height.HasValue || videoOutput.Height.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video output artifact did not contain valid dimensions.");
            }

            if (String.IsNullOrWhiteSpace(videoOutput.Sha256))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video output artifact did not contain a SHA-256 hash.");
            }

            var thumbnailOutput = callback.Outputs?.FirstOrDefault(output => output.Type == VideoProcessorOutputArtifactType.Thumbnail);
            if (thumbnailOutput == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed video assembly callback did not contain a thumbnail output artifact.");
            }

            if (String.IsNullOrWhiteSpace(thumbnailOutput.StorageReferenceName))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed thumbnail output artifact did not contain a storage reference name.");
            }

            var pendingRevision = mediaResource.GetPendingRevision();
            if (pendingRevision == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError($"Output media resource '{mediaResource.Id}' does not have the expected pending revision.");
            }

            mediaResource.StorageReferenceName = videoOutput.StorageReferenceName;
            if (!String.IsNullOrWhiteSpace(videoOutput.ExternalUri)) mediaResource.Link = videoOutput.ExternalUri;
            mediaResource.ContentSize = videoOutput.SizeBytes.Value;
            mediaResource.DurationSeconds = videoOutput.DurationSeconds.Value;
            mediaResource.Width = videoOutput.Width.Value;
            mediaResource.Height = videoOutput.Height.Value;
            mediaResource.ContentSha256 = videoOutput.Sha256;
            mediaResource.ThumbnailStorageReferenceName = thumbnailOutput.StorageReferenceName;
            if (!String.IsNullOrWhiteSpace(thumbnailOutput.ExternalUri)) mediaResource.ThumbnailUrl = thumbnailOutput.ExternalUri;

            pendingRevision.StorageReferenceName = videoOutput.StorageReferenceName;
            pendingRevision.ThumbnailStorageReferenceName = thumbnailOutput.StorageReferenceName;
            pendingRevision.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready);

            mediaResource.CurrentRevision = pendingRevision.Id;
            mediaResource.PendingRevision = null;
            mediaResource.ProcessingCompletedUtc = String.IsNullOrWhiteSpace(callback.TimestampUtc) ? UtcTimestamp.Now.Value : callback.TimestampUtc;
            mediaResource.ProcessingErrorMessage = null;
            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready);
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = composition.LastUpdatedBy ?? composition.CreatedBy;

            await _mediaServicesRepo.UpdateMediaResourceRecordAsync(mediaResource);

            _adminLogger.Trace($"{this.Tag()} [ASSEMBLY MEDIA RESOURCE READY] CompositionId={composition.Id}, MediaResourceId={mediaResource.Id}, StorageReferenceName={mediaResource.StorageReferenceName}, ThumbnailStorageReferenceName={mediaResource.ThumbnailStorageReferenceName}, ContentSize={mediaResource.ContentSize}, DurationSeconds={mediaResource.DurationSeconds}, Width={mediaResource.Width}, Height={mediaResource.Height}, Sha256={mediaResource.ContentSha256}");

            return InvokeResult<VideoProcessorOutputArtifact>.Create(videoOutput);
        }

        private async Task<InvokeResult<VideoProcessorOutputArtifact>> UpdateCompletedVimeoMediaResourceAsync(VideoComposition composition, VideoProcessorJobCallback callback)
        {
            if (composition.PublishedVideoMediaResource == null || String.IsNullOrWhiteSpace(composition.PublishedVideoMediaResource.Id))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo publishing operation does not have a published video media resource.");
            }

            var mediaResource = await _mediaServicesRepo.GetMediaResourceRecordAsync(composition.PublishedVideoMediaResource.Id);
            if (mediaResource == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError($"Could not find Vimeo media resource '{composition.PublishedVideoMediaResource.Id}'.");
            }

            var videoOutput = callback.Outputs?.FirstOrDefault(output => output.Type == VideoProcessorOutputArtifactType.Video && !String.IsNullOrWhiteSpace(output.ExternalUri));
            if (videoOutput == null)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo publishing callback did not contain a Vimeo video output artifact.");
            }

            if (!String.Equals(videoOutput.MediaResourceId, mediaResource.Id, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo output artifact does not match the published video media resource.");
            }

            if (!videoOutput.SizeBytes.HasValue || videoOutput.SizeBytes.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo output artifact did not contain a valid file size.");
            }

            if (!videoOutput.DurationSeconds.HasValue || videoOutput.DurationSeconds.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo output artifact did not contain a valid duration.");
            }

            if (!videoOutput.Width.HasValue || videoOutput.Width.Value <= 0 || !videoOutput.Height.HasValue || videoOutput.Height.Value <= 0)
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo output artifact did not contain valid dimensions.");
            }

            if (String.IsNullOrWhiteSpace(videoOutput.Sha256))
            {
                return InvokeResult<VideoProcessorOutputArtifact>.FromError("The completed Vimeo output artifact did not contain a SHA-256 hash.");
            }

            mediaResource.Link = videoOutput.ExternalUri;
            mediaResource.OriginalUrl = videoOutput.ExternalId;
            mediaResource.ContentSize = videoOutput.SizeBytes.Value;
            mediaResource.DurationSeconds = videoOutput.DurationSeconds.Value;
            mediaResource.Width = videoOutput.Width.Value;
            mediaResource.Height = videoOutput.Height.Value;
            mediaResource.ContentSha256 = videoOutput.Sha256;
            mediaResource.ProcessingCompletedUtc = String.IsNullOrWhiteSpace(callback.TimestampUtc) ? UtcTimestamp.Now.Value : callback.TimestampUtc;
            mediaResource.ProcessingErrorMessage = null;
            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready);
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = composition.LastUpdatedBy ?? composition.CreatedBy;

            composition.VimeoVideoUri = videoOutput.ExternalUri;
            composition.VimeoVideoId = videoOutput.ExternalId;

            await _mediaServicesRepo.UpdateMediaResourceRecordAsync(mediaResource);

            _adminLogger.Trace($"{this.Tag()} [VIMEO MEDIA RESOURCE READY] CompositionId={composition.Id}, MediaResourceId={mediaResource.Id}, VimeoVideoUri={composition.VimeoVideoUri}, VimeoVideoId={composition.VimeoVideoId}, ContentSize={mediaResource.ContentSize}, DurationSeconds={mediaResource.DurationSeconds}, Width={mediaResource.Width}, Height={mediaResource.Height}, Sha256={mediaResource.ContentSha256}");

            return InvokeResult<VideoProcessorOutputArtifact>.Create(videoOutput);
        }

        private async Task UpdateFailedVimeoMediaResourceAsync(VideoComposition composition, string mediaResourceId, string errorMessage, string callbackTimestamp)
        {
            if (String.IsNullOrWhiteSpace(mediaResourceId)) return;

            var mediaResource = await _mediaServicesRepo.GetMediaResourceRecordAsync(mediaResourceId);
            if (mediaResource == null) return;

            mediaResource.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Failed);
            mediaResource.ProcessingCompletedUtc = callbackTimestamp;
            mediaResource.ProcessingErrorMessage = errorMessage;
            mediaResource.LastUpdatedDate = UtcTimestamp.Now;
            mediaResource.LastUpdatedBy = composition.LastUpdatedBy ?? composition.CreatedBy;

            await _mediaServicesRepo.UpdateMediaResourceRecordAsync(mediaResource);
        }

        private async Task PublishVideoCompositionUpdatedAsync(VideoComposition composition)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, composition.Id, "video-composition-updated", composition);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, composition.OwnerOrganization.Id, "video-composition-updated", composition);
        }

        private static VideoCompositionAssemblyStage MapStage(string stage)
        {
            if (Enum.TryParse<VideoCompositionAssemblyStage>(stage, true, out var parsedStage))
            {
                return parsedStage;
            }

            return VideoCompositionAssemblyStage.None;
        }

        private static bool IsUploadStage(string stage)
        {
            return String.Equals(stage, VideoAssemblyStage.UploadingToAzure.ToString(), StringComparison.OrdinalIgnoreCase)
                || String.Equals(stage, VideoAssemblyStage.UploadingThumbnail.ToString(), StringComparison.OrdinalIgnoreCase)
                || String.Equals(stage, VideoAssemblyStage.UploadingToVimeo.ToString(), StringComparison.OrdinalIgnoreCase);
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

        private static string ComputeSha256(string value)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
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
