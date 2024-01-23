using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static LagoVista.Core.Models.AuthorizeResult;

namespace LagoVista.MediaServices.Managers
{
    public class MediaServicesManager : ManagerBase, IMediaServicesManager
    {
        IMediaServicesRepo _mediaRepo;
        IMediaLibraryRepo _libraryRepo;

        public MediaServicesManager(IMediaServicesRepo mediaRepo, IMediaLibraryRepo repo, IAdminLogger logger, IAppConfig appConfig, IDependencyManager depmanager, ISecurity security) :
            base(logger, appConfig, depmanager, security)
        {
            _mediaRepo = mediaRepo;
            _libraryRepo = repo;
        }

        public async Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            if(resource.MediaLibrary != null && String.IsNullOrEmpty(resource.MediaLibrary.Text))
            {
                var library = await _libraryRepo.GetMediaLibrary(resource.MediaLibrary.Id);
                resource.MediaLibrary.Text = library.Name;
            }

            await AuthorizeAsync(resource, AuthorizeActions.Create, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.AddMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResourceSummary>.Create(resource.CreateSummary());
        }

        public async Task<InvokeResult<MediaResource>> AddResourceMediaAsync(String id, Stream stream, string fileName, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false)
        {
            var mediaResource = new MediaResource();
            mediaResource.Id = id;
            mediaResource.CreationDate = DateTime.UtcNow.ToJSONString();
            mediaResource.LastUpdatedDate = mediaResource.CreationDate;
            mediaResource.CreatedBy = user;
            mediaResource.IsPublic = isPublic;
            mediaResource.LastUpdatedBy = user;
            mediaResource.OwnerOrganization = org;
            if (saveResourceRecord)
            {
                mediaResource.Name = "Auto Inserted";
                mediaResource.Key = "autoinserted";
            }

            await AuthorizeAsync(user, org, "addDeviceTypeResource", $"{{mediaItemId:'{id}'}}");

            // Also sets the blob reference name.
            mediaResource.SetContentType(contentType);

            mediaResource.ResourceType = EntityHeader<MediaResourceTypes>.Create(mediaResource.MimeType.StartsWith("image") ? MediaResourceTypes.Picture : MediaResourceTypes.Other);

            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            mediaResource.FileName = fileName;
            mediaResource.ContentSize = stream.Length;
            
            var result = await _mediaRepo.AddMediaAsync(bytes, org.Id, mediaResource.StorageReferenceName, contentType);
            if (result.Successful)
            {
                if (saveResourceRecord)
                    await _mediaRepo.AddOrUpdateMediaResourceAsync(mediaResource);

                return InvokeResult<MediaResource>.Create(mediaResource);
            }
            else
            {
                return InvokeResult<MediaResource>.FromInvokeResult(result);
            }
        }

        public async Task<InvokeResult> DeleteMediaResourceAsync(string id, EntityHeader org, EntityHeader user)
        {
            var record = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(record, AuthorizeActions.Delete, user, org);

            await _mediaRepo.DeleteMediaRecordAsync(id);
            if (record.IsFileUpload)
            {
                await _mediaRepo.DeleteMediaAsync(record.StorageReferenceName, org.Id);
            }

            return InvokeResult.Success;
        }

        public async Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(resource, AuthorizeActions.Read, user, org);
            return resource;
        }

        public async Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaResource));
            return await _mediaRepo.GetResourcesForLibrary(orgId, libraryId, listRequest);
        }

        public async Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(MediaResource));
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            var mediaItem = await _mediaRepo.GetMediaAsync(resource.StorageReferenceName, org.Id);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }

            return new MediaItemResponse()
            {
                ContentType = resource.MimeType,
                FileName = resource.FileName,
                ImageBytes = mediaItem.Result
            };
        }

        public async Task<MediaItemResponse> GetMediaItemAsync(string orgId, string id)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            var mediaItem = await _mediaRepo.GetMediaAsync(id, orgId);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }

            return new MediaItemResponse()
            {
                ContentType = resource.MimeType,
                FileName = resource.FileName,
                ImageBytes = mediaItem.Result
            };
        }

        public async Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(resource, AuthorizeActions.Update, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.UpdateMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResourceSummary>.Create(resource.CreateSummary());
        }
    }
}
