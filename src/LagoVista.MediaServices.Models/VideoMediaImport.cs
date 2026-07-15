using LagoVista.Core.Models;
using LagoVista.VideoAssembly.Contracts;

namespace LagoVista.MediaServices.Models
{
    public sealed class VideoMediaImportPreparationResult
    {
        public VideoProduction Production { get; set; }

        public MediaResource MediaResource { get; set; }

        public VideoMediaImportRequest Request { get; set; }
    }

    public sealed class VideoMediaImportCallback
    {
        public string RequestId { get; set; }

        public string ProductionId { get; set; }

        public string MediaResourceId { get; set; }

        public string Stage { get; set; }

        public int? PercentComplete { get; set; }

        public string Message { get; set; }

        public long? BytesCompleted { get; set; }

        public long? BytesTotal { get; set; }

        public int? DurationSeconds { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public long? ContentSize { get; set; }

        public string CompletedUtc { get; set; }

        public string ErrorMessage { get; set; }
    }
}
