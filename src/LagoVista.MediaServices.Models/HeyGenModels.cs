using Newtonsoft.Json;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    public static class HeyGenWebhookConstants
    {
        public const string RegistrationSecretId = "4A6F8F95D0914DB7B76A183B395D58CD";

        public const string EventVideoSuccess = "avatar_video.success";
        public const string EventVideoFail = "avatar_video.fail";

        public const string RegistrationLockKey = "media-services:heygen:webhook-registration";
        public const string RegistrationIdempotencyKey = "media-services:heygen:webhook-registration:v1";
    }

    public sealed class HeyGenWebhookRegistration
    {
        public string EndpointId { get; set; }

        public string SigningSecret { get; set; }

        public string CallbackUrl { get; set; }

        public List<string> Events { get; set; } = new List<string>();
    }

}