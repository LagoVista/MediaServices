using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Rest.McpWidgets;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    public class ImageFamilyController : LagoVistaBaseController
    {
        private readonly IImageFamilyManager _manager;
        private readonly IMediaLibraryManager _libraryManager;

        public ImageFamilyController(UserManager<AppUser> userManager, IAdminLogger logger,
            IImageFamilyManager manager, IMediaLibraryManager libraryManager) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
        }

        [McpEndpoint("media-image-families-factory", Description = "Create an unsaved image family for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamily/factory")]
        public DetailResponse<ImageFamily> CreateImageFamily()
        {
            var response = DetailResponse<ImageFamily>.Create();
            response.Model.Id = Guid.NewGuid().ToId();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            return response;
        }

        [McpEndpoint("media-image-families-create", Description = "Create an organization-owned image family with reusable visual guidance.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/imagefamily")]
        public Task<InvokeResult> AddImageFamilyAsync([FromBody] ImageFamily imageFamily)
        {
            SetAuditProperties(imageFamily);
            SetOwnedProperties(imageFamily);
            return _manager.AddImageFamilyAsync(imageFamily, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-image-families-update", Description = "Update visual guidance for an existing image family.", FamilyId = "media")]
        [Authorize]
        [HttpPut("/api/media/imagefamily")]
        public Task<InvokeResult> UpdateImageFamilyAsync([FromBody] ImageFamily imageFamily)
        {
            SetUpdatedProperties(imageFamily);
            return _manager.UpdateImageFamilyAsync(imageFamily, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Open a reusable workspace for creating or maintaining one Image Family.
        /// Only the ImageFamily record is persisted: the Studio itself has no
        /// separate workflow state. An omitted id starts an unsaved new family.
        /// </summary>
        [McpEndpoint("media-image-family-studio-open",
            Description = "OPEN THE APTIX IMAGE FAMILY STUDIO to create an Image Family or reopen an existing one. Optional imageFamilyId selects a saved family. The widget edits reusable visual guidance and allows selection/removal of existing Media Resource references using the authorized gallery. Save explicitly through existing media-image-families-create/update. No images are generated, imported, or uploaded by opening the Studio. Use this tool when the user wants to build, review, or maintain an image family.",
            FamilyId = "media", WidgetResourceUri = MediaImageFamilyStudioWidgetResource.ResourceUri)]
        [Authorize]
        [HttpGet("/api/media/imagefamily/studio/open")]
        public async Task<InvokeResult<MediaImageFamilyStudioOpen>> OpenImageFamilyStudio(string imageFamilyId = null)
        {
            var familyList = await _manager.GetImageFamiliesForOrgAsync(
                OrgEntityHeader.Id, ListRequest.Create(1, 100), UserEntityHeader);
            if (!familyList.Successful)
                return InvokeResult<MediaImageFamilyStudioOpen>.FromInvokeResult(familyList.ToInvokeResult());

            var libraryList = await _libraryManager.GetMediaLibrariesForOrgsAsync(
                OrgEntityHeader.Id, ListRequest.Create(1, 100), UserEntityHeader);
            if (!libraryList.Successful)
                return InvokeResult<MediaImageFamilyStudioOpen>.FromInvokeResult(libraryList.ToInvokeResult());

            ImageFamily selected;
            if (String.IsNullOrWhiteSpace(imageFamilyId))
            {
                selected = new ImageFamily { Id = Guid.NewGuid().ToId() };
                SetAuditProperties(selected);
                SetOwnedProperties(selected);
            }
            else
            {
                selected = await _manager.GetImageFamilyAsync(imageFamilyId, OrgEntityHeader, UserEntityHeader);
            }

            return InvokeResult<MediaImageFamilyStudioOpen>.Create(new MediaImageFamilyStudioOpen
            {
                InitialFamilyId = String.IsNullOrWhiteSpace(imageFamilyId) ? null : selected.Id.ToString(),
                Family = selected,
                Families = (familyList.Model ?? Enumerable.Empty<ImageFamilySummary>())
                    .Select(family => new MediaImageCreationFamily
                    {
                        Id = family.Id,
                        Name = family.Name,
                        Key = family.Key
                    }).ToList(),
                Libraries = (libraryList.Model ?? Enumerable.Empty<MediaLibrarySummary>())
                    .Select(library => new MediaGalleryLibrary
                    {
                        Id = library.Id,
                        Name = library.Name
                    }).ToList()
            });
        }

        [McpEndpoint("media-image-families-list", Description = "List image families available to the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamilies")]
        public Task<ListResponse<ImageFamilySummary>> GetImageFamiliesForOrgAsync()
        {
            return _manager.GetImageFamiliesForOrgAsync(OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }

        [McpEndpoint("media-image-families-get", Description = "Get one image family by id with organization authorization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamily/{id}")]
        public async Task<DetailResponse<ImageFamily>> GetImageFamilyAsync(string id)
        {
            var family = await _manager.GetImageFamilyAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<ImageFamily>.Create(family);
        }

        [McpEndpoint("media-image-families-get-by-key", Description = "Get an image family by key for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamily/key/{key}")]
        public async Task<DetailResponse<ImageFamily>> GetImageFamilyByKeyAsync(string key)
        {
            var family = await _manager.GetImageFamilyByKeyAsync(key, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<ImageFamily>.Create(family);
        }

        [McpEndpoint("media-image-families-key-in-use", Description = "Check whether an image family key is already in use.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamily/{key}/keyinuse")]
        public Task<bool> GetImageFamilyKeyInUseAsync(string key)
        {
            return _manager.QueryImageFamilyKeyInUseAsync(key, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-image-families-in-use", Description = "Check image family dependencies before deletion.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/imagefamily/{id}/inuse")]
        public Task<DependentObjectCheckResult> CheckImageFamilyInUseAsync(string id)
        {
            return _manager.CheckImageFamilyInUseAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [McpEndpoint("media-image-families-delete", Description = "Delete an image family after checking dependencies.", FamilyId = "media")]
        [Authorize]
        [HttpDelete("/api/media/imagefamily/{id}")]
        public Task<InvokeResult> DeleteImageFamilyAsync(string id)
        {
            return _manager.DeleteImageFamilyAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}
