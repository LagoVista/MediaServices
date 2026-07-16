using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ConfirmedUser]
    public class VideoProductionController : LagoVistaBaseController
    {
        private readonly IVideoProductionManager _manager;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly IVideoProcessorStorageUrlService _videoProcessorStorageUrlService;

        public VideoProductionController(IVideoProductionManager manager, IMediaServicesManager mediaServicesManager, IVideoProcessorStorageUrlService videoProcessorStorageUrlService, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
            _videoProcessorStorageUrlService = videoProcessorStorageUrlService ?? throw new ArgumentNullException(nameof(videoProcessorStorageUrlService));
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

        [HttpPost("/api/media/videoproduction/{id}/vimeo/publish")]
        public Task<InvokeResult<VideoProduction>> PublishVideoProductionToVimeoAsync(string id, CancellationToken cancellationToken = default)
        {
            return _manager.PublishVideoProductionToVimeoAsync(id, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/videoproduction/{id}/vimeo/refresh")]
        public Task<InvokeResult<VideoProduction>> RefreshVimeoVideoProductionStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            return _manager.RefreshVimeoVideoProductionStatusAsync(id, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpGet("/api/media/videoproduction/mediaresource/{mediaResourceId}/video")]
        public Task<IActionResult> DownloadGeneratedVideoAsync(string mediaResourceId, CancellationToken cancellationToken = default)
        {
            return RedirectToVideoProductionMediaAsync(mediaResourceId, false, cancellationToken);
        }

        [HttpGet("/api/media/videoproduction/mediaresource/{mediaResourceId}/thumbnail")]
        public Task<IActionResult> DownloadGeneratedVideoThumbnailAsync(string mediaResourceId, CancellationToken cancellationToken = default)
        {
            return RedirectToVideoProductionMediaAsync(mediaResourceId, true, cancellationToken);
        }

        private async Task<IActionResult> RedirectToVideoProductionMediaAsync(string mediaResourceId, bool thumbnail, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(mediaResourceId))
            {
                return BadRequest("Media resource ID is required.");
            }

            var mediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(mediaResourceId, OrgEntityHeader, UserEntityHeader);
            if (mediaResource == null)
            {
                return NotFound();
            }

            if (mediaResource.ResourceType?.Value != MediaResourceTypes.RawVideo &&
                mediaResource.ResourceType?.Value != MediaResourceTypes.Video)
            {
                return BadRequest($"The media resource is not a generated video-production resource, media type {mediaResource.ResourceType?.Value}.");
            }

            if (mediaResource.Status?.Value != MediaResourceStatus.Ready)
            {
                return Conflict("The generated video media resource is not ready.");
            }

            var currentRevision = mediaResource.GetCurrentRevision();
            var storageReferenceName = thumbnail
                ? currentRevision?.ThumbnailStorageReferenceName ?? mediaResource.ThumbnailStorageReferenceName
                : currentRevision?.StorageReferenceName ?? mediaResource.StorageReferenceName;

            if (String.IsNullOrWhiteSpace(storageReferenceName))
            {
                return NotFound(thumbnail ? "The generated video thumbnail is not available." : "The generated video file is not available.");
            }

            var readUrlResult = await _videoProcessorStorageUrlService.CreateReadUrlAsync(OrgEntityHeader.Id, storageReferenceName, cancellationToken);
            if (!readUrlResult.Successful)
            {
                return BadRequest(readUrlResult.Errors[0].Message);
            }

            return Redirect(readUrlResult.Result);
        }
    }
}