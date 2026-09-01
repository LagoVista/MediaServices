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
    public class VideoCompositionBlockTemplateController : LagoVistaBaseController
    {
        private readonly IVideoCompositionBlockTemplateManager _manager;

        public VideoCompositionBlockTemplateController(IVideoCompositionBlockTemplateManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/media/videocomposition/blocktemplate/factory")]
        public DetailResponse<VideoCompositionBlockTemplate> CreateVideoCompositionBlockTemplate()
        {
            var form = DetailResponse<VideoCompositionBlockTemplate>.Create();
            form.Model.Name = "New Block Template";
            form.Model.Key = "block-template-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);
            return form;
        }

        [HttpPost("/api/media/videocomposition/blocktemplate/fromblock")]
        public Task<DetailResponse<VideoCompositionBlockTemplate>> CreateVideoCompositionBlockTemplateFromBlockAsync([FromBody] CreateVideoCompositionBlockTemplateRequest request)
        {
            return _manager.CreateTemplateFromBlockAsync(request, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videocomposition/blocktemplate/{id}/fromblock")]
        public Task<InvokeResult> UpdateVideoCompositionBlockTemplateFromBlockAsync(string id, [FromBody] UpdateVideoCompositionBlockTemplateFromBlockRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return _manager.UpdateVideoCompositionBlockTemplateFromBlockAsync(id, request.Block, OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("/api/media/videocomposition/blocktemplate/{id}/block")]
        public Task<DetailResponse<VideoCompositionBlock>> CreateVideoCompositionBlockFromTemplateAsync(string id)
        {
            return _manager.CreateBlockFromTemplateAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("/api/media/videocomposition/blocktemplate/{id}")]
        public async Task<DetailResponse<VideoCompositionBlockTemplate>> GetVideoCompositionBlockTemplateAsync(string id)
        {
            var template = await _manager.GetVideoCompositionBlockTemplateAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<VideoCompositionBlockTemplate>.Create(template);
        }

        [HttpGet("/api/media/videocomposition/blocktemplates")]
        public Task<ListResponse<VideoCompositionBlockTemplateSummary>> GetVideoCompositionBlockTemplatesAsync()
        {
            return _manager.GetVideoCompositionBlockTemplatesForOrgAsync(OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader());
        }

        [HttpGet("/api/media/videocomposition/blocktemplate/{key}/keyinuse")]
        public Task<bool> GetVideoCompositionBlockTemplateKeyInUseAsync(string key)
        {
            return _manager.QueryKeyInUseAsync(key, OrgEntityHeader);
        }

        [HttpPost("/api/media/videocomposition/blocktemplate")]
        public Task<InvokeResult> AddVideoCompositionBlockTemplateAsync([FromBody] VideoCompositionBlockTemplate template)
        {
            return _manager.AddVideoCompositionBlockTemplateAsync(template, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videocomposition/blocktemplate")]
        public Task<InvokeResult> UpdateVideoCompositionBlockTemplateAsync([FromBody] VideoCompositionBlockTemplate template)
        {
            SetUpdatedProperties(template);
            return _manager.UpdateVideoCompositionBlockTemplateAsync(template, OrgEntityHeader, UserEntityHeader);
        }

        [HttpDelete("/api/media/videocomposition/blocktemplate/{id}")]
        public Task<InvokeResult> DeleteVideoCompositionBlockTemplateAsync(string id)
        {
            return _manager.DeleteVideoCompositionBlockTemplateAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}
