using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
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
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    public class BrandStandardsController : LagoVistaBaseController
    {
        private readonly IBrandStandardsManager _manager;

        public BrandStandardsController(UserManager<AppUser> userManager, IAdminLogger logger,
            IBrandStandardsManager manager) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [McpEndpoint("media-brand-standards-factory", Description = "Create an unsaved Brand Standards record for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/factory")]
        public DetailResponse<BrandStandards> CreateBrandStandards()
        {
            var response = DetailResponse<BrandStandards>.Create();
            response.Model.Id = Guid.NewGuid().ToId();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            return response;
        }

        [McpEndpoint("media-brand-standards-logo-factory", Description = "Create an unsaved logo entry for Brand Standards.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/logo/factory")]
        public DetailResponse<BrandStandardLogo> CreateBrandStandardLogo()
        {
            return DetailResponse<BrandStandardLogo>.Create(new BrandStandardLogo());
        }

        [McpEndpoint("media-brand-standards-color-factory", Description = "Create an unsaved color entry for Brand Standards.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/color/factory")]
        public DetailResponse<BrandStandardColor> CreateBrandStandardColor()
        {
            return DetailResponse<BrandStandardColor>.Create(new BrandStandardColor());
        }

        [McpEndpoint("media-brand-standards-create", Description = "Create organization-owned Brand Standards with approved logos, colors, typography and writing guidance.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/brandstandards")]
        public Task<InvokeResult> AddBrandStandardsAsync([FromBody] BrandStandards brandStandards)
        {
            SetAuditProperties(brandStandards);
            SetOwnedProperties(brandStandards);
            return _manager.AddBrandStandardsAsync(brandStandards, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-brand-standards-update", Description = "Update the complete existing Brand Standards record; use a full-record update.", FamilyId = "media")]
        [Authorize]
        [HttpPut("/api/media/brandstandards")]
        public Task<InvokeResult> UpdateBrandStandardsAsync([FromBody] BrandStandards brandStandards)
        {
            SetUpdatedProperties(brandStandards);
            return _manager.UpdateBrandStandardsAsync(brandStandards, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-brand-standards-list", Description = "List brand standards available to the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards")]
        public Task<ListResponse<BrandStandardsSummary>> GetBrandStandardsForOrgAsync()
        {
            return _manager.GetBrandStandardsForOrgAsync(OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }

        [McpEndpoint("media-brand-standards-get", Description = "Get Brand Standards by id with organization authorization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/{id}")]
        public async Task<DetailResponse<BrandStandards>> GetBrandStandardsAsync(string id)
        {
            var brandStandards = await _manager.GetBrandStandardsAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<BrandStandards>.Create(brandStandards);
        }

        [McpEndpoint("media-brand-standards-get-by-key", Description = "Get Brand Standards by key for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/key/{key}")]
        public async Task<DetailResponse<BrandStandards>> GetBrandStandardsByKeyAsync(string key)
        {
            var brandStandards = await _manager.GetBrandStandardsByKeyAsync(key, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<BrandStandards>.Create(brandStandards);
        }

        [McpEndpoint("media-brand-standards-key-in-use", Description = "Check whether a Brand Standards key is already used.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/{key}/keyinuse")]
        public Task<bool> GetBrandStandardsKeyInUseAsync(string key)
        {
            return _manager.QueryBrandStandardsKeyInUseAsync(key, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-brand-standards-in-use", Description = "Check Brand Standards dependencies before deletion.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/brandstandards/{id}/inuse")]
        public Task<DependentObjectCheckResult> CheckBrandStandardsInUseAsync(string id)
        {
            return _manager.CheckBrandStandardsInUseAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-brand-standards-delete", Description = "Delete Brand Standards after checking dependencies.", FamilyId = "media")]
        [Authorize]
        [HttpDelete("/api/media/brandstandards/{id}")]
        public Task<InvokeResult> DeleteBrandStandardsAsync(string id)
        {
            return _manager.DeleteBrandStandardsAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}
