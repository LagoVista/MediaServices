using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ApiController]
    public class VideoAssemblyCallbackController : ControllerBase
    {
        private readonly IVideoAssemblyCallbackHandler _handler;

        public VideoAssemblyCallbackController(IVideoAssemblyCallbackHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        [AllowAnonymous]
        [HttpPost("/api/media/webhooks/video-assembly")]
        public Task<InvokeResult<VideoComposition>> ApplyAsync([FromBody] VideoProcessorJobCallback callback, CancellationToken cancellationToken = default)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var accessToken = GetBearerToken(authorizationHeader);
            return _handler.ApplyAsync(callback, accessToken, cancellationToken);
        }

        private static string GetBearerToken(string authorizationHeader)
        {
            const string bearerPrefix = "Bearer ";

            if (String.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var accessToken = authorizationHeader.Substring(bearerPrefix.Length).Trim();
            return String.IsNullOrWhiteSpace(accessToken) ? null : accessToken;
        }
    }
}
