using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoCompositionManager : ManagerBase, IVideoCompositionManager
    {
        private readonly IVideoCompositionRepo _repo;
        private readonly INotificationPublisher _notificationPublisher;

        public VideoCompositionManager(IVideoCompositionRepo repo, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult> AddVideoCompositionAsync(VideoComposition composition, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoComposition(composition);

            ValidationCheck(composition, Actions.Create);
            await AuthorizeAsync(composition, AuthorizeResult.AuthorizeActions.Create, user, org);

            await _repo.AddVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateVideoCompositionAsync(VideoComposition composition, EntityHeader org, EntityHeader user)
        {
            NormalizeVideoComposition(composition);

            ValidationCheck(composition, Actions.Update);
            await AuthorizeAsync(composition, AuthorizeResult.AuthorizeActions.Update, user, org);

            await _repo.UpdateVideoCompositionAsync(composition);
            await PublishVideoCompositionUpdatedAsync(composition);

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteVideoCompositionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var composition = await _repo.GetVideoCompositionAsync(id);

            await AuthorizeAsync(composition, AuthorizeResult.AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(composition);
            await _repo.DeleteVideoCompositionAsync(id);
            await PublishVideoCompositionDeletedAsync(composition);

            return InvokeResult.Success;
        }

        public async Task<VideoComposition> GetVideoCompositionAsync(string id, EntityHeader org, EntityHeader user)
        {
            var composition = await _repo.GetVideoCompositionAsync(id);
            await AuthorizeAsync(composition, AuthorizeResult.AuthorizeActions.Read, user, org);
            return composition;
        }

        public async Task<ListResponse<VideoCompositionSummary>> GetVideoCompositionsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(VideoComposition));
            return await _repo.GetVideoCompositionSummariesForOrgAsync(org.Id, listRequest);
        }

        public Task<bool> QueryKeyInUseAsync(string key, EntityHeader org)
        {
            return _repo.QueryKeyInUseAsync(key, org.Id);
        }

        private static void NormalizeVideoComposition(VideoComposition composition)
        {
            if (composition == null)
            {
                return;
            }

            if (composition.Status == null)
            {
                composition.Status = EntityHeader<VideoCompositionStatus>.Create(VideoCompositionStatus.Draft);
            }

            composition.Blocks = composition.Blocks ?? new System.Collections.Generic.List<VideoCompositionBlock>();

            var orderedBlocks = composition.Blocks.OrderBy(block => block.SortOrder).ToList();
            for (var index = 0; index < orderedBlocks.Count; index++)
            {
                var block = orderedBlocks[index];
                block.Id = String.IsNullOrWhiteSpace(block.Id) ? Guid.NewGuid().ToId().Value : block.Id;
                block.Key = String.IsNullOrWhiteSpace(block.Key) ? $"block-{index + 1}" : block.Key.Trim().ToLowerInvariant();
                block.SortOrder = index;
                block.Labels = block.Labels ?? new System.Collections.Generic.List<VideoCompositionTextLabel>();
            }

            composition.Blocks = orderedBlocks;
            composition.AssemblyState = composition.AssemblyState ?? new VideoCompositionAssemblyState();
        }

        private async Task PublishVideoCompositionUpdatedAsync(VideoComposition composition)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, composition.Id, "video-composition-updated", composition);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, composition.OwnerOrganization.Id, "video-composition-updated", composition);
        }

        private async Task PublishVideoCompositionDeletedAsync(VideoComposition composition)
        {
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Entity, composition.Id, "video-composition-deleted", composition);
            await _notificationPublisher.PublishAsync(Targets.WebSocket, Channels.Org, composition.OwnerOrganization.Id, "video-composition-deleted", composition);
        }
    }
}
