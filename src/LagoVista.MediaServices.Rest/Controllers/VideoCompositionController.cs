using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using LagoVista.VideoAssembly.Contracts;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IMediaLibraryRepo _mediaLibraryRepo;

        public VideoCompositionController(IVideoCompositionManager manager, IVideoAssemblyRequestManager videoAssemblyRequestManager, IMediaLibraryRepo mediaLibraryRepo, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _videoAssemblyRequestManager = videoAssemblyRequestManager ?? throw new ArgumentNullException(nameof(videoAssemblyRequestManager));
            _mediaLibraryRepo = mediaLibraryRepo ?? throw new ArgumentNullException(nameof(mediaLibraryRepo));
        }

        [HttpGet("/api/media/videocomposition/factory")]
        public async Task<DetailResponse<VideoComposition>> CreateVideoCompositionAsync()
        {
            var form = DetailResponse<VideoComposition>.Create();
            form.Model.Name = $"Video Composition {DateTime.Now}";
            form.Model.Key = "vc" + Guid.NewGuid().ToId().Value.ToLower();
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);

            var defaultLibrary = await _mediaLibraryRepo.GetMediaLibraryByKeyAsync(OrgEntityHeader.Id, "publishedvideo");
            form.Model.OutputMediaLibrary = defaultLibrary?.ToEntityHeader();

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

        [HttpPost("/api/media/videocomposition/{id}/assemble")]
        public Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareAssemblyRequestAsync(string id, [FromQuery] double? thumbnailTimeSeconds = null, CancellationToken cancellationToken = default)
        {
            return _videoAssemblyRequestManager.PrepareAssemblyRequestAsync(id, thumbnailTimeSeconds, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/videocomposition/{id}/vimeo")]
        public Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareVimeoPublishRequestAsync(string id, CancellationToken cancellationToken = default)
        {
            return _videoAssemblyRequestManager.PrepareVimeoPublishRequestAsync(id, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [AllowAnonymous]
        [HttpPost("/api/media/videocomposition/vimeo/session")]
        public async Task<IActionResult> CreateVimeoUploadSessionAsync([FromBody] VideoAssemblyVimeoSessionRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _videoAssemblyRequestManager.CreateVimeoUploadSessionAsync(request, GetBearerToken(Request.Headers["Authorization"].ToString()), cancellationToken);
            if (!result.Successful) return BadRequest(result);
            return Ok(result.Result);
        }

        [HttpDelete("/api/media/videocomposition/{id}")]
        public Task<InvokeResult> DeleteVideoCompositionAsync(string id)
        {
            return _manager.DeleteVideoCompositionAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        private static string GetBearerToken(string authorizationHeader)
        {
            const string bearerPrefix = "Bearer ";
            if (String.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)) return null;

            var accessToken = authorizationHeader.Substring(bearerPrefix.Length).Trim();
            return String.IsNullOrWhiteSpace(accessToken) ? null : accessToken;
        }
    }
}
