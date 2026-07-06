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
        public LagoVistaIconGenerationUsage Usage { get; set; }
    }

    public class LagoVistaIconGenerationUsage
    {
        public string Provider { get; set; }
        public string Model { get; set; }
        public string Operation { get; set; }
        public int? InputTokenCount { get; set; }
        public int? OutputTokenCount { get; set; }
        public int? TotalTokenCount { get; set; }
        public int? ImageCount { get; set; }
        public string ImageSize { get; set; }
        public string ImageQuality { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string Currency { get; set; }
        public string BillingProductId { get; set; }
        public string BillingProductKey { get; set; }
        public string BillingProductName { get; set; }
        public string ProviderUsageJson { get; set; }
        public string CostAccountingJson { get; set; }
    }
}
