using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoCompositionTemplateManager : ManagerBase, IVideoCompositionTemplateManager
    {
        private readonly IVideoCompositionTemplateRepo _repo;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoCompositionTemplateManager(IVideoCompositionTemplateRepo repo, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
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
