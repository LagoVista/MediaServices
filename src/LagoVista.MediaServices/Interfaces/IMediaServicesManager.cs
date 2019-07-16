using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesManager
    {
        Task<InvokeResult<MediaResource>> AddResourceMediaAsync(string id, Stream media, string name, string contentType, EntityHeader org, EntityHeader user);
        Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
        Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteMediaResourceAsync(string id, EntityHeader org, EntityHeader user);
        Task<IEnumerable<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, EntityHeader user);
    }
}
