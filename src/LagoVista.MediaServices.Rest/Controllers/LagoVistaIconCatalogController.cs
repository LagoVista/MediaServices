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
    public class LagoVistaIconCatalogController : LagoVistaBaseController
    {
        private readonly ILagoVistaIconCatalogManager _manager;

        public LagoVistaIconCatalogController(ILagoVistaIconCatalogManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/media/icons/semantic/catalog/{orgNamespace}/master")]
        public Task<InvokeResult<LagoVistaIconMasterCatalogDocument>> GetMasterCatalogAsync(string orgNamespace)
        {
            return _manager.GetMasterCatalogAsync(orgNamespace);
        }

        [HttpGet("/api/media/icons/semantic/catalog/{orgNamespace}/family/{familyKey}")]
        public Task<InvokeResult<LagoVistaIconCatalogDocument>> GetFamilyCatalogAsync(string orgNamespace, string familyKey)
        {
            return _manager.GetFamilyCatalogAsync(orgNamespace, familyKey);
        }

        [HttpGet("/api/media/icons/semantic/catalog/system/master")]
        public Task<InvokeResult<LagoVistaIconMasterCatalogDocument>> GetSystemMasterCatalogAsync()
        {
            return _manager.GetMasterCatalogAsync(LagoVistaIconGenerationRequest.SystemOrgNamespace);
        }

        [HttpGet("/api/media/icons/semantic/catalog/system/family/{familyKey}")]
        public Task<InvokeResult<LagoVistaIconCatalogDocument>> GetSystemFamilyCatalogAsync(string familyKey)
        {
            return _manager.GetFamilyCatalogAsync(LagoVistaIconGenerationRequest.SystemOrgNamespace, familyKey);
        }
    }
}
