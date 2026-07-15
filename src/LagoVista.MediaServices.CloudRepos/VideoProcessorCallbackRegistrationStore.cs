using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public sealed class VideoProcessorCallbackRegistrationStore : TableStorageBase<VideoProcessorCallbackRegistration>, IVideoProcessorCallbackRegistrationStore
    {
        public VideoProcessorCallbackRegistrationStore(IMediaServicesConnectionSettings settings, IAdminLogger logger) : base(settings.MediaStorageConnection.AccountId, settings.MediaStorageConnection.AccessKey, logger)
        {
        }

        public Task AddAsync(VideoProcessorCallbackRegistration registration, CancellationToken cancellationToken = default)
        {
            return InsertAsync(registration);
        }

        public Task<VideoProcessorCallbackRegistration> GetAsync(string requestId, string attemptId, CancellationToken cancellationToken = default)
        {
            return base.GetAsync(requestId, attemptId);
        }

        public Task UpdateAsync(VideoProcessorCallbackRegistration registration, CancellationToken cancellationToken = default)
        {
            return base.UpdateAsync(registration);
        }

        public Task DeleteAsync(string requestId, string attemptId, CancellationToken cancellationToken = default)
        {
            return RemoveAsync(requestId, attemptId);
        }
    }
}
