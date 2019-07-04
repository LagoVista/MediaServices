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

        public MediaServicesManager(IMediaServicesRepo mediaRepo, IAdminLogger logger, IAppConfig appConfig, IDependencyManager depmanager, ISecurity security) :
            base(logger, appConfig, depmanager, security)
        {
            _mediaRepo = mediaRepo;
        }

        public async Task<InvokeResult> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(resource, AuthorizeActions.Create, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.AddMediaResourceRecordAsync(resource);

            return InvokeResult.Success;
        }


        public async Task<InvokeResult<MediaResource>> AddResourceMediaAsync(String id, Stream stream, string contentType, EntityHeader org, EntityHeader user)
        {
            var deviceTypeResource = new MediaResource();
            deviceTypeResource.Id = id;
            await AuthorizeAsync(user, org, "addDeviceTypeResource", $"{{mediaItemId:'{id}'}}");

            deviceTypeResource.SetContentType(contentType);

            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);

            deviceTypeResource.ContentSize = stream.Length;

            var result = await _mediaRepo.AddMediaAsync(bytes, org.Id, deviceTypeResource.FileName, contentType);
            if (result.Successful)
            {
                return InvokeResult<MediaResource>.Create(deviceTypeResource);
            }
            else
            {
                return InvokeResult<MediaResource>.FromInvokeResult(result);
            }
        }

        public async Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(resource, AuthorizeActions.Read, user, org);
            return resource;
        }

        public async Task<IEnumerable<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaResource));
            return await _mediaRepo.GetResourcesForLibrary(orgId, libraryId);
        }

        public async Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(MediaResource));

            var mediaItem = await _mediaRepo.GetMediaAsync(id, org.Id);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }

            return new MediaItemResponse()
            {
              ///  ContentType = deviceResource.MimeType,
               // FileName = deviceResource.FileName,
                ImageBytes = mediaItem.Result
            };
        }

        public async Task<InvokeResult> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(resource, AuthorizeActions.Update, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.UpdateMediaResourceRecordAsync(resource);

            return InvokeResult.Success;
        }
    }
}
