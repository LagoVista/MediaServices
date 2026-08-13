using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IEntityVideoProductionOrchestrator
    {
        Task<InvokeResult<EntityVideoProductionWorkspace>> PrepareAsync(PrepareEntityVideoProductionRequest request, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);

        Task<InvokeResult<EntityVideoProductionWorkspace>> CreateCompositionAsync(CreateEntityVideoCompositionFromProductionRequest request, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
    }

    public interface IEntityVideoCompositionContinuation
    {
        Task<InvokeResult> ContinueAfterVideoImportAsync(VideoProduction production, CancellationToken cancellationToken = default);
    }
}
