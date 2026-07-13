using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoProductionManager : ManagerBase, IVideoProductionManager
    {
        private readonly IVideoAvatarManager _videoAvatarManager;
        private readonly IVideoProductionRepo _repo;
        private readonly IHeyGenVideoService _heyGenVideoService;

        public VideoProductionManager(IVideoProductionRepo repo, IVideoAvatarManager videoAvatarManager, IHeyGenVideoService heyGenVideoService, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new NullReferenceException(nameof(repo));
            _videoAvatarManager = videoAvatarManager ?? throw new NullReferenceException(nameof(videoAvatarManager));
            _heyGenVideoService = heyGenVideoService ?? throw new NullReferenceException(nameof(heyGenVideoService));
        }

        public async Task<InvokeResult> AddVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoProduction(production);

            ValidationCheck(production, Actions.Create);
            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Create, user, org);

            await _repo.AddVideoProductionAsync(production);

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoProduction(production);

            ValidationCheck(production, Actions.Update);
            await AuthorizeAsync(production, AuthorizeResult.AuthorizeActions.Update, user, org);

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult.Success;
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

        public async Task<InvokeResult<VideoProduction>> EstimateVideoProductionCostAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            production.EstimatedDurationSeconds = EstimateDurationSeconds(production.Script);
            production.EstimatedPreviewAudioCost = EstimateCost(production.EstimatedDurationSeconds.Value, 0.000667m);
            production.EstimatedVideoGenerationCost = EstimateCost(production.EstimatedDurationSeconds.Value, 0.05m);
            production.EstimatedTotalCost = (production.EstimatedPreviewAudioCost ?? 0) + (production.EstimatedAvatarCreationCost ?? 0) + (production.EstimatedVideoGenerationCost ?? 0);
            production.CostCurrency = String.IsNullOrWhiteSpace(production.CostCurrency) ? "USD" : production.CostCurrency;
            production.CostModelVersion = String.IsNullOrWhiteSpace(production.CostModelVersion) ? "heygen-api-2026-07" : production.CostModelVersion;

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

            production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.GeneratingPreviewAudio);
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
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
                production.ErrorMessage = avatarResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return avatarResult.ToInvokeResult<VideoProduction>();
            }

            var avatarStatusResult = await _videoAvatarManager.RefreshProviderAvatarStatusAsync(production.VideoAvatar.Id, org, user);
            if (!avatarStatusResult.Successful)
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
                production.ErrorMessage = avatarStatusResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return avatarStatusResult.ToInvokeResult<VideoProduction>();
            }

            if (avatarStatusResult.Result.Status.Value != VideoAvatarStatus.Ready)
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.WaitingForAvatar);
                production.ErrorMessage = "Video avatar is not ready.";
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return InvokeResult<VideoProduction>.FromError("Video avatar is not ready.");
            }

            ApplyAvatarToProduction(production, avatarStatusResult.Result);

            var submitRequest = BuildHeyGenRequest(production);
            var submitResult = await _heyGenVideoService.SubmitVideoAsync(submitRequest);
            if (!submitResult.Successful)
            {
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Failed);
                production.ErrorMessage = submitResult.Errors[0].Message;
                production.LastStatusCheckUtc = UtcTimestamp.Now;
                await _repo.UpdateVideoProductionAsync(production);
                return submitResult.ToInvokeResult<VideoProduction>();
            }

            production.ProviderVideoId = submitResult.Result.VideoId;
            production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Submitted);
            production.SubmittedUtc = UtcTimestamp.Now;
            production.LastStatusCheckUtc = production.SubmittedUtc;
            production.ErrorMessage = null;

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
        }

        public async Task<InvokeResult<VideoProduction>> RefreshVideoProductionStatusAsync(string id, EntityHeader org, EntityHeader user)
        {
            var production = await GetVideoProductionAsync(id, org, user);

            if (String.IsNullOrWhiteSpace(production.ProviderVideoId))
            {
                return InvokeResult<VideoProduction>.FromError("Provider video ID has not been created.");
            }

            production.LastStatusCheckUtc = UtcTimestamp.Now;

            await _repo.UpdateVideoProductionAsync(production);

            return InvokeResult<VideoProduction>.Create(production);
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

        private static void ApplyAvatarToProduction(VideoProduction production, VideoAvatar avatar)
        {
            production.ProviderAvatarId = avatar.ProviderAvatarId;

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
                production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Draft);
            }

            if (String.IsNullOrWhiteSpace(production.CostCurrency))
            {
                production.CostCurrency = "USD";
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