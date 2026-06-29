using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoGenerationManager : IVideoGenerationManager
    {
        private readonly IVideoGenerationRequestStore _videoGenerationRequestStore;
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly IMediaServicesManager _mediaResourceManager;

        public VideoGenerationManager(IVideoGenerationRequestStore videoGenerationRequestStore, IMediaServicesManager mediaResourceManager, IHeyGenVideoService heyGenVideoService)
        {
            _videoGenerationRequestStore = videoGenerationRequestStore ?? throw new ArgumentNullException(nameof(videoGenerationRequestStore));
            _heyGenVideoService = heyGenVideoService ?? throw new ArgumentNullException(nameof(heyGenVideoService));
            _mediaResourceManager = mediaResourceManager ?? throw new ArgumentNullException(nameof(mediaResourceManager));
        }

        public async Task<InvokeResult<VideoGenerationRequest>> GenerateVideoAsync(GenerateEntityVideoRequest request, CancellationToken cancellationToken = default)
        {
            var createResult = await CreateVideoGenerationRequestAsync(request, cancellationToken);

            if (!createResult.Successful)
            {
                return createResult;
            }

            var workItem = createResult.Result;


            var avatarResult = await GetOrCreateHeyGenAvatarIdAsync(request.AvatarMediaResourceId, request.VideoName, request.Organization, request.User, cancellationToken);

            if (!avatarResult.Successful)
            {
                workItem.Status = VideoGenerationRequestStatuses.Failed;
                workItem.ErrorMessage = avatarResult.Errors.FirstOrDefault()?.Message;
                workItem.LastUpdatedUtc = UtcTimestamp.Now;

                await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

                return avatarResult.ToInvokeResult<VideoGenerationRequest>();
            }

            workItem.HeyGenAvatarId = avatarResult.Result;

            if (!string.IsNullOrWhiteSpace(request.BackgroundMediaResourceId))
            {
                var assetResult = await GetOrCreateHeyGenAssetIdAsync(request.BackgroundMediaResourceId, request.Organization, request.User, cancellationToken);

                if (!assetResult.Successful)
                {
                    workItem.Status = VideoGenerationRequestStatuses.Failed;
                    workItem.ErrorMessage = assetResult.Errors.FirstOrDefault()?.Message;
                    workItem.LastUpdatedUtc = UtcTimestamp.Now;

                    await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

                    return assetResult.ToInvokeResult<VideoGenerationRequest>();
                }

                workItem.HeyGenBackgroundAssetId = assetResult.Result;
            }

            var avatarStatusResult = await _heyGenVideoService.GetAvatarStatusAsync(workItem.HeyGenAvatarId, cancellationToken);

            if (!avatarStatusResult.Successful)
            {
                return avatarStatusResult.ToInvokeResult<VideoGenerationRequest>();
            }

            if (!avatarStatusResult.Result.IsReady)
            {
                workItem.Status = VideoGenerationRequestStatuses.WaitingForAvatar;
                workItem.ErrorMessage = avatarStatusResult.Result.ErrorMessage;
                workItem.LastUpdatedUtc = UtcTimestamp.Now;

                await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

                return InvokeResult<VideoGenerationRequest>.FromError($"The HeyGen avatar is not ready. Current status: '{avatarStatusResult.Result.Status}'.");
            }

            workItem.Status = VideoGenerationRequestStatuses.Submitting;
            workItem.LastUpdatedUtc = UtcTimestamp.Now;

            await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

            var heyGenRequest = BuildHeyGenRequest(workItem);
            var heyGenResult = await _heyGenVideoService.SubmitVideoAsync(heyGenRequest, cancellationToken);

            if (!heyGenResult.Successful)
            {
                workItem.Status = VideoGenerationRequestStatuses.Failed;
                workItem.ErrorMessage = heyGenResult.Errors.FirstOrDefault()?.Message;
                workItem.LastUpdatedUtc = UtcTimestamp.Now;

                await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

                return heyGenResult.ToInvokeResult<VideoGenerationRequest>();
            }

            workItem.HeyGenVideoId = heyGenResult.Result.VideoId;
            workItem.Status = VideoGenerationRequestStatuses.Submitted;
            workItem.HeyGenSubmittedUtc = UtcTimestamp.Now;
            workItem.LastUpdatedUtc = workItem.HeyGenSubmittedUtc;
            workItem.ErrorMessage = null;

            await _videoGenerationRequestStore.UpdateAsync(workItem, cancellationToken);

            return InvokeResult<VideoGenerationRequest>.Create(workItem);
        }

        public async Task<InvokeResult<VideoGenerationRequest>> GetVideoGenerationRequestAsync(string organizationId, string requestId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
            {
                return InvokeResult<VideoGenerationRequest>.FromError("Organization ID is required.");
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                return InvokeResult<VideoGenerationRequest>.FromError("Video generation request ID is required.");
            }

            var request = await _videoGenerationRequestStore.GetAsync(organizationId, requestId, cancellationToken);

            if (request == null)
            {
                return InvokeResult<VideoGenerationRequest>.FromError($"Could not find video generation request '{requestId}'.");
            }

            return InvokeResult<VideoGenerationRequest>.Create(request);
        }

        private async Task<InvokeResult<VideoGenerationRequest>> CreateVideoGenerationRequestAsync(GenerateEntityVideoRequest request, CancellationToken cancellationToken)
        {
            var validationResult = ValidateRequest(request);

            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<VideoGenerationRequest>();
            }

            var entity = CreateRequestEntity(request);

            await _videoGenerationRequestStore.AddAsync(entity, cancellationToken);

            return InvokeResult<VideoGenerationRequest>.Create(entity);
        }

        private static HeyGenVideoRequest BuildHeyGenRequest(VideoGenerationRequest workItem)
        {
            return new HeyGenVideoRequest
            {
                AvatarId = workItem.HeyGenAvatarId,
                Script = workItem.Script,
                Title = workItem.VideoName,
                CallbackId = $"{workItem.PartitionKey}:{workItem.RowKey}",
                Resolution = "1080p",
                AspectRatio = "16:9",
                Background = string.IsNullOrWhiteSpace(workItem.HeyGenBackgroundAssetId) ? null : new HeyGenBackground { AssetId = workItem.HeyGenBackgroundAssetId }
            };
        }

        private static InvokeResult ValidateRequest(GenerateEntityVideoRequest request)
        {
            if (request == null)
            {
                return InvokeResult.FromError("Video generation request is required.");
            }

            if (request.Organization == null || string.IsNullOrWhiteSpace(request.Organization.Id))
            {
                return InvokeResult.FromError("Organization is required.");
            }

            if (string.IsNullOrWhiteSpace(request.AvatarMediaResourceId))
            {
                return InvokeResult.FromError("Avatar media resource ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return InvokeResult.FromError("Video script is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EntityType) || string.IsNullOrWhiteSpace(request.EntityId) || string.IsNullOrWhiteSpace(request.EntityProperty))
            {
                return InvokeResult.FromError("Target entity type, entity ID, and entity property are required.");
            }

            return InvokeResult.Success;
        }

        private static VideoGenerationRequest CreateRequestEntity(GenerateEntityVideoRequest request)
        {
            var now = DateTime.UtcNow.ToString("o");

            return new VideoGenerationRequest
            {
                PartitionKey = request.Organization.Id,
                RowKey = DateTime.UtcNow.ToInverseTicksRowKey(),

                OrganizationId = request.Organization.Id,
                OrganizationName = request.Organization.Text,

                UserId = request.User?.Id,
                UserName = request.User?.Text,

                AvatarMediaResourceId = request.AvatarMediaResourceId,
                BackgroundMediaResourceId = request.BackgroundMediaResourceId,

                Script = request.Script,
                VideoName = request.VideoName,

                EntityType = request.EntityType,
                EntityId = request.EntityId,
                EntityProperty = request.EntityProperty,

                Status = VideoGenerationRequestStatuses.Created,
                CreatedUtc = now,
                LastUpdatedUtc = now
            };
        }

        private async Task<InvokeResult<string>> GetOrCreateHeyGenAvatarIdAsync(string mediaResourceId, string avatarName, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            var mediaResource = await _mediaResourceManager.GetMediaResourceRecordAsync(mediaResourceId, org, user);

            if (mediaResource == null)
            {
                return InvokeResult<string>.FromError($"Could not find Media Resource '{mediaResourceId}'.");
            }

            if (!string.IsNullOrWhiteSpace(mediaResource.HeyGenAvatarId))
            {
                return InvokeResult<string>.Create(mediaResource.HeyGenAvatarId);
            }

            var assetResult = await GetOrCreateHeyGenAssetIdAsync(mediaResourceId, org, user, cancellationToken);

            if (!assetResult.Successful)
            {
                return assetResult.ToInvokeResult<string>();
            }

            var avatarRequest = new HeyGenPhotoAvatarRequest
            {
                Name = string.IsNullOrWhiteSpace(avatarName) ? mediaResource.Name : avatarName,
                File = new HeyGenPhotoAvatarFile
                {
                    AssetId = assetResult.Result
                }
            };

            var avatarResult = await _heyGenVideoService.CreatePhotoAvatarAsync(avatarRequest, cancellationToken);

            if (!avatarResult.Successful)
            {
                return avatarResult.ToInvokeResult<string>();
            }

            mediaResource.HeyGenAssetId = assetResult.Result;
            mediaResource.HeyGenAvatarId = avatarResult.Result.AvatarId;

            await _mediaResourceManager.UpdateMediaResourceRecordAsync(mediaResource, org, user);

            return InvokeResult<string>.Create(mediaResource.HeyGenAvatarId);
        }

        private async Task<InvokeResult<string>> GetOrCreateHeyGenAssetIdAsync(string mediaResourceId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            var mediaResource = await _mediaResourceManager.GetMediaResourceRecordAsync(mediaResourceId, org, user);

            if (mediaResource == null)
            {
                return InvokeResult<string>.FromError($"Could not find Media Resource '{mediaResourceId}'.");
            }

            if (!string.IsNullOrWhiteSpace(mediaResource.HeyGenAssetId))
            {
                return InvokeResult<string>.Create(mediaResource.HeyGenAssetId);
            }

            var content = await _mediaResourceManager.GetResourceMediaAsync(mediaResourceId, org, user);

            using var stream = new MemoryStream(content.ImageBytes, writable: false);

            var uploadResult = await _heyGenVideoService.UploadAssetAsync(stream, content.FileName, content.ContentType, mediaResource.Id, cancellationToken);

            if (!uploadResult.Successful)
            {
                return uploadResult.ToInvokeResult<string>();
            }

            mediaResource.HeyGenAssetId = uploadResult.Result.AssetId;

            await _mediaResourceManager.UpdateMediaResourceRecordAsync(mediaResource, org, user);

            return InvokeResult<string>.Create(mediaResource.HeyGenAssetId);
        }
    }
}