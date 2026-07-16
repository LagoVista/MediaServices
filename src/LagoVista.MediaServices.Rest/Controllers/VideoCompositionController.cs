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
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ConfirmedUser]
    public class VideoCompositionController : LagoVistaBaseController
    {
        private readonly IVideoCompositionManager _manager;
        private readonly IVideoAssemblyRequestManager _videoAssemblyRequestManager;

        public VideoCompositionController(IVideoCompositionManager manager, IVideoAssemblyRequestManager videoAssemblyRequestManager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _videoAssemblyRequestManager = videoAssemblyRequestManager ?? throw new ArgumentNullException(nameof(videoAssemblyRequestManager));
        }

        [HttpGet("/api/media/videocomposition/factory")]
        public DetailResponse<VideoComposition> CreateVideoComposition()
        {
            var form = DetailResponse<VideoComposition>.Create();
            form.Model.Name = $"Video Composition {DateTime.Now}";
            form.Model.Key = "vc" + Guid.NewGuid().ToId().Value.ToLower();
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);
            return form;
        }

        [HttpGet("/api/media/videocomposition/block/factory")]
        public DetailResponse<VideoCompositionBlock> CreateVideoCompositionBlock()
        {
            return DetailResponse<VideoCompositionBlock>.Create(new VideoCompositionBlock
            {
                Key = "block" + Guid.NewGuid().ToId().Value.Substring(0, 6).ToLower(),
                Type = VideoCompositionBlockType.Image,
                DurationSeconds = 5,
                FadeInSeconds = 0,
                FadeOutSeconds = 0
            });
        }

        [HttpGet("/api/media/videocomposition/label/factory")]
        public DetailResponse<VideoCompositionTextLabel> CreateVideoCompositionTextLabel()
        {
            return DetailResponse<VideoCompositionTextLabel>.Create(new VideoCompositionTextLabel
            {
                FontSize = 48,
                Color = "#FFFFFF",
                Alignment = VideoCompositionTextAlignment.Left
            });
        }

        [HttpGet("/api/media/videocomposition/{id}")]
        public async Task<DetailResponse<VideoComposition>> GetVideoCompositionAsync(string id)
        {
            var composition = await _manager.GetVideoCompositionAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<VideoComposition>.Create(composition);
        }

        [HttpGet("/api/media/videocompositions")]
        public Task<ListResponse<VideoCompositionSummary>> GetVideoCompositionsAsync()
        {
            return _manager.GetVideoCompositionsForOrgAsync(OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader());
        }

        [HttpGet("/api/media/videocomposition/{key}/keyinuse")]
        public Task<bool> GetVideoCompositionKeyInUseAsync(string key)
        {
            return _manager.QueryKeyInUseAsync(key, OrgEntityHeader);
        }

        [HttpPost("/api/media/videocomposition")]
        public Task<InvokeResult> AddVideoCompositionAsync([FromBody] VideoComposition composition)
        {
            return _manager.AddVideoCompositionAsync(composition, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videocomposition")]
        public Task<InvokeResult> UpdateVideoCompositionAsync([FromBody] VideoComposition composition)
        {
            SetUpdatedProperties(composition);
            return _manager.UpdateVideoCompositionAsync(composition, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videocomposition/{id}/assembly/prepare")]
        public Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareAssemblyRequestAsync(string id, [FromQuery] double? thumbnailTimeSeconds = null, CancellationToken cancellationToken = default)
        {
            return _videoAssemblyRequestManager.PrepareAssemblyRequestAsync(id, thumbnailTimeSeconds, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpDelete("/api/media/videocomposition/{id}")]
        public Task<InvokeResult> DeleteVideoCompositionAsync(string id)
        {
            return _manager.DeleteVideoCompositionAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}
