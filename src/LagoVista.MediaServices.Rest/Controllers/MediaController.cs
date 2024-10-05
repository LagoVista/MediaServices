using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using LagoVista.Core;
using LagoVista.Core.Validation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using System.Globalization;

namespace LagoVista.MediaServices.Rest.Controllers
{
    /// <summary>
    /// The Media Controller will let users upload images and download them.
    /// </summary>
    public class MediaController : LagoVistaBaseController
    {

        IMediaServicesManager _mediaServicesManager;

        /// <summary>
        /// The Media Controller will let users upload images and download them.
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        /// <param name="mediaServiceManager"></param>
        public MediaController(UserManager<AppUser> userManager, IAdminLogger logger, IMediaServicesManager mediaServiceManager) : base(userManager, logger)
        {
            _mediaServicesManager = mediaServiceManager;
        }

        /// <summary>
        ///  Meida Resources - Create new record.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/resource/factory")]
        public DetailResponse<MediaResource> CreateDeviceResource()
        {
            var response = DetailResponse<MediaResource>.Create();
            response.Model.Id = Guid.NewGuid().ToId();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            return response;
        }

        /// <summary>
        /// Media Resources - Add a Media Resource Record
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        [HttpPost("/api/media/resource")]
        public Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync([FromBody] MediaResource resource)
        {
            return _mediaServicesManager.AddMediaResourceRecordAsync(resource, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Add a Media Resource Record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("/api/media/resource/{id}")]
        public async Task<DetailResponse<MediaResource>> GetMediaResource(string id)
        {
            var resource = await _mediaServicesManager.GetMediaResourceRecordAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<MediaResource>.Create(resource);
        }

        /// <summary>
        /// Media Resources - Delete the Media Resource Record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/media/resource/{id}")]
        public Task<InvokeResult> DeleteMediaResource(string id)
        {
            return _mediaServicesManager.DeleteMediaResourceAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Update Media Resource Record.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        [HttpPut("/api/media/resource")]
        public Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync([FromBody] MediaResource resource)
        {
            SetUpdatedProperties(resource);
            return _mediaServicesManager.UpdateMediaResourceRecordAsync(resource, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Get resources for library.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/library/{libraryid}/resources")]
        public Task<ListResponse<MediaResourceSummary>> GetResourceForLibraryAsync(string libraryid)
        {
            return _mediaServicesManager.GetMediaResourceSummariesAsync(libraryid, OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }


        /// <summary>
        /// Media Resources - Get resources for librarys.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/mediatype/key/{mediatypekey}/resources")]
        public Task<ListResponse<MediaResourceSummary>> GetForMediaTypeKey(string mediatypekey)
        {
            return _mediaServicesManager.GetResourcesForMediaTypeKeyLibrary(mediatypekey, OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }


        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resources/{id}")]
        public async Task<InvokeResult<MediaResource>> UploadMediaAsync(string id, IFormFile file, bool saveresource = false)
        {
            using (var strm = file.OpenReadStream())
            {
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader, saveresource);
            }
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resource/public/upload")]
        public async Task<InvokeResult<MediaResource>> UploadPublicMediaAsync(IFormFile file)
        {
            using (var strm = file.OpenReadStream())
            {
                var id = Guid.NewGuid().ToId();
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader, true, true);
            }
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resource/upload")]
        public async Task<InvokeResult<MediaResource>> UploadMediaAsync(IFormFile file)
        {
            using (var strm = file.OpenReadStream())
            {
                var id = Guid.NewGuid().ToId();
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader, true, false);
            }
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPut("/api/media/resource/{id}/resize")]
        public  Task<InvokeResult<MediaResource>> ResizeMediaAsync([FromBody] MediaResizeRequest request, string id)
        {
            return _mediaServicesManager.ResizeImageAsync(id, request.FileName, request.Width, request.Height, request.FileType, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resource - Download a media resource file.
        /// </summary>
        /// <param name="orgid"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("/api/media/resource/{orgid}/{id}/download")]
        public async Task<IActionResult> DownloadMedia(string orgid, string id)
        {
            var lastMod = String.Empty;
            if (!String.IsNullOrEmpty(Request.Headers["If-Modified-Since"]))
            {
                CultureInfo provider = CultureInfo.InvariantCulture;
                lastMod = DateTime.ParseExact(Request.Headers["If-Modified-Since"], "r", provider).ToJSONString();
            }
            var response = await _mediaServicesManager.GetPublicResourceRecordAsync(orgid, id, lastMod);
            if (response.NotModified)
            {
                return StatusCode(304);
            }

            var ms = new MemoryStream(response.ImageBytes);
            var idx = 1;
            foreach(var timing in response.Timings)
            {
                Response.Headers.Add($"x-{idx++}-{timing.Key}", $"{timing.Ms}ms");
            }

            Response.Headers[HeaderNames.CacheControl] = "public";
            Response.Headers[HeaderNames.LastModified] = new[] { response.LastModified.ToDateTime().ToString("R") }; // Format RFC1123

            return File(ms, response.ContentType, response.FileName);
        }

        [HttpPost("/api/media/resource/request")]
        public async Task<InvokeResult<MediaResource>> UploadAsync([FromBody] MediaUploadRequest uploadRequest )
        {
            return await _mediaServicesManager.AddResourceMediaAsync(new Uri(uploadRequest.Uri), uploadRequest.FileName, OrgEntityHeader, UserEntityHeader, true, uploadRequest.IsPublic, uploadRequest.License);
        }
    }
}
