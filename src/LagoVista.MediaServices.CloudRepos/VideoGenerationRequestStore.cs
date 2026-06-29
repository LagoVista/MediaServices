using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class VideoGenerationRequestStore : TableStorageBase<VideoGenerationRequest>, IVideoGenerationRequestStore
    {
        public VideoGenerationRequestStore(IMediaServicesConnectionSettings tableStorageClient, IAdminLogger logger) :
            base(tableStorageClient.MediaStorageConnection.AccountId, tableStorageClient.MediaStorageConnection.AccessKey, logger)
        {
        }

        public Task AddAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default)
        {
            return InsertAsync(request);
        }

        public Task DeleteAsync(string organizationId, string requestId, CancellationToken cancellationToken = default)
        {
            return RemoveAsync(organizationId, requestId);
        }

        public Task<VideoGenerationRequest> GetAsync(string organizationId, string requestId, CancellationToken cancellationToken = default)
        {
            return base.GetAsync(organizationId, requestId);
        }

        public Task UpdateAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default)
        {
            return base.UpdateAsync(request);
        }
    }
}
