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
        ///  Device Type Resource - Create New
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/mediaresource/factory")]
        public DetailResponse<MediaResource> CreateDeviceResource()
        {
            var response = DetailResponse<MediaResource>.Create();
            response.Model.Id = Guid.NewGuid().ToId();
            return response;
        }

        /// <summary>
        /// Upload a media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resources/{id}")]
        public async Task<InvokeResult<MediaResource>> UploadMediaAsync(string id, IFormFile file)
        {
            using (var strm = file.OpenReadStream())
            {
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.ContentType, OrgEntityHeader, UserEntityHeader);
            }
        }

        /// <summary>
        /// Download a media item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("/api/media/resources/{id}")]
        public async Task<IActionResult> DownloadMedia(string id)
        {
            var response = await _mediaServicesManager.GetResourceMediaAsync(id, OrgEntityHeader, UserEntityHeader);

            var ms = new MemoryStream(response.ImageBytes);
            return new FileStreamResult(ms, response.ContentType);
        }
    }
}
