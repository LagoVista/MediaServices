using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoCompositionBlockTemplateManager : ManagerBase, IVideoCompositionBlockTemplateManager
    {
        private readonly IVideoCompositionBlockTemplateRepo _repo;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoCompositionBlockTemplateManager(IVideoCompositionBlockTemplateRepo repo, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult> AddVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template, EntityHeader org, EntityHeader user)
        {
            NormalizeTemplate(template);
            ValidationCheck(template, Actions.Create);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Create, user, org);
            await _repo.AddVideoCompositionBlockTemplateAsync(template);
            await PublishTemplateUpdatedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template, EntityHeader org, EntityHeader user)
        {
            NormalizeTemplate(template);
            ValidationCheck(template, Actions.Update);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Update, user, org);
            await _repo.UpdateVideoCompositionBlockTemplateAsync(template);
            await PublishTemplateUpdatedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateVideoCompositionBlockTemplateFromBlockAsync(string id, VideoCompositionBlock block, EntityHeader org, EntityHeader user)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));

            var template = await _repo.GetVideoCompositionBlockTemplateAsync(id);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Update, user, org);

            template.Block = CloneBlock(block);
            template.LastUpdatedBy = user;
            template.LastUpdatedDate = UtcTimestamp.Now;

            NormalizeTemplate(template);
            ValidationCheck(template, Actions.Update);
            await _repo.UpdateVideoCompositionBlockTemplateAsync(template);
            await PublishTemplateUpdatedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteVideoCompositionBlockTemplateAsync(string id, EntityHeader org, EntityHeader user)
        {
            var template = await _repo.GetVideoCompositionBlockTemplateAsync(id);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(template);

            template.IsDeleted = true;
            template.DeletedBy = user;
            template.DeletionDate = UtcTimestamp.Now;
            template.IsActive = false;
            template.LastUpdatedBy = user;
            template.LastUpdatedDate = UtcTimestamp.Now;

            await _repo.UpdateVideoCompositionBlockTemplateAsync(template);
            await PublishTemplateDeletedAsync(template);
            return InvokeResult.Success;
        }

        public async Task<VideoCompositionBlockTemplate> GetVideoCompositionBlockTemplateAsync(string id, EntityHeader org, EntityHeader user)
        {
            var template = await _repo.GetVideoCompositionBlockTemplateAsync(id);
            await AuthorizeAsync(template, AuthorizeResult.AuthorizeActions.Read, user, org);
            return template;
        }

        public async Task<ListResponse<VideoCompositionBlockTemplateSummary>> GetVideoCompositionBlockTemplatesForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(VideoCompositionBlockTemplate));
            return await _repo.GetVideoCompositionBlockTemplateSummariesForOrgAsync(org.Id, listRequest);
        }

        public Task<bool> QueryKeyInUseAsync(string key, EntityHeader org)
        {
            return _repo.QueryKeyInUseAsync(key, org.Id);
        }

        public Task<DetailResponse<VideoCompositionBlockTemplate>> CreateTemplateFromBlockAsync(CreateVideoCompositionBlockTemplateRequest request, EntityHeader org, EntityHeader user)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Block == null) throw new ArgumentException("A source block is required.", nameof(request));

            var now = UtcTimestamp.Now;

            var template = new VideoCompositionBlockTemplate
            {
                Name = request.Name?.Trim(),
                Description = request.Description?.Trim(),
                Category = request.Category,
                Key = CreateTemplateKey(request.Name),
                Block = CloneBlock(request.Block),
                IsActive = true,
                OwnerOrganization = org,
                CreatedBy = user,
                LastUpdatedBy = user,
                CreationDate = now,
                LastUpdatedDate = now,
            };

            NormalizeTemplate(template);
            return Task.FromResult(DetailResponse<VideoCompositionBlockTemplate>.Create(template));
        }

        public async Task<DetailResponse<VideoCompositionBlock>> CreateBlockFromTemplateAsync(string templateId, EntityHeader org, EntityHeader user)
        {
            var template = await GetVideoCompositionBlockTemplateAsync(templateId, org, user);

            if (template.IsDeleted == true)
            {
                throw new InvalidOperationException($"Video composition block template '{template.Name}' has been deleted.");
            }

            if (!template.IsActive)
            {
                throw new InvalidOperationException($"Video composition block template '{template.Name}' is inactive.");
            }

            var block = CloneBlock(template.Block);
            return DetailResponse<VideoCompositionBlock>.Create(block);
        }

        private static void NormalizeTemplate(VideoCompositionBlockTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            template.Name = template.Name?.Trim();
            template.Key = template.Key.Value.Trim().ToLowerInvariant();
            template.Description = template.Description?.Trim();
            template.Icon = String.IsNullOrWhiteSpace(template.Icon) ? "lago-icon://system/nuvos-semantic-icon/video-production-default" : template.Icon.Value;
            template.Block = template.Block ?? new VideoCompositionBlock();
            template.Block.Id = String.IsNullOrWhiteSpace(template.Block.Id) ? Guid.NewGuid().ToId().Value : template.Block.Id;
            template.Block.Key = String.IsNullOrWhiteSpace(template.Block.Key) ? "templateblock" : template.Block.Key.Trim().ToLowerInvariant();
            template.Block.SortOrder = 0;
            template.Block.CompositionLabels = template.Block.CompositionLabels ?? new List<VideoCompositionTextLabel>();
        }

        private static VideoCompositionBlock CloneBlock(VideoCompositionBlock source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var json = JsonConvert.SerializeObject(source);
            var block = JsonConvert.DeserializeObject<VideoCompositionBlock>(json) ?? throw new InvalidOperationException("Could not clone the video composition block.");

            block.Id = Guid.NewGuid().ToId();
            block.Key = "block" + Guid.NewGuid().ToId().Value.Substring(0, 6).ToLowerInvariant();
            block.SortOrder = 0;
            block.CompositionLabels = block.CompositionLabels ?? new List<VideoCompositionTextLabel>();

            foreach (var label in block.CompositionLabels)
            {
                label.Id = Guid.NewGuid().ToId();
            }

            return block;
        }

        private static string CreateTemplateKey(string name)
        {
            var source = String.IsNullOrWhiteSpace(name) ? "block-template" : name.Trim().ToLowerInvariant();
            var chars = new List<char>();
            var previousWasSeparator = false;

            foreach (var character in source)
            {
                if (Char.IsLetterOrDigit(character))
                {
                    chars.Add(character);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator && chars.Count > 0)
                {
                    chars.Add('-');
                    previousWasSeparator = true;
                }
            }

            var key = new string(chars.ToArray()).Trim('-');
            if (String.IsNullOrWhiteSpace(key) || !Char.IsLetter(key[0])) key = "template-" + key;
            if (key.Length < 3) key += "-template";
            if (key.Length > 54) key = key.Substring(0, 54).TrimEnd('-');
            return key + "-" + Guid.NewGuid().ToId().Value.Substring(0, 8).ToLowerInvariant();
        }

        private async Task PublishTemplateUpdatedAsync(VideoCompositionBlockTemplate template)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, template.Id, "video-composition-block-template-updated", template);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, template.OwnerOrganization.Id, "video-composition-block-template-updated", template);
        }

        private async Task PublishTemplateDeletedAsync(VideoCompositionBlockTemplate template)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, template.Id, "video-composition-block-template-deleted", template);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, template.OwnerOrganization.Id, "video-composition-block-template-deleted", template);
        }
    }
}
