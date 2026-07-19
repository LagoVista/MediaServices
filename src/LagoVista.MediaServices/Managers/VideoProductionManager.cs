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
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using LagoVista.UserAdmin.Models.Orgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoProductionManager : ManagerBase, IVideoProductionManager
    {
        private readonly IVideoAvatarManager _videoAvatarManager;
        private readonly IVideoProductionRepo _repo;
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly EntityHeader _webhookSecretOwner;
        private readonly string _heyGenWebhookCallbackUrl;
        private readonly INotificationPublisher _notificationPublisher;
        private const string HeyGenWebhookPath = "/api/media/webhooks/heygen";
        private readonly ICacheProvider _cacheProvider;
        private readonly IVimeoVideoService _vimeoVideoService;
        private readonly IOrganizationLoaderRepo _organizationLoaderRepo;
        private readonly ISecureStorage _secureStorage;
        private readonly IMediaServicesRepo _mediaRepo;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IBillingEventRecorder _billingEventRecorder;

        private static readonly TimeSpan VimeoStatusPollingInterval = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan VimeoStatusPollingTimeout = TimeSpan.FromMinutes(30);

        private static readonly TimeSpan WebhookProcessingLockDuration = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan WebhookReceiptDuration = TimeSpan.FromDays(14);
        private readonly ILogger _logger;


        public VideoProductionManager(IVideoProductionRepo repo, IVideoAvatarManager videoAvatarManager, IHeyGenVideoService heyGenVideoService, IVimeoVideoService vimeoVideoService, IMediaServicesRepo mediaRepo, IMediaServicesManager mediaServicesManager, IOrganizationLoaderRepo organizationLoaderRepo, ISecureStorage secureStorage, ICacheProvider cacheProvider, ICoreAppServices coreAppServices, IBillingEventRecorder billingEventRecorder) : base(coreAppServices)
        {
            _repo = repo ?? throw new NullReferenceException(nameof(repo));
            _videoAvatarManager = videoAvatarManager ?? throw new NullReferenceException(nameof(videoAvatarManager));
            _heyGenVideoService = heyGenVideoService ?? throw new NullReferenceException(nameof(heyGenVideoService));
            _billingEventRecorder = billingEventRecorder ?? throw new NullReferenceException(nameof(billingEventRecorder));
            _vimeoVideoService = vimeoVideoService ?? throw new NullReferenceException(nameof(vimeoVideoService));
            _organizationLoaderRepo = organizationLoaderRepo ?? throw new NullReferenceException(nameof(organizationLoaderRepo));
            _secureStorage = secureStorage ?? throw new NullReferenceException(nameof(secureStorage));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
            _webhookSecretOwner = coreAppServices.AppConfig.SystemOwnerOrg;
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _mediaRepo = mediaRepo ?? throw new ArgumentNullException(nameof(mediaRepo));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));

            _logger = coreAppServices.Logger ?? throw new ArgumentNullException(nameof(coreAppServices.Logger));
            var siteUrl = coreAppServices.AppConfig.Environment == Environments.LocalDevelopment || coreAppServices.AppConfig.Environment == Environments.Development ? "https://dev.nuviot.com" : coreAppServices.AppConfig.WebAddress;

            _heyGenWebhookCallbackUrl = $"{siteUrl.TrimEnd('/')}{HeyGenWebhookPath}";
        }

        public async Task<InvokeResult<VideoProduction>> AddVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoProduction(production);

            ValidationCheck(production, Actions.Create);
            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Create, user, org);

            await _repo.AddVideoProductionAsync(production);

            return InvokeResult< VideoProduction>.Create(production);
        }

        public async Task<InvokeResult<VideoProduction>> UpdateVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoProduction(production);


            ValidationCheck(production, Actions.Update);
            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Update, user, org);

            var existing = await _repo.GetVideoProductionAsync(production.Id);
            if(production.FinalVideoMediaResource != null && existing.Name != production.Name)
            {
                var mediaResource = await _mediaRepo.GetMediaResourceRecordAsync(production.FinalVideoMediaResource.Id);
                mediaResource.Name = production.Name;
                await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);
            }

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        public async Task<InvokeResult> DeleteVideoProductionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await _repo.GetVideoProductionAsync(id);

            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(production);

            await _repo.DeleteVideoProductionAsync(id);

            return InvokeResult.Success;
        }

        public async Task<VideoProduction> GetVideoProductionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await _repo.GetVideoProductionAsync(id);
            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Read, user, org);
            return production;
        }

        public async Task<ListResponse<VideoProductionSummary>> GetVideoProductionsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(VideoProduction));
            return await _repo.GetVideoProductionSummariesForOrgAsync(org.Id, listRequest);
        }

        public async Task<InvokeResult<VideoProduction>> PublishVideoProductionToVimeoAsync(string id, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (!String.IsNullOrWhiteSpace(production.VimeoVideoId) || !String.IsNullOrWhiteSpace(production.VimeoVideoUri))
            {
                return await EnsureVimeoFolderAssignmentAsync(production, org, user, cancellationToken);
            }

            if (String.IsNullOrWhiteSpace(production.ProviderVideoId))
            {
                return InvokeResult<VideoProduction>.FromError("The video production does not have a HeyGen video ID.");
            }

            var settingsResult = await ResolveVimeoTenantSettingsAsync(org, user);
            if (!settingsResult.Successful)
            {
                return settingsResult.ToInvokeResult<VideoProduction>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var heyGenResult = await _heyGenVideoService.GetVideoAsync(production.ProviderVideoId, cancellationToken);
            if (!heyGenResult.Successful)
            {
                return heyGenResult.ToInvokeResult<VideoProduction>();
            }

            if (String.Equals(heyGenResult.Result.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = ResolveHeyGenVideoError(heyGenResult.Result);
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
            }

            if (!String.Equals(heyGenResult.Result.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.FromError($"The HeyGen video is not ready for Vimeo publishing. Current status: '{heyGenResult.Result.Status}'.");
            }

            if (String.IsNullOrWhiteSpace(heyGenResult.Result.VideoUrl))
            {
                return InvokeResult<VideoProduction>.FromError("HeyGen reported that the video is complete but did not return a video URL.");
            }

            var settings = settingsResult.Result;

            var uploadRequest = new VimeoPullUploadRequest
            {
                Name = String.IsNullOrWhiteSpace(production.VideoName) ? production.Name : production.VideoName,
                Description = production.Description,
                Upload = new VimeoPullUploadSource
                {
                    Link = heyGenResult.Result.VideoUrl
                },
                Privacy = new VimeoPrivacySettings
                {
                    View = settings.DefaultPrivacy
                }
            };

            var uploadResult = await _vimeoVideoService.CreatePullUploadAsync(settings.AccessToken, uploadRequest, cancellationToken);
            if (!uploadResult.Successful)
            {
                production.ErrorMessage = uploadResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return uploadResult.ToInvokeResult<VideoProduction>();
            }

            production.VimeoVideoUri = uploadResult.Result.Uri;
            production.VimeoVideoId = ResolveVimeoVideoId(uploadResult.Result.Uri);
            production.VimeoVideoUrl = uploadResult.Result.Link;
            production.SetStatus(VideoProductionStatus.ImportingToVimeo);
            production.LastStatusCheckUtc = UtcTimestamp.Now;
            production.ErrorMessage = null;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            if (!String.IsNullOrWhiteSpace(settings.DefaultFolderUri))
            {
                var folderResult = await _vimeoVideoService.AddVideoToFolderAsync(production.VimeoVideoUri, settings.DefaultFolderUri, settings.AccessToken, cancellationToken);
                if (!folderResult.Successful)
                {
                    production.ErrorMessage = folderResult.Errors[0].Message;
                    production.LastStatusCheckUtc = UtcTimestamp.Now;

                    await _repo.UpdateVideoProductionAsync(production);
                    await PublishVideoProductionUpdatedAsync(production);

                    return folderResult.ToInvokeResult<VideoProduction>();
                }

                production.VimeoFolderUri = settings.DefaultFolderUri;
                production.VimeoFolderAssignedUtc = UtcTimestamp.Now;
                production.ErrorMessage = null;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);
            }

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            QueueVimeoStatusPolling(production.Id, org, user);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private void QueueVimeoStatusPolling(string productionId, EntityHeader org, EntityHeader user)
        {
            if (String.IsNullOrWhiteSpace(productionId))
            {
                return;
            }

            _logger.Trace($"{this.Tag()} - Starting provider status polling for avatar '{productionId}'.");

            BackgroundServiceTaskQueueProvider.Instance.QueueBackgroundWorkItemAsync(async cancellationToken =>
            {
                var startedUtc = DateTime.UtcNow;

                while (DateTime.UtcNow - startedUtc < VimeoStatusPollingTimeout)
                {
                    await Task.Delay(VimeoStatusPollingInterval, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    var production = await _repo.GetVideoProductionAsync(productionId);
                    if (production == null)
                    {
                        return;
                    }

                    if (IsTerminalVideoProductionStatus(production.Status))
                    {
                        return;
                    }

                    if (String.IsNullOrWhiteSpace(production.VimeoVideoUri))
                    {
                        return;
                    }

                    var refreshResult = await RefreshVimeoStatusCoreAsync(production, org, user, cancellationToken);
                    if (!refreshResult.Successful || refreshResult.Result == null)
                    {
                        continue;
                    }

                    if (IsTerminalVideoProductionStatus(refreshResult.Result.Status))
                    {
                        _logger.Trace($"{this.Tag()} - Finished! {refreshResult.Result.Status} '{productionId}'.");
                        return;
                    }
                }

                var timedOutProduction = await _repo.GetVideoProductionAsync(productionId);
                if (timedOutProduction == null || IsTerminalVideoProductionStatus(timedOutProduction.Status))
                {
                    return;
                }

                timedOutProduction.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(timedOutProduction);
                await PublishVideoProductionUpdatedAsync(timedOutProduction);
            });
        }

        private static bool IsTerminalVideoProductionStatus(EntityHeader<VideoProductionStatus> status)
        {
            if (status == null)
            {
                return false;
            }

            return status.Value == VideoProductionStatus.Completed ||
                   status.Value == VideoProductionStatus.Failed ||
                   status.Value == VideoProductionStatus.Cancelled;
        }

        private async Task<InvokeResult<VideoProduction>> RefreshVimeoStatusCoreAsync(VideoProduction production, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            if (production == null)
            {
                return InvokeResult<VideoProduction>.FromError("Video production is required.");
            }

            if (String.IsNullOrWhiteSpace(production.VimeoVideoUri))
            {
                return InvokeResult<VideoProduction>.FromError("The video production does not have a Vimeo video URI.");
            }

            var settingsResult = await ResolveVimeoTenantSettingsAsync(org, user);
            if (!settingsResult.Successful)
            {
                return settingsResult.ToInvokeResult<VideoProduction>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var statusResult = await _vimeoVideoService.GetVideoAsync(settingsResult.Result.AccessToken, production.VimeoVideoUri, cancellationToken);
            if (!statusResult.Successful)
            {
                return statusResult.ToInvokeResult<VideoProduction>();
            }

            ApplyVimeoStatus(production, statusResult.Result);

            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private async Task<InvokeResult<VideoProduction>> EnsureVimeoFolderAssignmentAsync(VideoProduction production, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            var settingsResult = await ResolveVimeoTenantSettingsAsync(org, user);
            if (!settingsResult.Successful)
            {
                return settingsResult.ToInvokeResult<VideoProduction>();
            }

            var settings = settingsResult.Result;

            if (String.IsNullOrWhiteSpace(settings.DefaultFolderUri))
            {
                return InvokeResult<VideoProduction>.Create(production);
            }

            if (String.Equals(production.VimeoFolderUri, settings.DefaultFolderUri, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.Create(production);
            }

            var folderResult = await _vimeoVideoService.AddVideoToFolderAsync(production.VimeoVideoUri, settings.DefaultFolderUri, settings.AccessToken, cancellationToken);
            if (!folderResult.Successful)
            {
                production.ErrorMessage = folderResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return folderResult.ToInvokeResult<VideoProduction>();
            }

            production.VimeoFolderUri = settings.DefaultFolderUri;
            production.ErrorMessage = null;
            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private async Task<InvokeResult<VimeoTenantSettings>> ResolveVimeoTenantSettingsAsync(EntityHeader org, EntityHeader user)
        {
            if (org == null || String.IsNullOrWhiteSpace(org.Id))
            {
                return InvokeResult<VimeoTenantSettings>.FromError("An organization is required to publish a video to Vimeo.");
            }

            var organization = await _organizationLoaderRepo.GetOrganizationAsync(org.Id);

            if (organization == null)
            {
                return InvokeResult<VimeoTenantSettings>.FromError($"Could not load organization '{org.Id}'.");
            }

            if (!organization.VimeoEnabled)
            {
                return InvokeResult<VimeoTenantSettings>.FromError("Vimeo publishing is not enabled for this organization.");
            }

            if (String.IsNullOrWhiteSpace(organization.VimeoAccessTokenSecretId))
            {
                return InvokeResult<VimeoTenantSettings>.FromError("The organization does not have a Vimeo access token configured.");
            }

            var tokenResult = await _secureStorage.GetSecretAsync(org, organization.VimeoAccessTokenSecretId, user);
            if (!tokenResult.Successful)
            {
                return tokenResult.ToInvokeResult<VimeoTenantSettings>();
            }

            if (String.IsNullOrWhiteSpace(tokenResult.Result))
            {
                return InvokeResult<VimeoTenantSettings>.FromError("The configured Vimeo access token is empty.");
            }

            var privacy = organization.VimeoDefaultPrivacy?.Id;

            if (String.IsNullOrWhiteSpace(privacy))
            {
                privacy = Organization.Organization_VimeoUnlisted;
            }

            return InvokeResult<VimeoTenantSettings>.Create(new VimeoTenantSettings
            {
                AccessToken = tokenResult.Result,
                DefaultFolderUri = organization.VimeoDefaultFolderUri?.Trim(),
                DefaultPrivacy = privacy
            });
        }

        private static string ResolveVimeoVideoId(string videoUri)
        {
            if (String.IsNullOrWhiteSpace(videoUri))
            {
                return null;
            }

            var parts = videoUri.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0 ? null : parts[parts.Length - 1];
        }

        private static string ResolveHeyGenVideoError(HeyGenVideoDetails video)
        {
            if (!String.IsNullOrWhiteSpace(video.FailureMessage))
            {
                return video.FailureMessage;
            }

            if (!String.IsNullOrWhiteSpace(video.FailureCode))
            {
                return $"HeyGen video generation failed with code '{video.FailureCode}'.";
            }

            return "HeyGen video generation failed.";
        }

        public async Task<InvokeResult<VideoProduction>> EstimateVideoProductionCostAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            production.EstimatedDurationSeconds = EstimateDurationSeconds(production.Script);
            production.EstimatedPreviewAudioCost = EstimateCost(production.EstimatedDurationSeconds.Value, 0.000667m);
            var quality = production.Quality?.Value ?? VideoProductionQuality.Standard;
            var videoGenerationCostPerSecond = quality == VideoProductionQuality.Standard ? 1.0m / 60.0m : 3.0m / 60.0m;

            production.EstimatedVideoGenerationCost = EstimateCost(production.EstimatedDurationSeconds.Value, videoGenerationCostPerSecond);
            production.EstimatedTotalCost = (production.EstimatedPreviewAudioCost ?? 0) + (production.EstimatedAvatarCreationCost ?? 0) + (production.EstimatedVideoGenerationCost ?? 0);
            production.CostCurrency = String.IsNullOrWhiteSpace(production.CostCurrency) ? "USD" : production.CostCurrency;
            production.CostModelVersion = "heygen-standard-premium-2026-07";

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        public async Task<InvokeResult<VideoProduction>> GeneratePreviewAudioAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (String.IsNullOrWhiteSpace(production.Script))
            {
                return InvokeResult<VideoProduction>.FromError("Script is required to generate preview audio.");
            }

            if (String.IsNullOrWhiteSpace(production.VoiceId))
            {
                return InvokeResult<VideoProduction>.FromError("Voice ID is required to generate preview audio.");
            }

            production.SetStatus(VideoProductionStatus.GeneratingPreviewAudio);
            production.LastStatusCheckUtc = UtcTimestamp.Now;
            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.FromError("Preview audio generation has not been implemented yet.");
        }

        public async Task<InvokeResult<VideoProduction>> SubmitVideoProductionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (production.VideoAvatar == null || String.IsNullOrWhiteSpace(production.VideoAvatar.Id))
            {
                return InvokeResult<VideoProduction>.FromError("Video avatar is required.");
            }

            var avatarResult = await _videoAvatarManager.EnsureProviderAvatarAsync(production.VideoAvatar.Id, org, user);
            if (!avatarResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = avatarResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return avatarResult.ToInvokeResult<VideoProduction>();
            }

            var avatarStatusResult = await _videoAvatarManager.RefreshProviderAvatarStatusAsync(production.VideoAvatar.Id, org, user);
            if (!avatarStatusResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = avatarStatusResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return avatarStatusResult.ToInvokeResult<VideoProduction>();
            }

            var lookResult = ResolveAvatarLook(production, avatarStatusResult.Result);
            if (!lookResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = lookResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return lookResult.ToInvokeResult<VideoProduction>();
            }

            var selectedLook = lookResult.Result;

            if (selectedLook.Status?.Value != VideoAvatarStatus.Ready || String.IsNullOrWhiteSpace(selectedLook.ProviderAvatarId))
            {
                production.SetStatus(VideoProductionStatus.WaitingForAvatar);
                production.ErrorMessage = $"Video avatar look '{selectedLook.Name ?? selectedLook.Id}' is not ready.";
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
            }

            ApplyAvatarToProduction(production, avatarStatusResult.Result, selectedLook);

            var backgroundResult = await ResolveProviderBackgroundAssetAsync(production, org, user);
            if (!backgroundResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = backgroundResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return backgroundResult.ToInvokeResult<VideoProduction>();
            }

            production.ProviderBackgroundAssetId = backgroundResult.Result;
            production.SetStatus(VideoProductionStatus.Submitting);
            production.ErrorMessage = null;
            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var webhookResult = await _heyGenVideoService.EnsureWebhookRegistrationAsync(_webhookSecretOwner, user, _heyGenWebhookCallbackUrl);
            if (!webhookResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = webhookResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);

                return webhookResult.ToInvokeResult<VideoProduction>();
            }

            var submitRequest = BuildHeyGenRequest(production);
            var submitResult = await _heyGenVideoService.SubmitVideoAsync(submitRequest, production.Quality?.Value ?? VideoProductionQuality.Standard, production.Settings);
            if (!submitResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = submitResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return submitResult.ToInvokeResult<VideoProduction>();
            }

            production.ProviderVideoId = submitResult.Result.VideoId;
            production.SetStatus(VideoProductionStatus.Submitted);
            production.SubmittedUtc = UtcTimestamp.Now;
            production.LastStatusCheckUtc = production.SubmittedUtc;
            production.ErrorMessage = null;

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        public async Task<InvokeResult<VideoProduction>> ProcessHeyGenWebhookAsync(HeyGenWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
        {
            if (webhookEvent == null)
            {
                return InvokeResult<VideoProduction>.FromError("HeyGen webhook event is required.");
            }

            if (String.IsNullOrWhiteSpace(webhookEvent.EventId))
            {
                return InvokeResult<VideoProduction>.FromError("HeyGen webhook event ID is required.");
            }

            if (String.IsNullOrWhiteSpace(webhookEvent.EventType))
            {
                return InvokeResult<VideoProduction>.FromError("HeyGen webhook event type is required.");
            }

            var isSuccess = String.Equals(webhookEvent.EventType, HeyGenWebhookConstants.EventVideoSuccess, StringComparison.OrdinalIgnoreCase);
            var isFailure = String.Equals(webhookEvent.EventType, HeyGenWebhookConstants.EventVideoFail, StringComparison.OrdinalIgnoreCase);

            if (!isSuccess && !isFailure)
            {
                return InvokeResult<VideoProduction>.FromError($"Unsupported HeyGen webhook event type '{webhookEvent.EventType}'.");
            }

            var receiptKey = CreateWebhookReceiptKey(webhookEvent.EventId);
            var existingReceipt = await _cacheProvider.GetAsync(receiptKey);

            if (!String.IsNullOrWhiteSpace(existingReceipt))
            {
                return InvokeResult<VideoProduction>.Create(null);
            }

            var lockKey = CreateWebhookProcessingLockKey(webhookEvent.EventId);
            var lockToken = Guid.NewGuid().ToString("N");
            var lockAcquired = await _cacheProvider.AttemptAcquireLockAsync(lockKey, lockToken, WebhookProcessingLockDuration);

            if (!lockAcquired)
            {
                return InvokeResult<VideoProduction>.FromError("The HeyGen webhook event is already being processed.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                existingReceipt = await _cacheProvider.GetAsync(receiptKey);

                if (!String.IsNullOrWhiteSpace(existingReceipt))
                {
                    return InvokeResult<VideoProduction>.Create(null);
                }

                var eventData = webhookEvent.EventData?.ToObject<HeyGenVideoWebhookData>();

                if (eventData == null)
                {
                    return InvokeResult<VideoProduction>.FromError("HeyGen webhook event data is required.");
                }

                var production = await ResolveWebhookProductionAsync(eventData);

                if (production == null)
                {
                    return InvokeResult<VideoProduction>.FromError(
                        $"Could not locate the video production for callback '{eventData.CallbackId}' and provider video '{eventData.VideoId}'.");
                }

                if (!String.IsNullOrWhiteSpace(eventData.VideoId) &&
                    !String.IsNullOrWhiteSpace(production.ProviderVideoId) &&
                    !String.Equals(eventData.VideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeResult<VideoProduction>.FromError(
                        $"HeyGen webhook video ID '{eventData.VideoId}' does not match production provider video ID '{production.ProviderVideoId}'.");
                }

                VideoProduction currentProduction;

                if (isSuccess)
                {
                    currentProduction = await ApplySuccessfulWebhookAsync(production, eventData);
                }
                else
                {
                    currentProduction = await ApplyFailedWebhookAsync(production, eventData);
                }

                await PublishVideoProductionUpdatedAsync(currentProduction);

                if (isSuccess)
                {
                    QueueProviderVideoStatusRefresh(currentProduction.Id, currentProduction.OwnerOrganization, currentProduction.LastUpdatedBy);
                }

                await _cacheProvider.AddAsync(receiptKey, webhookEvent.EventType, WebhookReceiptDuration);

                return InvokeResult<VideoProduction>.Create(currentProduction);
            }
            finally
            {
                await _cacheProvider.ReleaseLockAsync(lockKey, lockToken);
            }
        }

        private async Task<VideoProduction> ResolveWebhookProductionAsync(HeyGenVideoWebhookData eventData)
        {
            if (!String.IsNullOrWhiteSpace(eventData.CallbackId))
            {
                var callbackProduction = await _repo.GetVideoProductionAsync(eventData.CallbackId);

                if (callbackProduction != null)
                {
                    return callbackProduction;
                }
            }

            if (!String.IsNullOrWhiteSpace(eventData.VideoId))
            {
                return await _repo.GetVideoProductionByProviderVideoIdAsync(eventData.VideoId);
            }

            return null;
        }

        private async Task<VideoProduction> ApplySuccessfulWebhookAsync(VideoProduction production, HeyGenVideoWebhookData eventData)
        {
            var state = new VideoProductionProviderState
            {
                ProviderVideoUrl = null,
                ProviderThumbnailUrl = production.ProviderThumbnailUrl,
                ProviderCaptionUrl = production.ProviderCaptionUrl,
                ActualDurationSeconds = production.ActualDurationSeconds,
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderCompleted),
                CompletedUtc = UtcTimestamp.Now,
                LastStatusCheckUtc = UtcTimestamp.Now,
                ErrorMessage = null
            };

            var duration = production.ActualDurationSeconds.HasValue ? (double)production.ActualDurationSeconds : 0;

            var quality = production.Quality?.Text ?? production.Quality?.Id ?? VideoProductionQuality.Standard.ToString();
            await _billingEventRecorder.RecordUsageAsync(BillingEventType.VideoGenerationStandard, duration, $"HeyGen {quality} Video Production {production.Name}, duration: {duration}", production.OwnerOrganization, production.LastUpdatedBy);

            return await _repo.UpdateVideoProductionProviderStateAsync(production.Id, state);
        }

        private async Task<VideoProduction> ApplyFailedWebhookAsync(VideoProduction production, HeyGenVideoWebhookData eventData)
        {
            var state = new VideoProductionProviderState
            {
                ProviderVideoUrl = production.ProviderVideoUrl,
                ProviderThumbnailUrl = production.ProviderThumbnailUrl,
                ProviderCaptionUrl = production.ProviderCaptionUrl,
                ActualDurationSeconds = production.ActualDurationSeconds,
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed),
                CompletedUtc = production.CompletedUtc,
                LastStatusCheckUtc = UtcTimestamp.Now,
                ErrorMessage = ResolveWebhookErrorMessage(eventData)
            };

            var duration = production.ActualDurationSeconds.HasValue ? (double)production.ActualDurationSeconds : 0;

            var quality = production.Quality?.Text ?? production.Quality?.Id ?? VideoProductionQuality.Standard.ToString();
            await _billingEventRecorder.RecordUsageAsync(BillingEventType.VideoGenerationPremium, duration, $"Failed HeyGen {quality} Video Production {production.Name}, duration: {duration}", production.OwnerOrganization, production.LastUpdatedBy);

            return await _repo.UpdateVideoProductionProviderStateAsync(production.Id, state);
        }

        public async Task<InvokeResult<VideoProduction>> RefreshVimeoVideoProductionStatusAsync(string id, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (String.IsNullOrWhiteSpace(production.VimeoVideoUri))
            {
                return InvokeResult<VideoProduction>.FromError("The video production does not have a Vimeo video URI.");
            }

            return await RefreshVimeoStatusCoreAsync(production, org, user, cancellationToken);
        }

        private static void ApplyVimeoStatus(VideoProduction production, VimeoVideo video)
        {
            production.VimeoVideoUri = video.Uri;
            production.VimeoVideoId = ResolveVimeoVideoId(video.Uri);

            if (!String.IsNullOrWhiteSpace(video.Link))
            {
                production.VimeoVideoUrl = video.Link;
            }

            var uploadStatus = video.Upload?.Status;
            var transcodeStatus = video.Transcode?.Status;

            if (String.Equals(uploadStatus, "error", StringComparison.OrdinalIgnoreCase))
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = String.IsNullOrWhiteSpace(video.Upload?.Error) ? "Vimeo failed to import the video." : video.Upload.Error;
                return;
            }

            if (String.Equals(transcodeStatus, "error", StringComparison.OrdinalIgnoreCase))
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = "Vimeo failed to process the video.";
                return;
            }

            if (!String.Equals(uploadStatus, "complete", StringComparison.OrdinalIgnoreCase))
            {
                production.SetStatus(VideoProductionStatus.ImportingToVimeo);
                production.ErrorMessage = null;
                return;
            }

            if (!String.Equals(transcodeStatus, "complete", StringComparison.OrdinalIgnoreCase))
            {
                production.SetStatus(VideoProductionStatus.ProcessingAtVimeo);
                production.ErrorMessage = null;
                return;
            }

            production.SetStatus(VideoProductionStatus.Completed);
            production.CompletedUtc = String.IsNullOrWhiteSpace(production.CompletedUtc) ? UtcTimestamp.Now.Value : production.CompletedUtc;
            production.ErrorMessage = null;
        }

        private static string ResolveWebhookErrorMessage(HeyGenVideoWebhookData eventData)
        {
            if (!String.IsNullOrWhiteSpace(eventData.ErrorMessage))
            {
                return eventData.ErrorMessage;
            }

            if (!String.IsNullOrWhiteSpace(eventData.Message))
            {
                return eventData.Message;
            }

            if (eventData.Error != null && eventData.Error.Type != Newtonsoft.Json.Linq.JTokenType.Null)
            {
                return eventData.Error.Type == Newtonsoft.Json.Linq.JTokenType.String
                    ? eventData.Error.ToObject<string>()
                    : eventData.Error.ToString(Formatting.None);
            }

            if (!String.IsNullOrWhiteSpace(eventData.ErrorCode))
            {
                return $"HeyGen video generation failed with code '{eventData.ErrorCode}'.";
            }

            return "HeyGen video generation failed.";
        }

      
        private void QueueProviderVideoStatusRefresh(string productionId, EntityHeader org, EntityHeader user)
        {
            var queue = BackgroundServiceTaskQueueProvider.Instance;
            var refreshOrg = EntityHeader.Create(org.Id, org.Text);
            var refreshUser = EntityHeader.Create(user.Id, user.Text);

            if (queue == null)
            {
                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Background task queue is unavailable. Provider video status refresh was not queued for production '{productionId}'.");
                return;
            }

            var queued = queue.TryQueueBackgroundWorkItem(cancellationToken => RefreshProviderVideoStatusInBackgroundAsync(productionId, refreshOrg, refreshUser, cancellationToken));

            if (!queued)
            {
                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Provider video status refresh could not be queued for production '{productionId}'.");
            }
        }

        private async Task RefreshProviderVideoStatusInBackgroundAsync(string productionId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var refreshResult = await RefreshVideoProductionStatusAsync(productionId, org, user);

                if (!refreshResult.Successful)
                {
                    var errorMessage = refreshResult.Errors?.FirstOrDefault()?.Message ?? "The provider video status refresh failed.";
                    _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Provider video status refresh failed for production '{productionId}'. {errorMessage}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex, new KeyValuePair<string, string>("ProductionId", productionId));
            }
        }

        private static string CreateWebhookProcessingLockKey(string eventId)
        {
            return $"media-services:heygen:webhook-processing:{eventId}".ToLowerInvariant();
        }

        private static string CreateWebhookReceiptKey(string eventId)
        {
            return $"media-services:heygen:webhook-receipt:{eventId}".ToLowerInvariant();
        }

        public async Task<InvokeResult<VideoProduction>> RefreshVideoProductionStatusAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (String.IsNullOrWhiteSpace(production.ProviderVideoId))
            {
                return InvokeResult<VideoProduction>.FromError("Provider video ID has not been created.");
            }

            var statusResult = await _heyGenVideoService.GetVideoStatusAsync(production.ProviderVideoId);
            if (!statusResult.Successful)
            {
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                production.ErrorMessage = statusResult.Errors[0].Message;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return statusResult.ToInvokeResult<VideoProduction>();
            }

            var providerStatus = statusResult.Result;

            production.LastStatusCheckUtc = UtcTimestamp.Now;
            production.ProviderVideoUrl = providerStatus.VideoUrl ?? production.ProviderVideoUrl;
            production.ProviderThumbnailUrl = providerStatus.ThumbnailUrl ?? production.ProviderThumbnailUrl;
            production.ProviderCaptionUrl = providerStatus.CaptionUrl ?? production.ProviderCaptionUrl;

            if (providerStatus.DurationSeconds.HasValue)
            {
                production.ActualDurationSeconds = Convert.ToInt32(Math.Ceiling(providerStatus.DurationSeconds.Value));
            }

            switch (providerStatus.Status?.Trim().ToLowerInvariant())
            {
                case "pending":
                case "waiting":
                    production.SetStatus(VideoProductionStatus.Submitted);
                    production.ErrorMessage = null;
                    break;

                case "processing":
                    production.SetStatus(VideoProductionStatus.Rendering);
                    production.ErrorMessage = null;
                    break;

                case "completed":
                    production.SetStatus(VideoProductionStatus.ProviderCompleted);
                    production.CompletedUtc = production.CompletedUtc ?? UtcTimestamp.Now;
                    production.ErrorMessage = null;
                    break;

                case "failed":
                    production.SetStatus(VideoProductionStatus.Failed);
                    production.ErrorMessage = ResolveVideoStatusErrorMessage(providerStatus);
                    break;

                default:
                    production.ErrorMessage = $"HeyGen returned the unrecognized video status '{providerStatus.Status}'.";
                    break;
            }

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private static string ResolveVideoStatusErrorMessage(HeyGenVideoStatusResult status)
        {
            if (!String.IsNullOrWhiteSpace(status.ErrorMessage))
            {
                return status.ErrorMessage;
            }

            if (!String.IsNullOrWhiteSpace(status.ErrorCode))
            {
                return $"HeyGen video generation failed with code '{status.ErrorCode}'.";
            }

            return "HeyGen video generation failed.";
        }

        private async Task<InvokeResult<string>> ResolveProviderBackgroundAssetAsync(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            if (production.BackgroundMediaResource == null || String.IsNullOrWhiteSpace(production.BackgroundMediaResource.Id))
            {
                production.ProviderBackgroundAssetId = null;
                return InvokeResult<string>.Create(null);
            }

            var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(production.BackgroundMediaResource.Id, org, user);
            if (mediaResource == null)
            {
                return InvokeResult<string>.FromError($"Could not find background media resource '{production.BackgroundMediaResource.Id}'.");
            }

            var externalAsset = mediaResource.ExternalAssets?.FirstOrDefault(asset =>
                asset.Provider?.Value == MediaExternalAssetProvider.HeyGen &&
                asset.Purpose?.Value == MediaExternalAssetPurpose.ProcessingAsset &&
                !String.IsNullOrWhiteSpace(asset.ProviderAssetId) &&
                String.Equals(asset.ContentSha256, mediaResource.ContentSha256, StringComparison.OrdinalIgnoreCase));

            if (externalAsset != null)
            {
                return InvokeResult<string>.Create(externalAsset.ProviderAssetId);
            }

            production.SetStatus(VideoProductionStatus.UploadingBackground);
            production.ProviderBackgroundAssetId = null;
            production.ErrorMessage = null;
            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var content = await _mediaServicesManager.GetResourceMediaAsync(mediaResource.Id, org, user);
            if (content == null || content.ImageBytes == null || content.ImageBytes.Length == 0)
            {
                return InvokeResult<string>.FromError($"Background media resource '{mediaResource.Id}' does not contain content.");
            }

            using var stream = new MemoryStream(content.ImageBytes, writable: false);

            var uploadResult = await _heyGenVideoService.UploadAssetAsync(stream, content.FileName, content.ContentType, mediaResource.Id);
            if (!uploadResult.Successful)
            {
                return uploadResult.ToInvokeResult<string>();
            }

            if (mediaResource.ExternalAssets == null)
            {
                mediaResource.ExternalAssets = new List<MediaExternalAsset>();
            }

            externalAsset = mediaResource.ExternalAssets.FirstOrDefault(asset =>
                asset.Provider?.Value == MediaExternalAssetProvider.HeyGen &&
                asset.Purpose?.Value == MediaExternalAssetPurpose.ProcessingAsset &&
                String.Equals(asset.ContentSha256, mediaResource.ContentSha256, StringComparison.OrdinalIgnoreCase));

            if (externalAsset == null)
            {
                externalAsset = new MediaExternalAsset
                {
                    Provider = EntityHeader<MediaExternalAssetProvider>.Create(MediaExternalAssetProvider.HeyGen),
                    Purpose = EntityHeader<MediaExternalAssetPurpose>.Create(MediaExternalAssetPurpose.ProcessingAsset),
                    CreatedUtc = UtcTimestamp.Now
                };

                mediaResource.ExternalAssets.Add(externalAsset);
            }

            externalAsset.ProviderAssetId = uploadResult.Result.AssetId;
            externalAsset.ContentSha256 = mediaResource.ContentSha256;
            externalAsset.Status = EntityHeader<MediaExternalAssetStatus>.Create(MediaExternalAssetStatus.Ready);
            externalAsset.ReadyUtc = UtcTimestamp.Now;
            externalAsset.LastStatusCheckUtc = UtcTimestamp.Now;
            externalAsset.ErrorMessage = null;

            await _mediaServicesManager.UpdateMediaResourceRecordAsync(mediaResource, org, user);

            return InvokeResult<string>.Create(externalAsset.ProviderAssetId);
        }

        private static HeyGenVideoRequest BuildHeyGenRequest(VideoProduction production)
        {
            return new HeyGenVideoRequest
            {
                Type = "avatar",
                AvatarId = production.ProviderAvatarId,
                Script = production.Script,
                VoiceId = production.VoiceId,
                Title = production.VideoName,
                CallbackId = production.Id,
                Resolution = "1080p",
                AspectRatio = "16:9",
                OutputFormat = "mp4",
                Background = String.IsNullOrWhiteSpace(production.ProviderBackgroundAssetId) ? null : new HeyGenBackground { AssetId = production.ProviderBackgroundAssetId },
                VoiceSettings = String.IsNullOrWhiteSpace(production.Locale) ? null : new HeyGenVoiceSettings { Locale = production.Locale }
            };
        }

        private static InvokeResult<VideoAvatarLook> ResolveAvatarLook(VideoProduction production, VideoAvatar avatar)
        {
            var activeLooks = avatar.Looks?
                .Where(look => look != null && look.IsActive)
                .ToList() ?? new System.Collections.Generic.List<VideoAvatarLook>();

            if (activeLooks.Count == 0)
            {
                return InvokeResult<VideoAvatarLook>.FromError($"Video avatar '{avatar.Name}' does not have any active looks.");
            }

            VideoAvatarLook selectedLook;

            if (!String.IsNullOrWhiteSpace(production.VideoAvatarLookId))
            {
                selectedLook = activeLooks.FirstOrDefault(look => String.Equals(look.Id, production.VideoAvatarLookId, StringComparison.OrdinalIgnoreCase));

                if (selectedLook == null)
                {
                    return InvokeResult<VideoAvatarLook>.FromError($"Video avatar look '{production.VideoAvatarLookId}' could not be found or is inactive.");
                }
            }
            else
            {
                selectedLook = activeLooks.FirstOrDefault(look => look.IsPrimary) ?? activeLooks.First();
            }

            return InvokeResult<VideoAvatarLook>.Create(selectedLook);
        }

        private static void ApplyAvatarToProduction(VideoProduction production, VideoAvatar avatar, VideoAvatarLook look)
        {
            production.VideoAvatarLookId = look.Id;
            production.ProviderAvatarId = look.ProviderAvatarId;

            var defaultVoice = avatar.GetDefaultVoice();

            if(defaultVoice == null)
            {
                throw new InvalidOperationException($"Avatar {avatar.Name} does not have a default voice.");
            }

            if (String.IsNullOrWhiteSpace(production.VoiceId))
            {
                production.VoiceId = defaultVoice.VoiceId;
            }

            if (String.IsNullOrWhiteSpace(production.VoiceName))
            {
                production.VoiceName = defaultVoice.VoiceName;
            }

            if (String.IsNullOrWhiteSpace(production.LanguageCode))
            {
                production.LanguageCode = defaultVoice.LanguageCode;
            }

            if (String.IsNullOrWhiteSpace(production.Locale))
            {
                production.Locale = defaultVoice.Locale;
            }
        }

        private static void NormalizeVideoProduction(VideoProduction production)
        {
            if (production == null)
            {
                return;
            }

            if (production.Provider == null)
            {
                production.Provider = EntityHeader<VideoProductionProvider>.Create(VideoProductionProvider.HeyGen);
            }

            if (production.Status == null)
            {
                production.SetStatus(VideoProductionStatus.Draft);
            }

            if (production.Quality == null)
            {
                production.Quality = EntityHeader<VideoProductionQuality>.Create(VideoProductionQuality.Standard);
            }

            if (production.Settings == null)
            {
                production.Settings = new VideoProductionSettings();
            }

            if (production.Settings.Width <= 0)
            {
                production.Settings.Width = 1920;
            }

            if (production.Settings.Height <= 0)
            {
                production.Settings.Height = 1080;
            }

            if (String.IsNullOrWhiteSpace(production.CostCurrency))
            {
                production.CostCurrency = "USD";
            }

            if (String.IsNullOrWhiteSpace(production.DefaultLocale))
            {
                production.DefaultLocale = VideoProduction.DefaultLocaleCode;
            }
        }

        private async Task PublishVideoProductionUpdatedAsync(VideoProduction production)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, production.Id, "video-production-updated", production);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, production.OwnerOrganization.Id, "video-production-updated", production);
        }

        private static int EstimateDurationSeconds(string script, int wordsPerMinute = 150)
        {
            if (String.IsNullOrWhiteSpace(script))
            {
                return 0;
            }

            var words = script.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return Math.Max(1, (int)Math.Ceiling(words.Length / (double)wordsPerMinute * 60));
        }

        private static decimal EstimateCost(int durationSeconds, decimal unitCostPerSecond)
        {
            return Math.Round(durationSeconds * unitCostPerSecond, 4);
        }
    }
}