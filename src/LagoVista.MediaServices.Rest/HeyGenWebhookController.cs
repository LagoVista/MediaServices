using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [AllowAnonymous]
    [ApiController]
    public sealed class HeyGenWebhookController : ControllerBase
    {
        private readonly IHeyGenVideoService _heyGenVideoService;
        private readonly IVideoProductionManager _videoProductionManager;
        private readonly EntityHeader _secretOwner;

        public HeyGenWebhookController(IHeyGenVideoService heyGenVideoService, IVideoProductionManager videoProductionManager, ICoreAppServices coreAppServices)
        {
            _heyGenVideoService = heyGenVideoService ?? throw new ArgumentNullException(nameof(heyGenVideoService));
            _videoProductionManager = videoProductionManager ?? throw new ArgumentNullException(nameof(videoProductionManager));
            _secretOwner = coreAppServices?.AppConfig?.SystemOwnerOrg ?? throw new ArgumentNullException(nameof(coreAppServices.AppConfig.SystemOwnerOrg));
        }

        [HttpPost("/api/media/webhooks/heygen")]
        public async Task<IActionResult> ProcessAsync(CancellationToken cancellationToken)
        {
            string rawPayload;

            using (var reader = new StreamReader(Request.Body))
            {
                rawPayload = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["Heygen-Signature"].ToString();
            var timestamp = Request.Headers["Heygen-Timestamp"].ToString();
            var deliveryEventId = Request.Headers["Heygen-Event-Id"].ToString();

            var validationResult = await _heyGenVideoService.ValidateWebhookSignatureAsync(_secretOwner, _secretOwner, rawPayload, signature, timestamp, cancellationToken);

            if (!validationResult.Successful)
            {
                return Unauthorized();
            }

            HeyGenWebhookEvent webhookEvent;

            try
            {
                webhookEvent = JsonConvert.DeserializeObject<HeyGenWebhookEvent>(rawPayload);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

            if (webhookEvent == null)
            {
                return BadRequest();
            }

            if (String.IsNullOrWhiteSpace(webhookEvent.EventId))
            {
                webhookEvent.EventId = deliveryEventId;
            }

            if (!String.IsNullOrWhiteSpace(deliveryEventId) &&
                !String.IsNullOrWhiteSpace(webhookEvent.EventId) &&
                !String.Equals(deliveryEventId, webhookEvent.EventId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var processResult = await _videoProductionManager.ProcessHeyGenWebhookAsync(webhookEvent, cancellationToken);

            if (!processResult.Successful)
            {
                return StatusCode(500);
            }

            return Ok();
        }
    }
}