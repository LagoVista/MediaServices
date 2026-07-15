using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProcessorCallbackRegistrationStore
    {
        Task AddAsync(VideoProcessorCallbackRegistration registration, CancellationToken cancellationToken = default);
        Task<VideoProcessorCallbackRegistration> GetAsync(string requestId, string attemptId, CancellationToken cancellationToken = default);
        Task UpdateAsync(VideoProcessorCallbackRegistration registration, CancellationToken cancellationToken = default);
        Task DeleteAsync(string requestId, string attemptId, CancellationToken cancellationToken = default);
    }
}
