using LagoVista.Core.Models;
using LagoVista.VideoAssembly.Contracts;

namespace LagoVista.MediaServices.Models
{
    public sealed class VideoMediaImportPreparationResult
    {
        public VideoProduction Production { get; set; }

        public MediaResource MediaResource { get; set; }

        public VideoMediaImportRequest Request { get; set; }

        public string AttemptId { get; set; }

        public string RequestStorageReferenceName { get; set; }

        public string RequestBlobUrl { get; set; }

        public string RequestUrl { get; set; }
    }

}
