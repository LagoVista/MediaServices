// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 9f91ba689ef3181489d9457b22a8c0fa840b2e85fed7eb8fe6077a60fc9cb21f
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesManager
    {
        Task<InvokeResult<MediaResource>> AddResourceMediaAsync(string id, Stream media, string name, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "", string url = "", string responseId = "",
            string originalPrompt = "", string revisedPrompt = "", string entityTypeName = "", string entityTypeFieldName = "", string size = "", string resourceName = "");
        Task<InvokeResult<MediaResource>> AddResourceMediaRevisionAsync(String id, Stream stream, string fileName, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "", string Url = "", string responseId = "", 
            string originalPrompt = "", string revisedPrompt = "",string size = "");

        Task<InvokeResult<MediaResourceSummary>> GenerateAudioAsync(TextToSpeechRequest request, EntityHeader org, EntityHeader user);

        Task<InvokeResult<MediaResourceSummary>> UpdateGeneratedAudioAsync(string requestId, TextToSpeechRequest request, EntityHeader org, EntityHeader user);

        Task<InvokeResult<MediaResourceSummary>> UpdateAudioAsync(string resourceId, TextToSpeechRequest request, EntityHeader org, EntityHeader user);

        Task<InvokeResult<List<EntityHeader>>> GetVoicesAsync(string languageCode, EntityHeader org, EntityHeader user);

        Task<Stream> PreviewAudioAsync(TextToSpeechRequest request, EntityHeader org, EntityHeader user);

        Task<InvokeResult<MediaResource>> AddResourceMediaAsync(Uri url, string name,  EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "");

        Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
       
        Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user);
        Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user);
        Task<MediaItemResponse> GetPublicResourceRecordAsync(string ownerOrgId, string id, string lastModified = null);
        Task<MediaItemResponse> GetMediaRevisionAsync(string id, string revisionId, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteMediaResourceAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, ListRequest listRequest, EntityHeader user);
        Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(ListRequest listRequest, EntityHeader org, EntityHeader user);
        Task<InvokeResult<MediaResource>> ResizeImageAsync(string id, string fileName, int width, int height, string fileType, EntityHeader org, EntityHeader user);
        Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string mediaTypeKey, string orgId, ListRequest listRequest, EntityHeader user);
        Task<InvokeResult<ImageDetails>> AddImageAsPngAsync(Stream stream, string containerName, bool isPublic, int width, int height);
        Task<InvokeResult<MediaResource>> CloneMediaResourceAsync(string id, string entityName, string entityFieldName, string resourceName, EntityHeader orgEntityHeader, EntityHeader userEntityHeader);
    }
}
