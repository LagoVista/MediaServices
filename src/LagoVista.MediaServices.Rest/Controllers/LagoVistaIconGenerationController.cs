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
        private readonly ILagoVistaEntityInstanceIconGenerationManager _manager;

        public LagoVistaIconGenerationController(UserManager<AppUser> userManager, IAdminLogger logger, ILagoVistaEntityInstanceIconGenerationManager manager, ILagoVistaIconGenerationManager iconGenerationManager) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
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
            return _iconGenerationManager.GenerateAsync(request, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/icons/semantic/publish")]
        public Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync([FromBody] LagoVistaIconAssetPublishRequest publishRequest)
        {
            return _iconGenerationManager.PublishAsync(publishRequest, OrgEntityHeader, UserEntityHeader);
        }


        [HttpPost("/api/media/icons/semantic/instance/generate")]
        public Task<InvokeResult<LagoVistaGeneratedInstanceIconResult>> GenerateInstanceIconAsync([FromBody] LagoVistaGeneratedInstanceIconRequest request)
        {
            return _manager.GenerateInstanceIconAsync(request, OrgEntityHeader, UserEntityHeader);
        }
    }
}
