using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
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
    [ConfirmedUser]
    public class VideoCompositionTemplateController : LagoVistaBaseController
    {
        private readonly IVideoCompositionTemplateManager _manager;
        private readonly IMediaLibraryRepo _mediaLibraryRepo;

        public VideoCompositionTemplateController(IVideoCompositionTemplateManager manager, IMediaLibraryRepo mediaLibraryRepo, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _mediaLibraryRepo = mediaLibraryRepo ?? throw new ArgumentNullException(nameof(mediaLibraryRepo));
        }

        [HttpGet("/api/media/videocompositiontemplate/factory")]
        public async Task<DetailResponse<VideoCompositionTemplate>> CreateVideoCompositionTemplateAsync()
        {
            var form = DetailResponse<VideoCompositionTemplate>.Create();
            form.Model.Name = $"Video Composition Template {DateTime.Now}";
            form.Model.Key = "vct" + Guid.NewGuid().ToId().Value.ToLowerInvariant();
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);

            var defaultLibrary = await _mediaLibraryRepo.GetMediaLibraryByKeyAsync(OrgEntityHeader.Id, "publishedvideo");
            form.Model.OutputMediaLibrary = defaultLibrary?.ToEntityHeader();

            return form;
        }

        [HttpGet("/api/media/videocompositiontemplate/{id}")]
        public async Task<DetailResponse<VideoCompositionTemplate>> GetVideoCompositionTemplateAsync(string id)
        {
            var template = await _manager.GetVideoCompositionTemplateAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<VideoCompositionTemplate>.Create(template);
        }

        [HttpGet("/api/media/videocompositiontemplates")]
        public Task<ListResponse<VideoCompositionTemplateSummary>> GetVideoCompositionTemplatesAsync()
        {
            return _manager.GetVideoCompositionTemplatesForOrgAsync(OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader());
        }

        [HttpGet("/api/media/videocompositiontemplate/{key}/keyinuse")]
        public Task<bool> GetVideoCompositionTemplateKeyInUseAsync(string key)
        {
            return _manager.QueryKeyInUseAsync(key, OrgEntityHeader);
        }

        [HttpPost("/api/media/videocompositiontemplate")]
        public Task<InvokeResult> AddVideoCompositionTemplateAsync([FromBody] VideoCompositionTemplate template)
        {
            return _manager.AddVideoCompositionTemplateAsync(template, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videocompositiontemplate/fromcomposition/{compositionId}")]
        public Task<InvokeResult<VideoCompositionTemplate>> CreateVideoCompositionTemplateFromCompositionAsync(string compositionId, [FromBody] CreateVideoCompositionTemplateFromCompositionRequest request)
        {
            return _manager.CreateFromCompositionAsync(compositionId, request, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videocompositiontemplate")]
        public Task<InvokeResult> UpdateVideoCompositionTemplateAsync([FromBody] VideoCompositionTemplate template)
        {
            SetUpdatedProperties(template);
            return _manager.UpdateVideoCompositionTemplateAsync(template, OrgEntityHeader, UserEntityHeader);
        }

        [HttpDelete("/api/media/videocompositiontemplate/{id}")]
        public Task<InvokeResult> DeleteVideoCompositionTemplateAsync(string id)
        {
            return _manager.DeleteVideoCompositionTemplateAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}
