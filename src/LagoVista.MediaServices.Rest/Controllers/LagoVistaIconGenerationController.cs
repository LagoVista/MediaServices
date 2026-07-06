using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    public class LagoVistaIconGenerationController : LagoVistaBaseController
    {
        private readonly ILagoVistaIconGenerationManager _iconGenerationManager;

        public LagoVistaIconGenerationController(UserManager<AppUser> userManager, IAdminLogger logger, ILagoVistaIconGenerationManager iconGenerationManager) : base(userManager, logger)
        {
            _iconGenerationManager = iconGenerationManager ?? throw new ArgumentNullException(nameof(iconGenerationManager));
        }

        [HttpPost("/api/media/icons/semantic/prompt")]
        public InvokeResult<string> BuildPrompt([FromBody] LagoVistaIconGenerationRequest request)
        {
            return _iconGenerationManager.BuildPrompt(request);
        }

        [HttpPost("/api/media/icons/semantic/generate")]
        public Task<InvokeResult<LagoVistaIconPublishResult>> GenerateAsync([FromBody] LagoVistaIconGenerationRequest request)
        {
            return _iconGenerationManager.GenerateAsync(request);
        }

        [HttpPost("/api/media/icons/semantic/publish")]
        public Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync([FromBody] LagoVistaIconAssetPublishRequest publishRequest)
        {
            return _iconGenerationManager.PublishAsync(publishRequest);
        }
    }
}
