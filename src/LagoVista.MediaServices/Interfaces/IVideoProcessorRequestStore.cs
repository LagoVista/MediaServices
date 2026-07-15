using LagoVista.Core.Validation;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProcessorRequestStore
    {
        Task<InvokeResult<VideoProcessorStoredRequest>> SaveAsync<TRequest>(string orgId, string jobType, string requestId, string attemptId, TRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class VideoProcessorStoredRequest
    {
        public string StorageReferenceName { get; set; }
        public string BlobUrl { get; set; }
        public string RequestUrl { get; set; }
    }
}
