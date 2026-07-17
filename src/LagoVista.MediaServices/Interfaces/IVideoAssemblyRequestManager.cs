using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAssemblyRequestManager
    {
        Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareAssemblyRequestAsync(string compositionId, double? thumbnailTimeSeconds, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
        Task<InvokeResult<VideoAssemblyPreparationResult>> PrepareVimeoPublishRequestAsync(string compositionId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default);
    }

    public sealed class VideoAssemblyPreparationResult
    {
        public VideoComposition Composition { get; set; }
        public MediaResource OutputMediaResource { get; set; }
        public VideoAssemblyRequest Request { get; set; }
        public string RequestStorageReferenceName { get; set; }
        public string RequestBlobUrl { get; set; }
        public string RequestUrl { get; set; }
    }
}
