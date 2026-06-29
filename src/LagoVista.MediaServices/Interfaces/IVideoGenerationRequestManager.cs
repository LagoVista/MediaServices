using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoGenerationManager
    {
        Task<InvokeResult<VideoGenerationRequest>> GenerateVideoAsync(GenerateEntityVideoRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<VideoGenerationRequest>> GetVideoGenerationRequestAsync(string organizationId, string requestId, CancellationToken cancellationToken = default);
    }
}
