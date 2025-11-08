// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: b47581ad099f8516aad09a996b9b7587a96bf0bcb78eb2584c3f89a6082543cb
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    public class ImageSearchController : LagoVistaBaseController
    {

        IMediaSearchManager _mediaSearchManager;

        public ImageSearchController(UserManager<AppUser> userManager, IAdminLogger logger, IMediaSearchManager mediaSearchManagerr) : 
            base(userManager, logger)
        {
            _mediaSearchManager = mediaSearchManagerr ?? throw new ArgumentNullException(nameof(mediaSearchManagerr));
        }

        [HttpGet("/api/media/images/search")]
        public Task<ListResponse<ImageSearchResult>> SearchImages(string term, string source)
        {
            return _mediaSearchManager.SearchImagesAsync(term, source, GetListRequestFromHeader(), OrgEntityHeader, UserEntityHeader);
        }
    }
}
