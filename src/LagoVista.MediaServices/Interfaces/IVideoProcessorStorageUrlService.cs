using LagoVista.Core.Validation;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProcessorStorageUrlService
    {
        Task<InvokeResult<VideoProcessorStorageDestination>> CreateWriteDestinationAsync(string orgId, string storageReferenceName, string contentType, CancellationToken cancellationToken = default);
        Task<InvokeResult<string>> CreateReadUrlAsync(string orgId, string storageReferenceName, CancellationToken cancellationToken = default);
    }

    public sealed class VideoProcessorStorageDestination
    {
        public string StorageReferenceName { get; set; }
        public string UploadUrl { get; set; }
        public string BlobUrl { get; set; }
    }
}
