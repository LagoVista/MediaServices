using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesRepo
    {
        Task<InvokeResult> AddMediaAsync(byte[] data, string org, string fileName, string contentType);
        Task<InvokeResult<byte[]>> GetMediaAsync(string blobReferenceName, string org);
        Task<MediaResource> GetMediaResourceRecordAsync(string id);
        Task DeleteMediaRecordAsync(string id);
        Task DeleteMediaAsync(string blobReferenceName, string orgId);

        Task AddMediaResourceRecordAsync(MediaResource resource);
        Task<ListResponse<MediaResourceSummary>> GetResourcesForLibrary(string orgId, string libraryId, ListRequest listRequest);
        Task UpdateMediaResourceRecordAsync(MediaResource updated);

        Task AddOrUpdateMediaResourceAsync(MediaResource record);
    }

    public interface IMediaServicesConnectionSettings
    {
        IConnectionSettings MediaLibraryConnection { get; }
        IConnectionSettings MediaStorageConnection { get; }

        bool ShouldConsolidateCollections { get; }
    }
}
