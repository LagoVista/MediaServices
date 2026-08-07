using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoCompositionTemplateManager : ManagerBase, IVideoCompositionTemplateManager
    {
        private readonly IVideoCompositionTemplateRepo _repo;
        private readonly IVideoCompositionManager _compositionManager;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoCompositionTemplateManager(IVideoCompositionTemplateRepo repo, IVideoCompositionManager compositionManager, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _compositionManager = compositionManager ?? throw new ArgumentNullException(nameof(compositionManager));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult> AddVideoCompositionTemplateAsync(VideoCompositionTemplate template, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoCompositionTemplate(template);
            ValidationCheck(template, Actions.Create);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Create, user, org);
            await _repo.AddVideoCompositionTemplateAsync(template);
            await PublishVideoCompositionTemplateUpdatedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult<VideoCompositionTemplate>> CreateFromCompositionAsync(string compositionId, CreateVideoCompositionTemplateFromCompositionRequest request, EntityHeader org, EntityHeader user)
        {
            if (String.IsNullOrWhiteSpace(compositionId))
            {
                return InvokeResult<VideoCompositionTemplate>.FromError("Video composition id is required.");
            }

            if (request == null)
            {
                return InvokeResult<VideoCompositionTemplate>.FromError("Create video composition template request is required.");
            }

            if (String.IsNullOrWhiteSpace(request.Name))
            {
                return InvokeResult<VideoCompositionTemplate>.FromError("Video composition template name is required.");
            }

            var composition = await _compositionManager.GetVideoCompositionAsync(compositionId, org, user);
            if (composition == null)
            {
                return InvokeResult<VideoCompositionTemplate>.FromError($"Could not find video composition '{compositionId}'.");
            }

            var blocks = CloneBlocks(composition.Blocks);
            AssignBlockRoles(blocks);

            var contentBlock = blocks.FirstOrDefault(block => block.Role == VideoCompositionBlockRole.Content);
            if (contentBlock == null)
            {
                return InvokeResult<VideoCompositionTemplate>.FromError("The video composition does not contain a content video block.");
            }

            contentBlock.MediaResource = null;
            contentBlock.MediaResourceFileName = null;
            contentBlock.MediaResourceMimeType = null;
            contentBlock.ThumbnailUrl = null;

            var template = new VideoCompositionTemplate
            {
                Id = Guid.NewGuid().ToId(),
                Name = request.Name.Trim(),
                Key = String.IsNullOrWhiteSpace(request.Key)
                    ? "vct" + Guid.NewGuid().ToId().Value.ToLowerInvariant()
                    : request.Key.Trim().ToLowerInvariant(),
                Description = request.Description,
                Category = request.Category,
                OwnerOrganization = org,
                CreatedBy = user,
                CreationDate = UtcTimestamp.Now,
                LastUpdatedBy = user,
                LastUpdatedDate = UtcTimestamp.Now,
                Version = 1,
                IsActive = true,
                DefaultLocale = String.IsNullOrWhiteSpace(composition.DefaultLocale)
                    ? VideoComposition.DefaultLocaleCode
                    : composition.DefaultLocale,
                BackgroundMediaResource = composition.BackgroundMediaResource,
                BackgroundAudioMediaResource = composition.BackgroundAudioMediaResource,
                BackgroundAudioVolume = composition.BackgroundAudioVolume,
                BackgroundAudioFadeInSeconds = composition.BackgroundAudioFadeInSeconds,
                BackgroundAudioFadeOutSeconds = composition.BackgroundAudioFadeOutSeconds,
                LoopBackgroundAudio = composition.LoopBackgroundAudio,
                OutputMediaLibrary = composition.OutputMediaLibrary,
                Blocks = blocks
            };

            NormalizeVideoCompositionTemplate(template);
            ValidationCheck(template, Actions.Create);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Create, user, org);
            await _repo.AddVideoCompositionTemplateAsync(template);
            await PublishVideoCompositionTemplateUpdatedAsync(template);

            return InvokeResult<VideoCompositionTemplate>.Create(template);
        }

        public async Task<InvokeResult> UpdateVideoCompositionTemplateAsync(VideoCompositionTemplate template, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoCompositionTemplate(template);
            ValidationCheck(template, Actions.Update);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Update, user, org);
            await _repo.UpdateVideoCompositionTemplateAsync(template);
            await PublishVideoCompositionTemplateUpdatedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteVideoCompositionTemplateAsync(string id, EntityHeader org, EntityHeader user)
        {
            var template = await _repo.GetVideoCompositionTemplateAsync(id);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(template);
            await _repo.DeleteVideoCompositionTemplateAsync(id);
            await PublishVideoCompositionTemplateDeletedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<VideoCompositionTemplate> GetVideoCompositionTemplateAsync(string id, EntityHeader org, EntityHeader user)
        {
            var template = await _repo.GetVideoCompositionTemplateAsync(id);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Read, user, org);
            return template;
        }

        public async Task<ListResponse<VideoCompositionTemplateSummary>> GetVideoCompositionTemplatesForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(VideoCompositionTemplate));
            return await _repo.GetVideoCompositionTemplateSummariesForOrgAsync(org.Id, listRequest);
        }

        public Task<bool> QueryKeyInUseAsync(string key, EntityHeader org)
        {
            return _repo.QueryKeyInUseAsync(key, org.Id);
        }

        private static List<VideoCompositionBlock> CloneBlocks(List<VideoCompositionBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return new List<VideoCompositionBlock>();
            }

            return JsonConvert.DeserializeObject<List<VideoCompositionBlock>>(
                JsonConvert.SerializeObject(blocks)) ?? new List<VideoCompositionBlock>();
        }

        private static void AssignBlockRoles(List<VideoCompositionBlock> blocks)
        {
            var orderedBlocks = (blocks ?? new List<VideoCompositionBlock>())
                .OrderBy(block => block.SortOrder)
                .ToList();

            var contentBlock = orderedBlocks.FirstOrDefault(block => block.Role == VideoCompositionBlockRole.Content)
                ?? orderedBlocks.FirstOrDefault(block => block.Type == VideoCompositionBlockType.Video);

            if (contentBlock == null)
            {
                return;
            }

            contentBlock.Role = VideoCompositionBlockRole.Content;

            foreach (var block in orderedBlocks.Where(block => block != contentBlock && block.Role == VideoCompositionBlockRole.None))
            {
                block.Role = block.SortOrder < contentBlock.SortOrder
                    ? VideoCompositionBlockRole.Intro
                    : VideoCompositionBlockRole.CallToAction;
            }
        }

        private static void NormalizeVideoCompositionTemplate(VideoCompositionTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            if (template.Version < 1)
            {
                template.Version = 1;
            }

            if (String.IsNullOrWhiteSpace(template.DefaultLocale))
            {
                template.DefaultLocale = VideoComposition.DefaultLocaleCode;
            }

            template.Blocks = template.Blocks ?? new List<VideoCompositionBlock>();

            var orderedBlocks = template.Blocks.OrderBy(block => block.SortOrder).ToList();
            for (var index = 0; index < orderedBlocks.Count; index++)
            {
                var block = orderedBlocks[index];
                block.Id = String.IsNullOrWhiteSpace(block.Id) ? Guid.NewGuid().ToId().Value : block.Id;
                block.Key = String.IsNullOrWhiteSpace(block.Key) ? $"block{index + 1}" : block.Key.Trim().ToLowerInvariant();
                block.SortOrder = index;
                block.CompositionLabels = block.CompositionLabels ?? new List<VideoCompositionTextLabel>();
                block.OverlayImages = block.OverlayImages ?? new List<VideoCompositionBlockImage>();
            }

            template.Blocks = orderedBlocks;
        }

        private async Task PublishVideoCompositionTemplateUpdatedAsync(VideoCompositionTemplate template)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, template.Id, "video-composition-template-updated", template);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, template.OwnerOrganization.Id, "video-composition-template-updated", template);
        }

        private async Task PublishVideoCompositionTemplateDeletedAsync(VideoCompositionTemplate template)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, template.Id, "video-composition-template-deleted", template);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, template.OwnerOrganization.Id, "video-composition-template-deleted", template);
        }
    }
}
