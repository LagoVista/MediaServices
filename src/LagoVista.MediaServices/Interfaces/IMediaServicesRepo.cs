using LagoVista.Core.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesRepo
    {
        Task<InvokeResult> AddMediaAsync(byte[] data, string org, string fileName, string contentType);
        Task<InvokeResult<byte[]>> GetMediaAsync(string id, string org);
        Task<MediaResource> GetMediaResourceRecordAsync(string id);
        Task AddMediaResourceRecordAsync(MediaResource resource);
        Task<IEnumerable<MediaResourceSummary>> GetResourcesForLibrary(string orgId, string libraryId);
        Task UpdateMediaResourceRecordAsync(MediaResource updated);
    }

    public interface IMediaServicesConnectionSettings
    {
        IConnectionSettings MediaLibraryConnection { get; }
        IConnectionSettings MediaStorageConnection { get; }

        bool ShouldConsolidateCollections { get; }
    }
}
