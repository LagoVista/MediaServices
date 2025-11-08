// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 22f7fbbc90308c7d550171e474614fa2d3fdf4140b9ead6f1988602876c6ae1b
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
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
        Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForCustomerAsync(string orgId, string customerId, ListRequest listRequest, EntityHeader user);
        Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId, ListRequest listRequest, EntityHeader user);
        Task<InvokeResult> UpdateMediaLibraryAsync(MediaLibrary mediaLibrary, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteMediaLibraryAsync(string id, EntityHeader org, EntityHeader user);
        Task<bool> QueryMediaLibraryKeyInUseAsync(string key, string orgId);
    }
}
