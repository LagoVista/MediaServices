// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 6ee5265484e6ecf4358632e04f8af8c2731aec62634c9424051d59c20338b931
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaLibraryRepo
    { 
        Task AddMediaLibraryAsync(MediaLibrary mediaLibrary);
        Task<MediaLibrary> GetMediaLibraryAsync(string id);
        Task<MediaLibrary> GetMediaLibraryByKeyAsync(string orgId, string id);
        Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId, ListRequest listRequest);
        Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForCustomerAsync(string orgId, string customerId, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
        Task UpdateMediaLibraryAsync(MediaLibrary mediaLibrary);
        Task DeleteStateMachineAsync(string id);
    }
}
