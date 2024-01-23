using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesManager
    {
        Task<InvokeResult<MediaResource>> AddResourceMediaAsync(string id, Stream media, string name, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false);
        Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
        Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
        Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteMediaResourceAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, ListRequest listRequest, EntityHeader user);
    }
}
