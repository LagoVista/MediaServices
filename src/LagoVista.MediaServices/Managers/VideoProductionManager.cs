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
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly IVideoMediaImportManager _videoMediaImportManager;
        private readonly IBillingEventRecorder _billingEventRecorder;

        private static readonly HttpClient PreviewAudioHttpClient = new HttpClient();
        private static readonly Regex HeyGenPauseMarkerRegex = new Regex(@"\[pause\s*=\s*(?<seconds>\d+(?:\.\d+)?)\s*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly TimeSpan VimeoStatusPollingInterval = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan VimeoStatusPollingTimeout = TimeSpan.FromMinutes(30);

        private static readonly TimeSpan WebhookProcessingLockDuration = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan WebhookReceiptDuration = TimeSpan.FromDays(14);
        private readonly ILogger _logger;


        public VideoProductionManager(IVideoProductionRepo repo, IVideoAvatarManager videoAvatarManager, IHeyGenVideoService heyGenVideoService, IVimeoVideoService vimeoVideoService, IMediaServicesRepo mediaRepo, IMediaServicesManager mediaServicesManager, IVideoMediaImportManager videoMediaImportManager, IOrganizationLoaderRepo organizationLoaderRepo, ISecureStorage secureStorage, ICacheProvider cacheProvider, ICoreAppServices coreAppServices, IBillingEventRecorder billingEventRecorder) : base(coreAppServices)
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
            _videoMediaImportManager = videoMediaImportManager ?? throw new ArgumentNullException(nameof(videoMediaImportManager));

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
            // A look and voice binding belong to a specific avatar. Do not carry the
            // previous avatar's binding into an edited production after an avatar swap.
            if (existing != null &&
                !String.Equals(existing.VideoAvatar?.Id, production.VideoAvatar?.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(production.VideoAvatarLookId, existing.VideoAvatarLookId, StringComparison.OrdinalIgnoreCase))
                    production.VideoAvatarLookId = null;

                if (String.Equals(production.ProviderAvatarId, existing.ProviderAvatarId, StringComparison.OrdinalIgnoreCase))
                    production.ProviderAvatarId = null;

                if (String.Equals(production.VoiceBindingId, existing.VoiceBindingId, StringComparison.OrdinalIgnoreCase))
                {
                    production.VoiceBindingId = null;
                    production.VoiceId = null;
                    production.VoiceName = null;
                    production.LanguageCode = null;
                    production.Locale = null;
                }
            }

            if (production.FinalVideoMediaResource != null && !String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                var mediaResource = await _mediaRepo.GetMediaResourceRecordAsync(production.FinalVideoMediaResource.Id);
                if (mediaResource != null)
                {
                    var mediaResourceChanged = false;

                    if (existing.Name != production.Name)
                    {
                        mediaResource.Name = production.Name;
                        mediaResourceChanged = true;
                    }

                    if (production.OutputMediaLibrary != null && !String.IsNullOrWhiteSpace(production.OutputMediaLibrary.Id) && !String.Equals(mediaResource.MediaLibrary?.Id, production.OutputMediaLibrary.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        mediaResource.MediaLibrary = production.OutputMediaLibrary;
                        mediaResourceChanged = true;
                    }

                    if (mediaResourceChanged)
                    {
                        mediaResource.LastUpdatedBy = user;
                        mediaResource.LastUpdatedDate = UtcTimestamp.Now;
                        await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);
                    }
                }
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
            production.ErrorMessage = null;
            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            var script = BuildHeyGenScript(production.Script);
            _logger.Trace($"{this.Tag()} - Preview Script: {script}");

            var previewResult = await _heyGenVideoService.GenerateSpeechPreviewAsync(new HeyGenSpeechPreviewRequest
            {
                VoiceId = production.VoiceId,
                Text = script,
                Locale = String.IsNullOrWhiteSpace(production.Locale) ? production.DefaultLocale : production.Locale
            });


            if (!previewResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = previewResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return previewResult.ToInvokeResult<VideoProduction>();
            }

            if (String.IsNullOrWhiteSpace(previewResult.Result.AudioUrl))
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = "HeyGen did not return a preview audio URL.";
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
            }

            byte[] audioBytes;
            string contentType;

            try
            {
                using var audioResponse = await PreviewAudioHttpClient.GetAsync(previewResult.Result.AudioUrl);
                if (!audioResponse.IsSuccessStatusCode)
                {
                    production.SetStatus(VideoProductionStatus.Failed);
                    production.ErrorMessage = $"Could not download HeyGen preview audio. Status: {(int)audioResponse.StatusCode}.";
                    production.LastStatusCheckUtc = UtcTimestamp.Now;

                    await _repo.UpdateVideoProductionAsync(production);
                    await PublishVideoProductionUpdatedAsync(production);

                    return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
                }

                audioBytes = await audioResponse.Content.ReadAsByteArrayAsync();
                contentType = audioResponse.Content.Headers.ContentType?.MediaType;
            }
            catch (Exception ex)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = $"Could not download HeyGen preview audio. {ex.Message}";
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
            }

            if (audioBytes == null || audioBytes.Length == 0)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = "HeyGen preview audio was empty.";
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return InvokeResult<VideoProduction>.FromError(production.ErrorMessage);
            }

            contentType = String.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType;
            var fileExtension = ResolvePreviewAudioExtension(contentType);
            var fileName = $"{CreatePreviewAudioFileName(production)}.{fileExtension}";
            var scriptSha256 = ComputeSha256(production.Script);
            var generatedUtc = UtcTimestamp.Now;

            InvokeResult<MediaResource> mediaResult;
            using (var audioStream = new MemoryStream(audioBytes, writable: false))
            {
                if (production.PreviewAudioMediaResource == null || String.IsNullOrWhiteSpace(production.PreviewAudioMediaResource.Id))
                {
                    mediaResult = await _mediaServicesManager.AddResourceMediaAsync(Guid.NewGuid().ToId(), audioStream, fileName, contentType, org, user, true, false, resourceName: $"{production.Name} Preview Audio");
                }
                else
                {
                    mediaResult = await _mediaServicesManager.AddResourceMediaRevisionAsync(production.PreviewAudioMediaResource.Id, audioStream, fileName, contentType, org, user, true);
                }
            }

            if (!mediaResult.Successful)
            {
                production.SetStatus(VideoProductionStatus.Failed);
                production.ErrorMessage = mediaResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;

                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);

                return mediaResult.ToInvokeResult<VideoProduction>();
            }

            var mediaResource = mediaResult.Result;
            mediaResource.DurationSeconds = previewResult.Result.DurationSeconds;
            mediaResource.SourceEntityType = nameof(VideoProduction);
            mediaResource.SourceEntity = EntityHeader.Create(production.Id, production.Name);

            var currentRevision = mediaResource.GetCurrentRevision();
            if (currentRevision == null)
            {
                return InvokeResult<VideoProduction>.FromError("Preview audio Media Resource was created without a current revision.");
            }

            currentRevision.DurationSeconds = previewResult.Result.DurationSeconds;
            currentRevision.MimeType = contentType;
            currentRevision.FileName = fileName;
            currentRevision.ContentSize = audioBytes.LongLength;
            currentRevision.ContentSha256 = ComputeSha256(audioBytes);
            currentRevision.Status = EntityHeader<MediaResourceStatus>.Create(MediaResourceStatus.Ready);
            currentRevision.CompletedUtc = generatedUtc;

            var actualCost = previewResult.Result.EstimatedCost ?? (previewResult.Result.DurationSeconds.HasValue
                ? EstimateCost(previewResult.Result.DurationSeconds.Value, 0.000667m)
                : (decimal?)null);

            GuidString36? billingEventId = null;
            if (previewResult.Result.DurationSeconds.HasValue && previewResult.Result.DurationSeconds.Value > 0)
            {
                _logger.Trace($"{this.Tag()} - HeyGen preview audio for Video Production {{production.Name}}, duration: {{previewResult.Result.DurationSeconds.Value}}");
                billingEventId = await _billingEventRecorder.RecordUsageAsync(BillingEventType.AudioGeneration, previewResult.Result.DurationSeconds.Value, $"HeyGen preview audio for Video Production {production.Name}, duration: {previewResult.Result.DurationSeconds.Value}", production.OwnerOrganization, production.LastUpdatedBy);
            }

            currentRevision.AudioGenerationReceipt = new AudioGenerationReceipt
            {
                Provider = "HeyGen",
                ProviderRequestId = previewResult.Result.AudioUrl,
                ScriptSha256 = scriptSha256,
                VoiceId = production.VoiceId,
                Locale = String.IsNullOrWhiteSpace(production.Locale) ? production.DefaultLocale : production.Locale,
                VoiceSpeed = production.Settings?.VoiceSpeed,
                VoicePitch = production.Settings?.VoicePitch,
                VoiceVolume = production.Settings?.VoiceVolume,
                DurationSeconds = previewResult.Result.DurationSeconds,
                ActualCost = actualCost,
                Currency = String.IsNullOrWhiteSpace(previewResult.Result.Currency) ? "USD" : previewResult.Result.Currency,
                BillingEventId = billingEventId?.ToString(),
                GeneratedUtc = generatedUtc
            };

            await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);

            production.PreviewAudioMediaResource = EntityHeader.Create(mediaResource.Id, mediaResource.Name);
            production.PreviewAudioScriptSha256 = scriptSha256;
            production.PreviewAudioGeneratedUtc = generatedUtc;
            production.PreviewAudioBillingEventId = billingEventId?.ToString();
            production.ActualPreviewAudioCost = actualCost;
            production.PreviewAudioGenerationCount++;
            production.TotalPreviewAudioCost = (production.TotalPreviewAudioCost ?? 0) + (actualCost ?? 0);
            RecalculateProductionCosts(production);
            production.CostCurrency = String.IsNullOrWhiteSpace(previewResult.Result.Currency) ? "USD" : previewResult.Result.Currency;
            production.SetStatus(VideoProductionStatus.PreviewAudioReady);
            production.ErrorMessage = null;
            production.LastStatusCheckUtc = generatedUtc;

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private static string ComputeSha256(string value)
        {
            return ComputeSha256(Encoding.UTF8.GetBytes(value ?? String.Empty));
        }

        private static string ComputeSha256(byte[] value)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(value ?? Array.Empty<byte>());
            return String.Concat(hash.Select(item => item.ToString("x2")));
        }

        private static string CreatePreviewAudioFileName(VideoProduction production)
        {
            var sourceName = String.IsNullOrWhiteSpace(production.VideoName) ? production.Name : production.VideoName;
            var safeName = new String((sourceName ?? "video-production-preview")
                .Select(character => Char.IsLetterOrDigit(character) ? Char.ToLowerInvariant(character) : '-')
                .ToArray());

            while (safeName.Contains("--"))
            {
                safeName = safeName.Replace("--", "-");
            }

            safeName = safeName.Trim('-');
            return String.IsNullOrWhiteSpace(safeName) ? "video-production-preview" : $"{safeName}-preview";
        }

        private static string ResolvePreviewAudioExtension(string contentType)
        {
            switch ((contentType ?? String.Empty).Trim().ToLowerInvariant())
            {
                case "audio/wav":
                case "audio/x-wav":
                    return "wav";
                case "audio/ogg":
                    return "ogg";
                case "audio/mp4":
                case "audio/m4a":
                    return "m4a";
                default:
                    return "mp3";
            }
        }

        public async Task<InvokeResult<VideoProduction>> SubmitVideoProductionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            NormalizeVideoProduction(production);

            var outputModeValidation = ValidateOutputMode(production);
            if (!outputModeValidation.Successful)
            {
                return outputModeValidation.ToInvokeResult<VideoProduction>();
            }

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
                // Invalid new inputs must not replace the status of an already imported
                // video. Preserve its MediaResource and cost history while the user
                // selects a valid look. Without an imported video, await a valid avatar.
                if (!HasCompletedCurrentProviderImport(production))
                    production.SetStatus(VideoProductionStatus.WaitingForAvatar);

                production.ErrorMessage = lookResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);
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

            if (production.Settings.OutputMode.Value == VideoProductionOutputMode.TransparentPresenter)
            {
                production.ProviderBackgroundAssetId = null;
            }
            else
            {
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
            }
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
            var submitResult = await _heyGenVideoService.SubmitVideoAsync(submitRequest);
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
            production.VideoGenerationBillingEventId = null;

            EnsureRuns(production);
            production.Runs.Insert(0, new VideoProductionRun
            {
                ProviderVideoId = production.ProviderVideoId,
                ProviderRequestId = submitResult.Result.RequestId,
                QualityAtSubmission = production.Quality,
                BillingRequired = true,
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Submitted),
                InputSha256 = production.ExecutionInputSha256 ?? production.CurrentInputSha256,
                SubmittedUtc = production.SubmittedUtc,
                LastStatusCheckUtc = production.LastStatusCheckUtc
            });

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
                await _cacheProvider.AddAsync(receiptKey, webhookEvent.EventType, WebhookReceiptDuration);

                if (isSuccess)
                {
                    QueueProviderVideoImport(currentProduction.Id, currentProduction.OwnerOrganization, currentProduction.LastUpdatedBy);
                    QueueProviderVideoStatusRefresh(currentProduction.Id, currentProduction.OwnerOrganization, currentProduction.LastUpdatedBy);
                }

                return InvokeResult<VideoProduction>.Create(currentProduction);
            }
            finally
            {
                await _cacheProvider.ReleaseLockAsync(lockKey, lockToken);
            }
        }

        private void QueueProviderVideoImport(string productionId, EntityHeader org, EntityHeader user)
        {
            if (String.IsNullOrWhiteSpace(productionId) || org == null || user == null)
            {
                return;
            }

            var importOrg = EntityHeader.Create(org.Id, org.Text);
            var importUser = EntityHeader.Create(user.Id, user.Text);
            var queue = BackgroundServiceTaskQueueProvider.Instance;

            if (queue == null)
            {
                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Background task queue is unavailable. Provider video import was not queued for production '{productionId}'.");
                return;
            }

            var queued = queue.TryQueueBackgroundWorkItem(async cancellationToken =>
            {
                try
                {
                    var importResult = await _videoMediaImportManager.EnsureProviderVideoImportAsync(productionId, 3, importOrg, importUser, cancellationToken);
                    if (!importResult.Successful)
                    {
                        var errorMessage = importResult.Errors?.FirstOrDefault()?.Message ?? "Provider video import could not be started.";
                        _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Automatic provider video import failed for production '{productionId}'. {errorMessage}");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.AddException(this.Tag(), ex, new KeyValuePair<string, string>("ProductionId", productionId));
                }
            });

            if (!queued)
            {
                _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Automatic provider video import could not be queued for production '{productionId}'.");
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
            // The provider reports rendering completion, not completion of our downstream import.
            // A delayed/replayed webhook must not move an already imported video backwards.
            var latest = await _repo.GetVideoProductionAsync(production.Id);
            if (HasCompletedCurrentProviderImport(latest) || IsImportOrLater(latest))
            {
                return latest;
            }

            var state = new VideoProductionProviderState
            {
                ProviderVideoUrl = eventData.VideoUrl,
                ProviderThumbnailUrl = production.ProviderThumbnailUrl,
                ProviderCaptionUrl = production.ProviderCaptionUrl,
                ActualDurationSeconds = production.ActualDurationSeconds,
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderCompleted),
                CompletedUtc = UtcTimestamp.Now,
                LastStatusCheckUtc = UtcTimestamp.Now,
                ErrorMessage = null
            };

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

                // Reconcile independently of provider status: an import callback may
                // already have advanced the production past ProviderCompleted.
                var reconcileResult = await ReconcileVideoProductionCostsAsync(productionId, org, user, cancellationToken);
                if (!reconcileResult.Successful)
                    _logger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"HeyGen charge reconciliation failed for production '{productionId}': {reconcileResult.Errors?.FirstOrDefault()?.Message}");
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

        private static bool IsImportOrLater(VideoProduction production)
        {
            switch (production?.Status?.Value)
            {
                case VideoProductionStatus.ImportingProviderVideo:
                case VideoProductionStatus.ProviderVideoReady:
                case VideoProductionStatus.ImportingToVimeo:
                case VideoProductionStatus.ProcessingAtVimeo:
                case VideoProductionStatus.UpdatingEntity:
                case VideoProductionStatus.Completed:
                case VideoProductionStatus.Failed:
                case VideoProductionStatus.Cancelled:
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasCompletedCurrentProviderImport(VideoProduction production)
        {
            return production != null &&
                   !String.IsNullOrWhiteSpace(production.ProviderVideoId) &&
                   String.Equals(production.ProviderVideoImportCompletedProviderVideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase) &&
                   !String.IsNullOrWhiteSpace(production.FinalVideoMediaResource?.Id) &&
                   !String.IsNullOrWhiteSpace(production.ProviderVideoImportCompletedUtc?.ToString());
        }

        public async Task<InvokeResult<VideoProduction>> RefreshVideoProductionStatusAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            // Recover a completed import after an older provider poll or a later
            // invalid avatar-look submission changed the status. Never re-render here.
            // The input/output fingerprints still decide whether the recovered video
            // represents the CURRENT avatar and script.
            var recoverableLookFailure = production.Status?.Value == VideoProductionStatus.Failed &&
                production.ErrorMessage?.StartsWith("Video avatar look '", StringComparison.OrdinalIgnoreCase) == true &&
                production.ErrorMessage.EndsWith("could not be found or is inactive.", StringComparison.OrdinalIgnoreCase);
            if (HasCompletedCurrentProviderImport(production) &&
                (production.Status?.Value == VideoProductionStatus.ProviderCompleted || recoverableLookFailure))
            {
                production.SetStatus(VideoProductionStatus.ProviderVideoReady);
                production.ErrorMessage = null;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                SyncCurrentRun(production);
                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);
                return InvokeResult<VideoProduction>.Create(production);
            }

            if (IsImportOrLater(production))
            {
                return InvokeResult<VideoProduction>.Create(production);
            }

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

            // HeyGen may have responded while the video processor was completing the import.
            // Re-read before applying provider state so an older request cannot undo that work.
            var latestProduction = await _repo.GetVideoProductionAsync(production.Id);
            if (!String.Equals(latestProduction?.ProviderVideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<VideoProduction>.Create(latestProduction);
            }

            production = latestProduction;
            if (HasCompletedCurrentProviderImport(production) &&
                production.Status?.Value == VideoProductionStatus.ProviderCompleted)
            {
                production.SetStatus(VideoProductionStatus.ProviderVideoReady);
                SyncCurrentRun(production);
                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);
                return InvokeResult<VideoProduction>.Create(production);
            }

            if (IsImportOrLater(production))
            {
                return InvokeResult<VideoProduction>.Create(production);
            }

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
                    await ApplyActualVideoGenerationCostAsync(production);
                    break;

                case "failed":
                    production.SetStatus(VideoProductionStatus.Failed);
                    production.ErrorMessage = ResolveVideoStatusErrorMessage(providerStatus);
                    break;

                default:
                    production.ErrorMessage = $"HeyGen returned the unrecognized video status '{providerStatus.Status}'.";
                    break;
            }

            SyncCurrentRun(production);

            await _repo.UpdateVideoProductionAsync(production);
            await PublishVideoProductionUpdatedAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        private async Task ApplyActualVideoGenerationCostAsync(VideoProduction production)
        {
            if (!production.ActualDurationSeconds.HasValue || production.ActualDurationSeconds.Value <= 0)
                return;

            var run = GetCurrentRun(production);
            if (run == null)
            {
                EnsureRuns(production);
                run = new VideoProductionRun
                {
                    ProviderVideoId = production.ProviderVideoId,
                    Status = production.Status,
                    InputSha256 = production.ExecutionInputSha256 ?? production.CurrentInputSha256,
                    SubmittedUtc = production.SubmittedUtc
                };
                production.Runs.Insert(0, run);
            }

            run.DurationSeconds = production.ActualDurationSeconds;
            run.ProviderVideoUrl = production.ProviderVideoUrl;
            run.ProviderThumbnailUrl = production.ProviderThumbnailUrl;
            run.ProviderCaptionUrl = production.ProviderCaptionUrl;
            run.Status = production.Status;
            run.CompletedUtc = production.CompletedUtc;
            run.LastStatusCheckUtc = production.LastStatusCheckUtc;
            run.ErrorMessage = production.ErrorMessage;

            await ApplyRunVideoGenerationCostAsync(production, run);
            RecalculateProductionCosts(production);
        }

        private async Task ApplyRunVideoGenerationCostAsync(VideoProduction production, VideoProductionRun run)
        {
            if (!run.DurationSeconds.HasValue || run.DurationSeconds.Value <= 0)
            {
                if (run.ProviderChargeUsd.HasValue)
                {
                    run.ActualVideoGenerationCost = run.ProviderChargeUsd;
                    run.CostSource = "HeyGenRequestLogUsd";
                    if (String.Equals(production.ProviderVideoId, run.ProviderVideoId, StringComparison.OrdinalIgnoreCase))
                        production.ActualVideoGenerationCost = run.ActualVideoGenerationCost;
                }
                return;
            }
            var quality = run.QualityAtSubmission?.Value ?? production.Quality?.Value ?? VideoProductionQuality.Standard;
            var estimatedCost = EstimateCost(run.DurationSeconds.Value,
                quality == VideoProductionQuality.Standard ? 1.0m / 60.0m : 3.0m / 60.0m);
            run.ActualVideoGenerationCost = run.ProviderChargeUsd ?? estimatedCost;
            run.CostSource = run.ProviderChargeUsd.HasValue ? "HeyGenRequestLogUsd" : "DurationRateEstimate";
            if (String.Equals(production.ProviderVideoId, run.ProviderVideoId, StringComparison.OrdinalIgnoreCase))
                production.ActualVideoGenerationCost = run.ActualVideoGenerationCost;
            production.CostCurrency = String.IsNullOrWhiteSpace(production.CostCurrency) ? "USD" : production.CostCurrency;
            production.CostModelVersion = String.IsNullOrWhiteSpace(production.CostModelVersion) ? "heygen-standard-premium-2026-07" : production.CostModelVersion;

            // New renders have a stable business identity. Old unverified records are
            // reconciled for cost only to avoid double invoicing historical work.
            if (run.BillingRequired && String.IsNullOrWhiteSpace(run.BillingEventId))
            {
                var billingEventType = quality == VideoProductionQuality.Standard
                    ? BillingEventType.VideoGenerationStandard
                    : BillingEventType.VideoGenerationPremium;
                var key = $"heygen-video-generation:{production.OwnerOrganization?.Id}:{production.Id}:{run.ProviderVideoId}".ToLowerInvariant();
                var duration = (double)run.DurationSeconds.Value;
                var providerChargeNote = run.ProviderChargeAmount.HasValue
                    ? $", HeyGen charge: {run.ProviderChargeAmount.Value} {run.ProviderChargeUnit}"
                    : ", HeyGen charge: pending";
                var billingEventId = await _billingEventRecorder.RecordUsageAsync(
                    billingEventType, duration,
                    $"HeyGen video generation {production.Name}, provider video: {run.ProviderVideoId}, duration: {duration} seconds{providerChargeNote}",
                    production.OwnerOrganization, production.LastUpdatedBy, key);
                run.BillingEventId = billingEventId.ToString();
                production.VideoGenerationBillingEventId = run.BillingEventId;
            }

        }

        /// <summary>
        /// Reconcile all retakes independently. Request-log charges are provider evidence;
        /// duration-derived dollar values remain clearly marked estimates when HeyGen
        /// returns only credits/minutes. Legacy runs are never auto-invoiced.
        /// </summary>
        public async Task<InvokeResult<VideoProduction>> ReconcileVideoProductionCostsAsync(
            string id, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            var initial = await GetVideoProductionAsync(id, org, user);
            if (!String.Equals(initial.OwnerOrganization?.Id, org?.Id, StringComparison.OrdinalIgnoreCase))
                return InvokeResult<VideoProduction>.FromError("Only the owning organization can reconcile video production charges.");

            var lockKey = $"media-services:heygen:charge-reconcile:{id}".ToLowerInvariant();
            var lockToken = Guid.NewGuid().ToString("N");
            if (!await _cacheProvider.AttemptAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(10)))
                return InvokeResult<VideoProduction>.FromError("Video charge reconciliation is already in progress.");

            try
            {
                var production = await _repo.GetVideoProductionAsync(id);
                EnsureRuns(production);
                // Older productions predate run tracking. Preserve their latest render
                // and its existing billing reference rather than silently skipping it.
                if (!String.IsNullOrWhiteSpace(production.ProviderVideoId) &&
                    !production.Runs.Any(run => String.Equals(run?.ProviderVideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase)))
                {
                    production.Runs.Insert(0, new VideoProductionRun
                    {
                        ProviderVideoId = production.ProviderVideoId,
                        Status = production.Status,
                        SubmittedUtc = production.SubmittedUtc,
                        CompletedUtc = production.CompletedUtc,
                        DurationSeconds = production.ActualDurationSeconds,
                        ActualVideoGenerationCost = production.ActualVideoGenerationCost,
                        BillingEventId = production.VideoGenerationBillingEventId,
                        QualityAtSubmission = production.Quality,
                        BillingRequired = false
                    });
                }

                foreach (var run in production.Runs.Where(item => item != null && !String.IsNullOrWhiteSpace(item.ProviderVideoId)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var submittedUtc = run.SubmittedUtc;
                    if (String.IsNullOrWhiteSpace(submittedUtc) &&
                        String.Equals(run.ProviderVideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase))
                        submittedUtc = production.SubmittedUtc;

                    try
                    {
                        var chargeResult = await _heyGenVideoService.GetVideoChargeAsync(run.ProviderVideoId, submittedUtc, cancellationToken);
                        run.ProviderChargeCheckedUtc = UtcTimestamp.Now.ToString();
                        if (chargeResult.Successful && chargeResult.Result?.Found == true)
                        {
                            var charge = chargeResult.Result;
                            run.ProviderRequestId = String.IsNullOrWhiteSpace(run.ProviderRequestId) ? charge.RequestId : run.ProviderRequestId;
                            run.ProviderChargeRecorded = charge.ChargeRecorded;
                            run.ProviderChargeRefunded = charge.Refunded;
                            if (charge.ChargeRecorded)
                            {
                                run.ProviderChargeAmount = charge.Amount;
                                run.ProviderChargeUnit = charge.Unit;
                                run.ProviderChargeUsd = charge.AmountUsd;
                            }

                            if (String.Equals(charge.JobStatus, "completed", StringComparison.OrdinalIgnoreCase) &&
                                (run.Status == null || run.Status.Value == VideoProductionStatus.Submitted ||
                                 run.Status.Value == VideoProductionStatus.Rendering))
                                run.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderCompleted);
                        }
                        else if (!chargeResult.Successful)
                        {
                            _logger.AddCustomEvent(LogLevel.Warning, this.Tag(),
                                $"HeyGen request-log lookup failed for provider video '{run.ProviderVideoId}': {chargeResult.Errors?.FirstOrDefault()?.Message}");
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        _logger.AddException(this.Tag(), ex,
                            new KeyValuePair<string, string>("ProviderVideoId", run.ProviderVideoId));
                    }

                    var isComplete = run.Status?.Value == VideoProductionStatus.ProviderCompleted ||
                        run.Status?.Value == VideoProductionStatus.ImportingProviderVideo ||
                        run.Status?.Value == VideoProductionStatus.ProviderVideoReady ||
                        run.Status?.Value == VideoProductionStatus.ImportingToVimeo ||
                        run.Status?.Value == VideoProductionStatus.ProcessingAtVimeo ||
                        run.Status?.Value == VideoProductionStatus.UpdatingEntity ||
                        run.Status?.Value == VideoProductionStatus.Completed;

                    if (!isComplete || !run.DurationSeconds.HasValue || run.DurationSeconds.Value <= 0)
                    {
                        try
                        {
                            var detailsResult = await _heyGenVideoService.GetVideoAsync(run.ProviderVideoId, cancellationToken);
                            if (detailsResult.Successful && detailsResult.Result != null)
                            {
                                var details = detailsResult.Result;
                                if (details.Duration.HasValue && details.Duration.Value > 0)
                                    run.DurationSeconds = Convert.ToInt32(Math.Ceiling(details.Duration.Value));
                                if (String.Equals(details.Status, "completed", StringComparison.OrdinalIgnoreCase))
                                {
                                    isComplete = true;
                                    if (run.Status == null || run.Status.Value == VideoProductionStatus.Submitted ||
                                        run.Status.Value == VideoProductionStatus.Rendering)
                                        run.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderCompleted);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                        catch (Exception ex)
                        {
                            _logger.AddException(this.Tag(), ex,
                                new KeyValuePair<string, string>("ProviderVideoId", run.ProviderVideoId));
                        }
                    }

                    if (isComplete || run.ProviderChargeUsd.HasValue)
                    {
                        await ApplyRunVideoGenerationCostAsync(production, run);
                    }
                }

                RecalculateProductionCosts(production);
                await _repo.UpdateVideoProductionAsync(production);
                await PublishVideoProductionUpdatedAsync(production);
                return InvokeResult<VideoProduction>.Create(production);
            }
            finally
            {
                await _cacheProvider.ReleaseLockAsync(lockKey, lockToken);
            }
        }

        /// <summary>Operator-triggered best-effort backfill. Missing old charges do not stop the batch.</summary>
        public async Task<InvokeResult<int>> QueueHistoricalVideoCostBackfillAsync(EntityHeader org, EntityHeader user)
        {
            var productions = (await _repo.GetFullVideoProductionsForOrgAsync(org.Id))
                .Where(production => String.Equals(production.OwnerOrganization?.Id, org.Id, StringComparison.OrdinalIgnoreCase)
                    && (production.Runs?.Any(run => !String.IsNullOrWhiteSpace(run?.ProviderVideoId)) == true ||
                        !String.IsNullOrWhiteSpace(production.ProviderVideoId)))
                .ToList();

            var queue = BackgroundServiceTaskQueueProvider.Instance;
            if (queue == null)
                return InvokeResult<int>.FromError("Background task queue is unavailable.");

            var orgSnapshot = EntityHeader.Create(org.Id, org.Text);
            var userSnapshot = EntityHeader.Create(user.Id, user.Text);
            var queued = queue.TryQueueBackgroundWorkItem(async cancellationToken =>
            {
                foreach (var candidate in productions)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var result = await ReconcileVideoProductionCostsAsync(candidate.Id, orgSnapshot, userSnapshot, cancellationToken);
                        if (!result.Successful)
                            _logger.AddCustomEvent(LogLevel.Warning, this.Tag(),
                                $"Historical HeyGen backfill for '{candidate.Id}' failed: {result.Errors?.FirstOrDefault()?.Message}");
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        _logger.AddException(this.Tag(), ex, new KeyValuePair<string, string>("ProductionId", candidate.Id));
                    }
                    await Task.Delay(250, cancellationToken);
                }
            });

            return queued
                ? InvokeResult<int>.Create(productions.Count)
                : InvokeResult<int>.FromError("Could not enqueue the historical video cost backfill.");
        }

        private static void EnsureRuns(VideoProduction production)
        {
            if (production.Runs == null)
            {
                production.Runs = new List<VideoProductionRun>();
            }
        }

        private static VideoProductionRun GetCurrentRun(VideoProduction production)
        {
            EnsureRuns(production);

            if (!String.IsNullOrWhiteSpace(production.ProviderVideoId))
            {
                var providerRun = production.Runs.FirstOrDefault(run => run != null && String.Equals(run.ProviderVideoId, production.ProviderVideoId, StringComparison.OrdinalIgnoreCase));
                if (providerRun != null)
                {
                    return providerRun;
                }
            }

            return production.Runs.FirstOrDefault(run => run != null);
        }

        private static void SyncCurrentRun(VideoProduction production)
        {
            var run = GetCurrentRun(production);
            if (run == null)
            {
                return;
            }

            run.ProviderVideoId = production.ProviderVideoId;
            run.Status = production.Status;
            run.SubmittedUtc = production.SubmittedUtc;
            run.CompletedUtc = production.CompletedUtc;
            run.LastStatusCheckUtc = production.LastStatusCheckUtc;
            run.DurationSeconds = production.ActualDurationSeconds;
            run.ProviderVideoUrl = production.ProviderVideoUrl;
            run.ProviderThumbnailUrl = production.ProviderThumbnailUrl;
            run.ProviderCaptionUrl = production.ProviderCaptionUrl;
            run.ErrorMessage = production.ErrorMessage;

            if (production.FinalVideoMediaResource != null)
            {
                run.OutputMediaResource = production.FinalVideoMediaResource;
            }
        }

        private static void RecalculateProductionCosts(VideoProduction production)
        {
            EnsureRuns(production);
            var runs = production.Runs.Where(run => run != null).ToList();
            production.TotalVideoGenerationCost = runs.Sum(run => run.ActualVideoGenerationCost ?? 0m);
            var creditRuns = runs.Where(run => String.Equals(run.ProviderChargeUnit, "credits", StringComparison.OrdinalIgnoreCase) && run.ProviderChargeAmount.HasValue).ToList();
            var minuteRuns = runs.Where(run => String.Equals(run.ProviderChargeUnit, "minutes", StringComparison.OrdinalIgnoreCase) && run.ProviderChargeAmount.HasValue).ToList();
            var usdRuns = runs.Where(run => run.ProviderChargeUsd.HasValue).ToList();
            production.TotalHeyGenCredits = creditRuns.Count > 0 ? creditRuns.Sum(run => run.ProviderChargeAmount ?? 0m) : (decimal?)null;
            production.TotalHeyGenMinutes = minuteRuns.Count > 0 ? minuteRuns.Sum(run => run.ProviderChargeAmount ?? 0m) : (decimal?)null;
            production.TotalProviderReportedVideoCostUsd = usdRuns.Count > 0 ? usdRuns.Sum(run => run.ProviderChargeUsd ?? 0m) : (decimal?)null;
            production.VideoGenerationCount = runs.Count(run => run.ActualVideoGenerationCost.HasValue);
            production.ActualTotalCost =
                (production.ActualPreviewAudioCost ?? 0) +
                (production.ActualAvatarCreationCost ?? 0) +
                (production.ActualVideoGenerationCost ?? 0);

            production.TotalProductionCost =
                (production.TotalPreviewAudioCost ?? 0) +
                (production.ActualAvatarCreationCost ?? 0) +
                (production.TotalVideoGenerationCost ?? 0);
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
            var settings = production.Settings ?? new VideoProductionSettings();
            var generateTransparentPresenter = settings.OutputMode?.Value == VideoProductionOutputMode.TransparentPresenter;

            return new HeyGenVideoRequest
            {
                Type = "avatar",
                AvatarId = production.ProviderAvatarId,
                Script = BuildHeyGenScript(production.Script),
                VoiceId = production.VoiceId,
                Title = production.VideoName,
                CallbackId = production.Id,
                Resolution = ResolveHeyGenResolution(settings.Resolution),
                AspectRatio = ResolveHeyGenAspectRatio(settings.AspectRatio),
                Fit = ResolveHeyGenFit(settings.Fit),
                RemoveBackground = true,
                OutputFormat = generateTransparentPresenter ? "webm" : "mp4",
                Background = generateTransparentPresenter ? null : new HeyGenBackground { Type = "image", AssetId = production.ProviderBackgroundAssetId },
                Caption = settings.BurnInCaptions ? new HeyGenCaptionSettings { FileFormat = "srt", Style = String.IsNullOrWhiteSpace(settings.CaptionStyle) ? "default" : settings.CaptionStyle } : null,
                MotionPrompt = settings.MotionPrompt,
                Expressiveness = production.Engine?.Value == VideoProductionEngine.AvatarIV ? ResolveHeyGenExpressiveness(settings.Expressiveness) : null,
                Engine = new HeyGenEngineSettings { Type = ResolveHeyGenEngine(production.Engine) },
                VoiceSettings = new HeyGenVoiceSettings
                {
                    Speed = settings.VoiceSpeed,
                    Pitch = settings.VoicePitch,
                    Volume = settings.VoiceVolume,
                    Locale = production.Locale
                }
            };
        }

        private static string BuildHeyGenScript(string script)
        {
            if (String.IsNullOrWhiteSpace(script)) return script;

            var matches = HeyGenPauseMarkerRegex.Matches(script);
            if (matches.Count == 0) return script;

            var result = new StringBuilder("");
            var sourceIndex = 0;
            var pauseCount = 0;

            foreach (Match match in matches)
            {
                if (!Decimal.TryParse(match.Groups["seconds"].Value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var seconds) || seconds <= 0 || seconds > 10)
                {
                    continue;
                }

                result.Append(EscapeSsmlText(script.Substring(sourceIndex, match.Index - sourceIndex)));
                result.Append($"<break time=\"{seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}s\"/>");
                sourceIndex = match.Index + match.Length;
                pauseCount++;
            }

            if (pauseCount == 0) return script;

            result.Append(EscapeSsmlText(script.Substring(sourceIndex)));
      
            return result.ToString();
        }

        private static string EscapeSsmlText(string value)
        {
            return (value ?? String.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static string ResolveHeyGenEngine(EntityHeader<VideoProductionEngine> engine)
        {
            switch (engine?.Value ?? VideoProductionEngine.AvatarIV)
            {
                case VideoProductionEngine.AvatarIII:
                    return "avatar_iii";
                case VideoProductionEngine.AvatarV:
                    return "avatar_v";
                default:
                    return "avatar_iv";
            }
        }

        private static string ResolveHeyGenResolution(EntityHeader<VideoProductionResolution> resolution)
        {
            switch (resolution?.Value ?? VideoProductionResolution.FullHD1080)
            {
                case VideoProductionResolution.HD720:
                    return "720p";
                case VideoProductionResolution.UHD4K:
                    return "4k";
                default:
                    return "1080p";
            }
        }

        private static string ResolveHeyGenAspectRatio(EntityHeader<VideoProductionAspectRatio> aspectRatio)
        {
            switch (aspectRatio?.Value ?? VideoProductionAspectRatio.Landscape16x9)
            {
                case VideoProductionAspectRatio.Portrait9x16:
                    return "9:16";
                case VideoProductionAspectRatio.Portrait4x5:
                    return "4:5";
                case VideoProductionAspectRatio.Landscape5x4:
                    return "5:4";
                case VideoProductionAspectRatio.Square1x1:
                    return "1:1";
                case VideoProductionAspectRatio.Auto:
                    return "auto";
                default:
                    return "16:9";
            }
        }

        private static string ResolveHeyGenFit(EntityHeader<VideoProductionFit> fit)
        {
            switch (fit?.Value ?? VideoProductionFit.Automatic)
            {
                case VideoProductionFit.Contain:
                    return "contain";
                case VideoProductionFit.Cover:
                    return "cover";
                default:
                    return null;
            }
        }

        private static string ResolveHeyGenExpressiveness(EntityHeader<VideoProductionExpressiveness> expressiveness)
        {
            switch (expressiveness?.Value ?? VideoProductionExpressiveness.Low)
            {
                case VideoProductionExpressiveness.Medium:
                    return "medium";
                case VideoProductionExpressiveness.High:
                    return "high";
                default:
                    return "low";
            }
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

            if (production.Quality == null)
            {
                production.Quality = EntityHeader<VideoProductionQuality>.Create(VideoProductionQuality.Standard);
            }

            if (production.Engine == null)
            {
                production.Engine = EntityHeader<VideoProductionEngine>.Create(VideoProductionEngine.AvatarIV);
            }

            if (production.Settings == null)
            {
                production.Settings = new VideoProductionSettings();
            }

            if (production.Settings.OutputMode == null)
            {
                var hasBackground = production.BackgroundMediaResource != null && !String.IsNullOrWhiteSpace(production.BackgroundMediaResource.Id);
                var outputMode = production.Settings.GenerateTransparentPresenter || !hasBackground
                    ? VideoProductionOutputMode.TransparentPresenter
                    : VideoProductionOutputMode.CompositedBackground;

                production.Settings.OutputMode = EntityHeader<VideoProductionOutputMode>.Create(outputMode);
            }

            production.Settings.RemoveBackground = true;

            if (production.Settings.OutputMode.Value == VideoProductionOutputMode.TransparentPresenter)
            {
                production.Settings.GenerateTransparentPresenter = true;
                production.BackgroundMediaResource = null;
                production.ProviderBackgroundAssetId = null;
            }
            else
            {
                production.Settings.GenerateTransparentPresenter = false;
            }

            if (production.Settings.Resolution == null)
            {
                production.Settings.Resolution = EntityHeader<VideoProductionResolution>.Create(VideoProductionResolution.FullHD1080);
            }

            if (production.Settings.AspectRatio == null)
            {
                production.Settings.AspectRatio = EntityHeader<VideoProductionAspectRatio>.Create(VideoProductionAspectRatio.Landscape16x9);
            }

            if (production.Settings.Fit == null)
            {
                production.Settings.Fit = EntityHeader<VideoProductionFit>.Create(VideoProductionFit.Automatic);
            }

            if (production.Engine.Value == VideoProductionEngine.AvatarIV && production.Settings.Expressiveness == null)
            {
                production.Settings.Expressiveness = EntityHeader<VideoProductionExpressiveness>.Create(VideoProductionExpressiveness.Low);
            }

            if (production.Engine.Value != VideoProductionEngine.AvatarIV)
            {
                production.Settings.Expressiveness = null;
            }

            production.Settings.SpecialInstructions = production.Settings.SpecialInstructions?.Trim();
            production.Settings.MotionPrompt = production.Settings.MotionPrompt?.Trim();
            production.Settings.CaptionStyle = String.IsNullOrWhiteSpace(production.Settings.CaptionStyle) ? "default" : production.Settings.CaptionStyle.Trim();

            if (production.Status == null)
            {
                production.SetStatus(VideoProductionStatus.Draft);
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

        private static InvokeResult ValidateOutputMode(VideoProduction production)
        {
            if (production?.Settings == null)
            {
                return InvokeResult.FromError("Video production settings are required.");
            }

            if (production.Settings.OutputMode == null)
            {
                return InvokeResult.FromError("Video production output mode is required.");
            }

            if (!production.Settings.RemoveBackground)
            {
                return InvokeResult.FromError("Avatar background removal is required for supported video production output modes.");
            }

            switch (production.Settings.OutputMode.Value)
            {
                case VideoProductionOutputMode.TransparentPresenter:
                    if (!production.Settings.GenerateTransparentPresenter)
                    {
                        return InvokeResult.FromError("Transparent presenter output must generate a transparent presenter video.");
                    }

                    if (production.BackgroundMediaResource != null && !String.IsNullOrWhiteSpace(production.BackgroundMediaResource.Id))
                    {
                        return InvokeResult.FromError("Transparent presenter output cannot include a background Media Resource.");
                    }

                    break;

                case VideoProductionOutputMode.CompositedBackground:
                    if (production.Settings.GenerateTransparentPresenter)
                    {
                        return InvokeResult.FromError("Video-with-background output cannot generate a transparent presenter video.");
                    }

                    if (production.BackgroundMediaResource == null || String.IsNullOrWhiteSpace(production.BackgroundMediaResource.Id))
                    {
                        return InvokeResult.FromError("A background Media Resource is required for video-with-background output.");
                    }

                    break;

                default:
                    return InvokeResult.FromError($"Video production output mode '{production.Settings.OutputMode.Id}' is not supported.");
            }

            return InvokeResult.Success;
        }

        private async Task PublishVideoProductionUpdatedAsync(VideoProduction production)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, production.Id, "video-production-updated", production);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, production.OwnerOrganization.Id, "video-production-updated", production);

            if (!String.IsNullOrWhiteSpace(production.NotificationRunId))
            {
                await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, production.NotificationRunId, "video-production-updated", production);
            }
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