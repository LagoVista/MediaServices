using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ConfirmedUser]
    public class VideoMediaImportController : LagoVistaBaseController
    {
        private readonly IVideoMediaImportManager _manager;

        public VideoMediaImportController(IVideoMediaImportManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpPost("/api/media/videoproduction/{id}/media/ensure")]
        public Task<InvokeResult<VideoMediaImportPreparationResult>> EnsureProviderVideoImportAsync(string id, [FromQuery] int? thumbnailTimeSeconds = null, CancellationToken cancellationToken = default)
        {
            return _manager.EnsureProviderVideoImportAsync(id, thumbnailTimeSeconds, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/videoproduction/{id}/media/prepare")]
        public Task<InvokeResult<VideoMediaImportPreparationResult>> PrepareProviderVideoImportAsync(string id, [FromQuery] int? thumbnailTimeSeconds = null, CancellationToken cancellationToken = default)
        {
            return _manager.PrepareProviderVideoImportAsync(id, thumbnailTimeSeconds, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [AllowAnonymous]
        [HttpPost("/api/media/webhooks/video-import")]
        public Task<InvokeResult<VideoProduction>> ApplyVideoMediaImportCallbackAsync([FromBody] VideoMediaImportCallback callback, CancellationToken cancellationToken = default)
        {
            return _manager.ApplyVideoMediaImportCallbackAsync(callback, cancellationToken);
        }
    }
}
