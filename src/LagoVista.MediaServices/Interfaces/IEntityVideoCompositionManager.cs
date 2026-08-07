using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IEntityVideoCompositionManager
    {
        Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest, CancellationToken cancellationToken = default);

        Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);

        Task<InvokeResult<VideoComposition>> CreateCompositionAsync(CreateEntityVideoCompositionRequest request, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);

        Task<InvokeResult<VideoComposition>> SyncCompositionAsync(string entityType, string entityId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);

        Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
    }
}
