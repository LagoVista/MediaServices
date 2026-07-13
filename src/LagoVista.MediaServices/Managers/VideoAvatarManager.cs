using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoAvatarManager : ManagerBase, IVideoAvatarManager
    {
        private readonly IVideoAvatarRepo _repo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IHeyGenVideoService _heyGenVideoService;

        public VideoAvatarManager(IVideoAvatarRepo repo, IMediaServicesManager mediaServicesManager, IHeyGenVideoService heyGenVideoService, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new NullReferenceException(nameof(repo));
            _mediaServicesManager = mediaServicesManager ?? throw new NullReferenceException(nameof(mediaServicesManager));
            _heyGenVideoService = heyGenVideoService ?? throw new NullReferenceException(nameof(heyGenVideoService));
        }

        public async Task<InvokeResult> AddVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoAvatar(avatar);

            ValidationCheck(avatar, Actions.Create);
            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Create, user, org);

            await _repo.AddVideoAvatarAsync(avatar);

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoAvatar(avatar);

            ValidationCheck(avatar, Actions.Update);
            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Update, user, org);

            await _repo.UpdateVideoAvatarAsync(avatar);

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteVideoAvatarAsync(string id, EntityHeader org, EntityHeader user)
        {
            var avatar = await _repo.GetVideoAvatarAsync(id);

            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(avatar);

            await _repo.DeleteVideoAvatarAsync(id);

            return InvokeResult.Success;
        }

        public async Task<VideoAvatar> GetVideoAvatarAsync(string id, EntityHeader org, EntityHeader user)
        {
            var avatar = await _repo.GetVideoAvatarAsync(id);
            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Read, user, org);
            return avatar;
        }

        public async Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(VideoAvatar));
            return await _repo.GetVideoAvatarSummariesForOrgAsync(org.Id, listRequest);
        }

        public async Task<InvokeResult<VideoAvatar>> EnsureProviderAvatarAsync(string id, EntityHeader org, EntityHeader user)
        {
            var avatar = await GetVideoAvatarAsync(id, org, user);

            if (!String.IsNullOrWhiteSpace(avatar.ProviderAvatarId))
            {
                return InvokeResult<VideoAvatar>.Create(avatar);
            }

            if (avatar.AvatarImage == null || String.IsNullOrWhiteSpace(avatar.AvatarImage.Id))
            {
                return InvokeResult<VideoAvatar>.FromError("Video avatar source image is required.");
            }

            var assetResult = await GetOrCreateProviderAssetIdAsync(avatar.AvatarImage.Id, org, user);
            if (!assetResult.Successful)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
                avatar.ErrorMessage = assetResult.Errors[0].Message;
                avatar.LastStatusCheck = UtcTimestamp.Now;
                await _repo.UpdateVideoAvatarAsync(avatar);
                return assetResult.ToInvokeResult<VideoAvatar>();
            }

            avatar.ProviderAssetId = assetResult.Result;
            avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Preparing);
            avatar.LastStatusCheck = UtcTimestamp.Now;
            await _repo.UpdateVideoAvatarAsync(avatar);

            var avatarRequest = new HeyGenPhotoAvatarRequest
            {
                Name = avatar.Name,
                File = new HeyGenPhotoAvatarFile { AssetId = assetResult.Result }
            };

            var createResult = await _heyGenVideoService.CreatePhotoAvatarAsync(avatarRequest);
            if (!createResult.Successful)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
                avatar.ErrorMessage = createResult.Errors[0].Message;
                avatar.LastStatusCheck = UtcTimestamp.Now;
                await _repo.UpdateVideoAvatarAsync(avatar);
                return createResult.ToInvokeResult<VideoAvatar>();
            }

            avatar.ProviderAvatarId = createResult.Result.AvatarId;
            avatar.ProviderAvatarStatus = VideoAvatar.Status_WaitingForProvider;
            avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.WaitingForProvider);
            avatar.ErrorMessage = null;
            avatar.LastStatusCheck = UtcTimestamp.Now;

            await _repo.UpdateVideoAvatarAsync(avatar);

            return InvokeResult<VideoAvatar>.Create(avatar);
        }

        public async Task<InvokeResult<VideoAvatar>> RefreshProviderAvatarStatusAsync(string id, EntityHeader org, EntityHeader user)
        {
            var avatar = await GetVideoAvatarAsync(id, org, user);

            if (String.IsNullOrWhiteSpace(avatar.ProviderAvatarId))
            {
                return InvokeResult<VideoAvatar>.FromError("Provider avatar ID has not been created.");
            }

            var statusResult = await _heyGenVideoService.GetAvatarStatusAsync(avatar.ProviderAvatarId);
            if (!statusResult.Successful)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
                avatar.ErrorMessage = statusResult.Errors[0].Message;
                avatar.LastStatusCheck = UtcTimestamp.Now;
                await _repo.UpdateVideoAvatarAsync(avatar);
                return statusResult.ToInvokeResult<VideoAvatar>();
            }

            avatar.ProviderAvatarStatus = statusResult.Result.Status;
            avatar.LastStatusCheck = UtcTimestamp.Now;
            avatar.ErrorMessage = statusResult.Result.ErrorMessage;

            if (statusResult.Result.IsReady)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Ready);
                avatar.ErrorMessage = null;
            }
            else if (!String.IsNullOrWhiteSpace(statusResult.Result.ErrorCode))
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
            }
            else
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.WaitingForProvider);
            }

            await _repo.UpdateVideoAvatarAsync(avatar);

            return InvokeResult<VideoAvatar>.Create(avatar);
        }

        private async Task<InvokeResult<string>> GetOrCreateProviderAssetIdAsync(string mediaResourceId, EntityHeader org, EntityHeader user)
        {
            var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(mediaResourceId, org, user);
            if (mediaResource == null)
            {
                return InvokeResult<string>.FromError($"Could not find media resource '{mediaResourceId}'.");
            }

            if (!String.IsNullOrWhiteSpace(mediaResource.HeyGenAssetId))
            {
                return InvokeResult<string>.Create(mediaResource.HeyGenAssetId);
            }

            var content = await _mediaServicesManager.GetResourceMediaAsync(mediaResourceId, org, user);
            if (content == null || content.ImageBytes == null || content.ImageBytes.Length == 0)
            {
                return InvokeResult<string>.FromError($"Media resource '{mediaResourceId}' does not contain image content.");
            }

            using var stream = new MemoryStream(content.ImageBytes, writable: false);

            var uploadResult = await _heyGenVideoService.UploadAssetAsync(stream, content.FileName, content.ContentType, mediaResource.Id);
            if (!uploadResult.Successful)
            {
                return uploadResult.ToInvokeResult<string>();
            }

            mediaResource.HeyGenAssetId = uploadResult.Result.AssetId;
            await _mediaServicesManager.UpdateMediaResourceRecordAsync(mediaResource, org, user);

            return InvokeResult<string>.Create(mediaResource.HeyGenAssetId);
        }

        private static void NormalizeVideoAvatar(VideoAvatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            if (avatar.Provider == null)
            {
                avatar.Provider = EntityHeader<VideoAvatarProvider>.Create(VideoAvatarProvider.HeyGen);
            }

            if (avatar.Role == null)
            {
                avatar.Role = EntityHeader<VideoAvatarRole>.Create(VideoAvatarRole.Primary);
            }

            if (avatar.Status == null)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Draft);
            }

            avatar.Voices = avatar.Voices ?? new List<VideoAvatarVoice>();

            var defaultVoiceSeen = false;

            foreach (var voice in avatar.Voices)
            {
                if (string.IsNullOrWhiteSpace(voice.Id))
                {
                    voice.Id = Guid.NewGuid().ToId();
                }

                if (string.IsNullOrWhiteSpace(voice.Label))
                {
                    voice.Label = CreateVoiceLabel(voice);
                }

                if (voice.IsDefault)
                {
                    if (defaultVoiceSeen)
                    {
                        voice.IsDefault = false;
                    }
                    else
                    {
                        defaultVoiceSeen = true;
                    }
                }
            }

            if (!defaultVoiceSeen && avatar.Voices.Count > 0)
            {
                avatar.Voices[0].IsDefault = true;
            }
        }
        private static string CreateVoiceLabel(VideoAvatarVoice voice)
        {
            if (!string.IsNullOrWhiteSpace(voice.Label))
            {
                return voice.Label;
            }

            if (!string.IsNullOrWhiteSpace(voice.LanguageCode) && !string.IsNullOrWhiteSpace(voice.VoiceName))
            {
                return $"{voice.LanguageCode} - {voice.VoiceName}";
            }

            return voice.VoiceName ?? voice.VoiceId ?? "Voice";
        }
    }
}