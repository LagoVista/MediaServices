using System;

namespace LagoVista.MediaServices.Models.Icons
{
    public class LagoVistaIconGeneratedImage
    {
        public LagoVistaIconGeneratedImage()
        {
            GeneratedUtc = DateTime.UtcNow.ToString("o");
            ContentType = "image/webp";
            OutputFormat = "webp";
        }

        public byte[] ImageData { get; set; }
        public string ContentType { get; set; }
        public string OutputFormat { get; set; }
        public string Provider { get; set; }
        public string ProviderResponseId { get; set; }
        public string Model { get; set; }
        public string RevisedPrompt { get; set; }
        public string GeneratedUtc { get; set; }
    }
}
