using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

            var signature = Request.Headers["Signature"].ToString();

            if (String.IsNullOrWhiteSpace(signature))
            {
                signature = Request.Headers["Heygen-Signature"].ToString();
            }

            var timestamp = Request.Headers["Heygen-Timestamp"].ToString();
            var deliveryEventId = Request.Headers["Heygen-Event-Id"].ToString();

            var validationResult = await _heyGenVideoService.ValidateWebhookSignatureAsync(
                _secretOwner,
                _secretOwner,
                rawPayload,
                signature,
                timestamp,
                cancellationToken);

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

            if (String.IsNullOrWhiteSpace(webhookEvent.EventId))
            {
                webhookEvent.EventId = CreateDeterministicEventId(webhookEvent);
            }

            if (String.IsNullOrWhiteSpace(webhookEvent.EventId))
            {
                return BadRequest();
            }

            if (!String.IsNullOrWhiteSpace(deliveryEventId)
                && !String.Equals(deliveryEventId, webhookEvent.EventId, StringComparison.OrdinalIgnoreCase))
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

        private static string CreateDeterministicEventId(HeyGenWebhookEvent webhookEvent)
        {
            var callbackId = webhookEvent.EventData?.Value<string>("callback_id");
            var videoId = webhookEvent.EventData?.Value<string>("video_id");

            if (String.IsNullOrWhiteSpace(webhookEvent.EventType)
                || String.IsNullOrWhiteSpace(callbackId)
                || String.IsNullOrWhiteSpace(videoId))
            {
                return null;
            }

            var source = $"{webhookEvent.EventType}:{callbackId}:{videoId}";

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(source));
            var result = new StringBuilder(hash.Length * 2);

            foreach (var value in hash)
            {
                result.Append(value.ToString("x2"));
            }

            return result.ToString();
        }
    }
}