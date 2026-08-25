using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IEntityVideoCompositionRepo
    {
        Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, string orgId, ListRequest listRequest, CancellationToken cancellationToken = default);

        Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, string orgId, CancellationToken cancellationToken = default);

        Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, EntityHeader org, string name, string key, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader user, CancellationToken cancellationToken = default);
    }
}
