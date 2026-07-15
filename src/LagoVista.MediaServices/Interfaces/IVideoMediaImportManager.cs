using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoMediaImportManager
    {
        Task<InvokeResult<VideoMediaImportPreparationResult>> EnsureProviderVideoImportAsync(string productionId, int? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
        Task<InvokeResult<VideoMediaImportPreparationResult>> PrepareProviderVideoImportAsync(string productionId, int? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
        Task<InvokeResult<VideoProduction>> ApplyVideoMediaImportCallbackAsync(VideoMediaImportCallback callback, CancellationToken cancellationToken = default);
    }
}
