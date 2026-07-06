using System;

namespace LagoVista.MediaServices.Models.Icons
{
    public class LagoVistaIconAssetPublishRequest
    {
        public LagoVistaIconAssetPublishRequest()
        {
            GeneratedUtc = DateTime.UtcNow.ToString("o");
        }

        public LagoVistaIconGenerationRequest GenerationRequest { get; set; }
        public LagoVistaIconStyleProfile StyleProfile { get; set; }
        public byte[] SourceImageData { get; set; }
        public string SourceContentType { get; set; }
        public string SourceOutputFormat { get; set; }
        public string Prompt { get; set; }
        public string RevisedPrompt { get; set; }
        public string Provider { get; set; }
        public string ProviderResponseId { get; set; }
        public string Model { get; set; }
        public string GeneratedUtc { get; set; }
        public string CdnBaseUrl { get; set; }
    }
}
