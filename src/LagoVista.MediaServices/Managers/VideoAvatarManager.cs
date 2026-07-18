using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoAvatarManager : ManagerBase, IVideoAvatarManager
    {
        private readonly IVideoAvatarRepo _repo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger _adminLogger;
        private readonly IBillingEventRecorder _billingEventRecorder;
        private static readonly TimeSpan ProviderStatusPollInterval = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ProviderStatusPollTimeout = TimeSpan.FromMinutes(5);

        public VideoAvatarManager(IVideoAvatarRepo repo, IMediaServicesManager mediaServicesManager, IBillingEventRecorder billingEventRecorder, 
                                  IHeyGenVideoService heyGenVideoService, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new NullReferenceException(nameof(repo));
            _mediaServicesManager = mediaServicesManager ?? throw new NullReferenceException(nameof(mediaServicesManager));
            _heyGenVideoService = heyGenVideoService ?? throw new NullReferenceException(nameof(heyGenVideoService));
            _adminLogger = coreAppServices?.Logger ?? throw new ArgumentNullException(nameof(coreAppServices.Logger)); 
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
            _billingEventRecorder = billingEventRecorder ?? throw new NullReferenceException(nameof(billingEventRecorder));

        }

        public async Task<InvokeResult<VideoAvatar>> AddVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user)
        {
            if (avatar == null)
            {
                return InvokeResult<VideoAvatar>.FromError("Video avatar is required.");
            }

            NormalizeVideoAvatar(avatar);

            var voiceValidation = ValidateVoices(avatar);
            if (!voiceValidation.Successful)
            {
                return voiceValidation.ToInvokeResult<VideoAvatar>();
            }

            ValidationCheck(avatar, Actions.Create);
            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Create, user, org);

            await _repo.AddVideoAvatarAsync(avatar);

            return InvokeResult<VideoAvatar>.Create(avatar);
        }

        public async Task<InvokeResult<VideoAvatar>> UpdateVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user)
        {
            if (avatar == null)
            {
                return InvokeResult<VideoAvatar>.FromError("Video avatar is required.");
            }

            if (String.IsNullOrWhiteSpace(avatar.Id))
            {
                return InvokeResult<VideoAvatar>.FromError("Video avatar ID is required.");
            }

            NormalizeVideoAvatar(avatar);

            var voiceValidation = ValidateVoices(avatar);
            if (!voiceValidation.Successful)
            {
                return voiceValidation.ToInvokeResult<VideoAvatar>();
            }

            ValidationCheck(avatar, Actions.Update);
            await AuthorizeAsync(avatar, AuthorizeResult.AuthorizeActions.Update, user, org);

            var updatedAvatar = await _repo.UpdateVideoAvatarAsync(avatar);

            return InvokeResult<VideoAvatar>.Create(updatedAvatar);
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
            if (String.IsNullOrWhiteSpace(id))
            {
                return InvokeResult<VideoAvatar>.FromError("Video avatar ID is required.");
            }

            var lockToken = Guid.NewGuid().ToString("N");
            var lockAcquired = await _repo.AttemptAcquireProviderCreationLockAsync(id, lockToken);

            if (!lockAcquired)
            {
                return InvokeResult<VideoAvatar>.FromError("This video avatar is already being prepared.");
            }

            try
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

                    await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

                    return assetResult.ToInvokeResult<VideoAvatar>();
                }

                avatar.ProviderAssetId = assetResult.Result;
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Preparing);
                avatar.ErrorMessage = null;
                avatar.LastStatusCheck = UtcTimestamp.Now;

                avatar = await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

                var avatarRequest = new HeyGenPhotoAvatarRequest
                {
                    Name = avatar.Name,
                    File = new HeyGenPhotoAvatarFile
                    {
                        AssetId = assetResult.Result
                    }
                };

                var createResult = await _heyGenVideoService.CreatePhotoAvatarAsync(avatarRequest);
                if (!createResult.Successful)
                {
                    avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
                    avatar.ErrorMessage = createResult.Errors[0].Message;
                    avatar.LastStatusCheck = UtcTimestamp.Now;

                    await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

                    return createResult.ToInvokeResult<VideoAvatar>();
                }

                avatar.ProviderAvatarId = createResult.Result.AvatarId;
                avatar.ProviderAvatarStatus = VideoAvatar.Status_WaitingForProvider;
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.WaitingForProvider);
                avatar.ErrorMessage = null;
                avatar.LastStatusCheck = UtcTimestamp.Now;

                var currentAvatar = await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

                QueueProviderAvatarStatusPolling(currentAvatar.Id, org, user);

                await PublishAvatarUpdatedAsync(currentAvatar);

                return InvokeResult<VideoAvatar>.Create(currentAvatar);
            }
            finally
            {
                await _repo.ReleaseProviderCreationLockAsync(id, lockToken);
            }
        }

        private static VideoAvatarProviderState CreateProviderState(VideoAvatar avatar)
        {
            return new VideoAvatarProviderState
            {
                ProviderAssetId = avatar.ProviderAssetId,
                ProviderAvatarId = avatar.ProviderAvatarId,
                ProviderAvatarStatus = avatar.ProviderAvatarStatus,
                Status = avatar.Status,
                ErrorMessage = avatar.ErrorMessage,
                LastStatusCheck = avatar.LastStatusCheck
            };
        }

        private void QueueProviderAvatarStatusPolling(string avatarId, EntityHeader org, EntityHeader user)
        {
            var queue = BackgroundServiceTaskQueueProvider.Instance;
            var pollingOrg = EntityHeader.Create(org.Id, org.Text);
            var pollingUser = EntityHeader.Create(user.Id, user.Text);


            if (queue == null)
            {
                _adminLogger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Background task queue is unavailable. Provider status polling was not queued for avatar '{avatarId}'.");
                return;
            }

            var queued = queue.TryQueueBackgroundWorkItem(cancellationToken => PollProviderAvatarStatusAsync(avatarId, pollingOrg, pollingUser, cancellationToken));

            if (!queued)
            {
                _adminLogger.AddCustomEvent(
                    LogLevel.Warning,
                    this.Tag(),
                    $"Provider status polling could not be queued for avatar '{avatarId}'.");
            }
        }

        private static bool IsAvatarStatus(VideoAvatar avatar, VideoAvatarStatus status)
        {
            return String.Equals(avatar?.Status?.Key, status.ToString(), StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(avatar?.Status?.Id, status.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task PollProviderAvatarStatusAsync(string avatarId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            var startedUtc = DateTime.UtcNow;

            var idx = 0;
            _adminLogger.Trace($"{this.Tag()} - Starting provider status polling for avatar '{avatarId}'.");

            try
            {
                while (DateTime.UtcNow - startedUtc < ProviderStatusPollTimeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var avatar = await GetVideoAvatarAsync(avatarId, org, user);

                    if (avatar == null || String.IsNullOrWhiteSpace(avatar.ProviderAvatarId))
                    {
                        return;
                    }

                    if (IsAvatarStatus(avatar, VideoAvatarStatus.Ready) || IsAvatarStatus(avatar, VideoAvatarStatus.Failed))
                    {
                        return;
                    }

                    var statusResult = await _heyGenVideoService.GetAvatarStatusAsync(avatar.ProviderAvatarId, cancellationToken);

                    if (statusResult.Successful)
                    {
                        var updatedAvatar = await ApplyProviderStatusAsync(avatar, statusResult.Result);

                        if (statusResult.Result.IsReady || !String.IsNullOrWhiteSpace(statusResult.Result.ErrorCode))
                        {
                            _adminLogger.Trace($"{this.Tag()} - Avatar IS ready Congratulations -'{avatarId}' - {idx++}.");
                            await _billingEventRecorder.RecordUsageAsync(BillingEventType.VideoAvatarCreated, 1, $"HeyGen Video Avatar '{avatarId}' is ready", avatar.OwnerOrganization, avatar.LastUpdatedBy);
                            await PublishAvatarUpdatedAsync(updatedAvatar);
                            return;
                        }
                    }
                  
                    await Task.Delay(ProviderStatusPollInterval, cancellationToken);
                    _adminLogger.Trace($"{this.Tag()} - Avatar not ready '{avatarId}' - {idx++}.");

                }

                await RecordProviderPollingTimeoutAsync(avatarId, org, user);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _adminLogger.AddException(this.Tag(), ex, new KeyValuePair<string, string>("AvatarId", avatarId));
            }
        }

        private async Task RecordProviderPollingTimeoutAsync(string avatarId, EntityHeader org, EntityHeader user)
        {
            var avatar = await GetVideoAvatarAsync(avatarId, org, user);

            if (avatar == null || IsAvatarStatus(avatar, VideoAvatarStatus.Ready) || IsAvatarStatus(avatar, VideoAvatarStatus.Failed))
            {
                return;
            }

            avatar.LastStatusCheck = UtcTimestamp.Now;

            var currentAvatar = await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

            await PublishAvatarUpdatedAsync(currentAvatar);
        }

        private async Task<VideoAvatar> ApplyProviderStatusAsync(VideoAvatar avatar, HeyGenAvatarStatusResult providerStatus)
        {
            avatar.ProviderAvatarStatus = providerStatus.Status;
            avatar.LastStatusCheck = UtcTimestamp.Now;
            avatar.ErrorMessage = providerStatus.ErrorMessage;

            if (providerStatus.IsReady)
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Ready);
                avatar.ErrorMessage = null;
            }
            else if (!String.IsNullOrWhiteSpace(providerStatus.ErrorCode))
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Failed);
            }
            else
            {
                avatar.Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.WaitingForProvider);
            }

            return await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));
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

                var failedAvatar = await _repo.UpdateVideoAvatarProviderStateAsync(avatar.Id, CreateProviderState(avatar));

                return statusResult.ToInvokeResult<VideoAvatar>();
            }

            var currentAvatar = await ApplyProviderStatusAsync(avatar, statusResult.Result);

            await PublishAvatarUpdatedAsync(currentAvatar);

            return InvokeResult<VideoAvatar>.Create(currentAvatar);
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

            NormalizeVoices(avatar);
        }

        private async Task PublishAvatarUpdatedAsync(VideoAvatar avatar)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, avatar.Id, "video-avatar-updated", avatar);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, avatar.OwnerOrganization.Id, "video-avatar-updated", avatar);
        }

        private static void NormalizeVoices(VideoAvatar avatar)
        {
            var sourceVoices = avatar.Voices ?? new List<VideoAvatarVoice>();
            var normalizedVoices = new List<VideoAvatarVoice>();
            var voicesByIdentity = new Dictionary<string, VideoAvatarVoice>(StringComparer.OrdinalIgnoreCase);

            foreach (var voice in sourceVoices)
            {
                if (voice == null || String.IsNullOrWhiteSpace(voice.VoiceId))
                {
                    continue;
                }

                voice.VoiceId = voice.VoiceId.Trim();
                voice.Locale = NormalizeOptionalValue(voice.Locale);
                voice.LanguageCode = NormalizeOptionalValue(voice.LanguageCode);
                voice.LanguageName = NormalizeOptionalValue(voice.LanguageName);
                voice.VoiceName = NormalizeOptionalValue(voice.VoiceName);
                voice.Label = NormalizeOptionalValue(voice.Label);
                voice.Gender = NormalizeOptionalValue(voice.Gender);
                voice.Accent = NormalizeOptionalValue(voice.Accent);
                voice.VoiceType = NormalizeOptionalValue(voice.VoiceType);

                var identity = CreateVoiceIdentity(voice);

                if (voicesByIdentity.TryGetValue(identity, out var existing))
                {
                    MergeMissingVoiceValues(existing, voice);
                    existing.IsDefault = existing.IsDefault || voice.IsDefault;
                    continue;
                }

                if (String.IsNullOrWhiteSpace(voice.Id))
                {
                    voice.Id = Guid.NewGuid().ToId();
                }

                if (String.IsNullOrWhiteSpace(voice.Label))
                {
                    voice.Label = CreateVoiceLabel(voice);
                }

                voicesByIdentity[identity] = voice;
                normalizedVoices.Add(voice);
            }

            NormalizeDefaultVoice(normalizedVoices);

            avatar.Voices = normalizedVoices;
        }

        private static string CreateVoiceIdentity(VideoAvatarVoice voice)
        {
            var voiceId = voice.VoiceId.Trim().ToLowerInvariant();
            var locale = (voice.Locale ?? String.Empty).Trim().ToLowerInvariant();

            return $"{voiceId}|{locale}";
        }

        private static void NormalizeDefaultVoice(IList<VideoAvatarVoice> voices)
        {
            if (voices == null || voices.Count == 0)
            {
                return;
            }

            var defaultVoiceSeen = false;

            foreach (var voice in voices)
            {
                if (!voice.IsDefault)
                {
                    continue;
                }

                if (defaultVoiceSeen)
                {
                    voice.IsDefault = false;
                }
                else
                {
                    defaultVoiceSeen = true;
                }
            }

            if (!defaultVoiceSeen)
            {
                voices[0].IsDefault = true;
            }
        }

        private static void MergeMissingVoiceValues(VideoAvatarVoice target, VideoAvatarVoice source)
        {
            target.Id = FirstValue(target.Id, source.Id);
            target.Label = FirstValue(target.Label, source.Label);
            target.VoiceName = FirstValue(target.VoiceName, source.VoiceName);
            target.LanguageCode = FirstValue(target.LanguageCode, source.LanguageCode);
            target.LanguageName = FirstValue(target.LanguageName, source.LanguageName);
            target.Locale = FirstValue(target.Locale, source.Locale);
            target.Gender = FirstValue(target.Gender, source.Gender);
            target.Accent = FirstValue(target.Accent, source.Accent);
            target.VoiceType = FirstValue(target.VoiceType, source.VoiceType);
            target.PreviewAudioUrl = FirstValue(target.PreviewAudioUrl, source.PreviewAudioUrl);
            target.IsPreviewable = target.IsPreviewable || source.IsPreviewable;

            if (String.IsNullOrWhiteSpace(target.Label))
            {
                target.Label = CreateVoiceLabel(target);
            }

            if (String.IsNullOrWhiteSpace(target.Id))
            {
                target.Id = Guid.NewGuid().ToId();
            }
        }

        private static string FirstValue(string current, string candidate)
        {
            return !String.IsNullOrWhiteSpace(current) ? current : NormalizeOptionalValue(candidate);
        }

        private static string NormalizeOptionalValue(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
 
        private static InvokeResult ValidateVoices(VideoAvatar avatar)
        {
            var voices = avatar?.Voices ?? new List<VideoAvatarVoice>();

            foreach (var voice in voices)
            {
                if (String.IsNullOrWhiteSpace(voice.VoiceId))
                {
                    return InvokeResult.FromError("Every avatar voice must have a HeyGen voice ID.");
                }

                if (String.IsNullOrWhiteSpace(voice.Id))
                {
                    return InvokeResult.FromError($"Voice '{voice.VoiceName ?? voice.VoiceId}' does not have a binding ID.");
                }
            }

            var duplicateIdentity = voices
                .GroupBy(CreateVoiceIdentity, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateIdentity != null)
            {
                return InvokeResult.FromError($"The avatar contains duplicate voice binding '{duplicateIdentity.Key}'.");
            }

            var defaultCount = voices.Count(voice => voice.IsDefault);

            if (voices.Count > 0 && defaultCount != 1)
            {
                return InvokeResult.FromError("An avatar with voices must have exactly one default voice.");
            }

            if (voices.Count == 0 && defaultCount != 0)
            {
                return InvokeResult.FromError("An avatar without voices cannot have a default voice.");
            }

            return InvokeResult.Success;
        }

        public Task<VideoAvatar> UpdateVideoAvatarProviderStateAsync(string id, VideoAvatarProviderState state)
        {
            return _repo.UpdateVideoAvatarProviderStateAsync(id, state);
        }
    }
}