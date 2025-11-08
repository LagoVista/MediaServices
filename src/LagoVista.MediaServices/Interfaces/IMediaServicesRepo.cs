// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 6360dd9a5358d8b908f25c3a0cac7d64169bb94c2cc082262562c696781b6f2c
// IndexVersion: 2
// --- END CODE INDEX META ---
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
        Task<ListResponse<MediaResourceSummary>> GetResourcesAsync(string orgId, ListRequest listRequest);
        Task<ListResponse<MediaResourceSummary>> GetResourcesForLibraryAsync(string orgId, string libraryId, ListRequest listRequest);
        Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string orgId, string mediaTypeKey, ListRequest listRequest);
        Task UpdateMediaResourceRecordAsync(MediaResource updated);

        Task AddOrUpdateMediaResourceAsync(MediaResource record);
        Task<InvokeResult<string>> AddToContainerAsync(byte[] data, string containerName, string fileName, string contentType, bool isPublic);
    }

    public interface IMediaServicesConnectionSettings
    {
        IConnectionSettings MediaLibraryConnection { get; }
        IConnectionSettings MediaStorageConnection { get; }

        string ImageSearchUri { get; }
        string ImageSearchClientId { get; }
        string ImageSearchClientSecret { get;  }        
        string GoogleTextToSpeechAPIKey { get; }

        bool ShouldConsolidateCollections { get; }
    }
}
