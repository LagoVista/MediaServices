using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ConfirmedUser]
    public class VideoProviderController : LagoVistaBaseController
    {
        private readonly IHeyGenVideoService _heyGenVideoService;

        public VideoProviderController(IHeyGenVideoService heyGenVideoService, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _heyGenVideoService = heyGenVideoService ?? throw new ArgumentNullException(nameof(heyGenVideoService));
        }

        [HttpGet("/api/media/video/providers/heygen/voices")]
        public Task<InvokeResult<HeyGenVoiceListResult>> GetHeyGenVoicesAsync([FromQuery] string language, [FromQuery] string gender, [FromQuery] string type, [FromQuery] string pageToken)
        {
            var request = new HeyGenVoiceListRequest
            {
                Engine = "starfish",
                Language = language,
                Gender = gender,
                Type = type,
                Token = pageToken,
                Limit = 100
            };

            return _heyGenVideoService.GetVoicesAsync(request);
        }

        [HttpPost("/api/media/video/providers/heygen/speech/preview")]
        public Task<InvokeResult<HeyGenSpeechPreviewResult>> GenerateHeyGenSpeechPreviewAsync([FromBody] HeyGenSpeechPreviewRequest request)
        {
            return _heyGenVideoService.GenerateSpeechPreviewAsync(request);
        }
    }
}