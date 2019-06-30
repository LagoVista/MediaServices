using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaLibraryRepo
    { 
        Task AddMediaLibraryAsync(MediaLibrary mediaLibrary);
        Task<MediaLibrary> GetMediaLibrary(string id);
        Task<IEnumerable<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
        Task UpdateMediaLibraryAsync(MediaLibrary mediaLibrary);
        Task DeleteStateMachineAsync(string id);
    }
}
