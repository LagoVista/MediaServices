using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [SystemAdmin]
    [ConfirmedUser]
    public class LagoVistaSystemDefaultIconGenerationController : LagoVistaBaseController
    {
        private readonly ILagoVistaSystemDefaultIconGenerationManager _manager;

        public LagoVistaSystemDefaultIconGenerationController(ILagoVistaSystemDefaultIconGenerationManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpPost("/api/media/icons/semantic/default/{entityTypeName}")]
        public Task<InvokeResult<LagoVistaIconPublishResult>> GenerateDefaultIconAsync(string entityTypeName, [FromBody] LagoVistaDefaultIconGenerationRequest request)
        {
            return _manager.GenerateDefaultIconAsync(entityTypeName, request, OrgEntityHeader, UserEntityHeader);
        }
    }
}
