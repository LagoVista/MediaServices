using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoGenerationRequestStore
    {
        Task AddAsync(
            VideoGenerationRequest request,
            CancellationToken cancellationToken = default);

        Task<VideoGenerationRequest> GetAsync(
            string organizationId,
            string requestId,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            VideoGenerationRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string organizationId,
            string requestId,
            CancellationToken cancellationToken = default);
    }
}
