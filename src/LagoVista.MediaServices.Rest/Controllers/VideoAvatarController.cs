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
    public class VideoAvatarController : LagoVistaBaseController
    {
        private readonly IVideoAvatarManager _manager;

        public VideoAvatarController(IVideoAvatarManager manager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        [HttpGet("/api/media/videoavatar/factory")]
        public DetailResponse<VideoAvatar> CreateVideoAvatar()
        {
            var form = DetailResponse<VideoAvatar>.Create();
            SetAuditProperties(form.Model);
            SetOwnedProperties(form.Model);
            return form;
        }

        [HttpGet("/api/media/videoavatar/{id}")]
        public async Task<DetailResponse<VideoAvatar>> GetVideoAvatarAsync(string id)
        {
            var avatar = await _manager.GetVideoAvatarAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<VideoAvatar>.Create(avatar);
        }

        [HttpGet("/api/media/videoavatars")]
        public Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarsAsync()
        {
            return _manager.GetVideoAvatarsForOrgAsync(OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader());
        }

        [HttpPost("/api/media/videoavatar")]
        public Task<InvokeResult> AddVideoAvatarAsync([FromBody] VideoAvatar avatar)
        {
            SetAuditProperties(avatar);
            return _manager.AddVideoAvatarAsync(avatar, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/videoavatar")]
        public Task<InvokeResult> UpdateVideoAvatarAsync([FromBody] VideoAvatar avatar)
        {
            SetUpdatedProperties(avatar);
            return _manager.UpdateVideoAvatarAsync(avatar, OrgEntityHeader, UserEntityHeader);
        }

        [HttpDelete("/api/media/videoavatar/{id}")]
        public Task<InvokeResult> DeleteVideoAvatarAsync(string id)
        {
            return _manager.DeleteVideoAvatarAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoavatar/{id}/provider/ensure")]
        public Task<InvokeResult<VideoAvatar>> EnsureProviderAvatarAsync(string id)
        {
            return _manager.EnsureProviderAvatarAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/videoavatar/{id}/provider/status")]
        public Task<InvokeResult<VideoAvatar>> RefreshProviderAvatarStatusAsync(string id)
        {
            return _manager.RefreshProviderAvatarStatusAsync(id, OrgEntityHeader, UserEntityHeader);
        }
    }
}