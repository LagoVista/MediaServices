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
    public class VideoProductionController : LagoVistaBaseController
    {
        private readonly IVideoProductionManager _manager;

        public VideoProductionController(IVideoProductionManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/media/videoproduction/factory")]
        public DetailResponse<VideoProduction> CreateVideoProduction()
        {
            var form = DetailResponse<VideoProduction>.Create();
            form.Model.Name = $"Video Production {DateTime.Now}";
            form.Model.Key = "v" + Guid.NewGuid().ToId().ToString();
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);
            return form;
        }

        [HttpGet("/api/media/videoproduction/{id}")]
        public async Task<DetailResponse<VideoProduction>> GetVideoProductionAsync(string id)
        {
            var production = await _manager.GetVideoProductionAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<VideoProduction>.Create(production);
        }

        [HttpGet("/api/media/videoproductions")]
        public Task<ListResponse<VideoProductionSummary>> GetVideoProductionsAsync()
        {
            return _manager.GetVideoProductionsForOrgAsync(OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader());
        }

        [HttpPost("/api/media/videoproduction")]
        public Task<InvokeResult> AddVideoProductionAsync([FromBody] VideoProduction production)
        {
            SetAuditProperties(production);
            return _manager.AddVideoProductionAsync(production, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videoproduction")]
        public Task<InvokeResult> UpdateVideoProductionAsync([FromBody] VideoProduction production)
        {
            SetUpdatedProperties(production);
            return _manager.UpdateVideoProductionAsync(production, OrgEntityHeader, UserEntityHeader);
        }

        [HttpDelete("/api/media/videoproduction/{id}")]
        public Task<InvokeResult> DeleteVideoProductionAsync(string id)
        {
            return _manager.DeleteVideoProductionAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoproduction/{id}/cost/estimate")]
        public Task<InvokeResult<VideoProduction>> EstimateVideoProductionCostAsync(string id)
        {
            return _manager.EstimateVideoProductionCostAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoproduction/{id}/preview/audio")]
        public Task<InvokeResult<VideoProduction>> GeneratePreviewAudioAsync(string id)
        {
            return _manager.GeneratePreviewAudioAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoproduction/{id}/submit")]
        public Task<InvokeResult<VideoProduction>> SubmitVideoProductionAsync(string id)
        {
            return _manager.SubmitVideoProductionAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoproduction/{id}/status")]
        public Task<InvokeResult<VideoProduction>> RefreshVideoProductionStatusAsync(string id)
        {
            return _manager.RefreshVideoProductionStatusAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}