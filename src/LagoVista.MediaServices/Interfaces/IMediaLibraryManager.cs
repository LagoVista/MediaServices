using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaLibraryManager
    {
        Task<InvokeResult> AddMediaLibraryAsync(MediaLibrary mediaLibrary, EntityHeader org, EntityHeader user);
        Task<MediaLibrary> GetMediaLibraryAsync(string id, EntityHeader org, EntityHeader user);

        Task<DependentObjectCheckResult> CheckMediaLibraryInUseAsync(string id, EntityHeader org, EntityHeader user);

        Task<IEnumerable<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId, EntityHeader user);
        Task<InvokeResult> UpdateMediaLibraryAsync(MediaLibrary mediaLibrary, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteMediaLibraryAsync(string id, EntityHeader org, EntityHeader user);
        Task<bool> QueryMediaLibraryKeyInUseAsync(string key, string orgId);
    }
}
