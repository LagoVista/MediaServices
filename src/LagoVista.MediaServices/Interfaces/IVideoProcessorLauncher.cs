using LagoVista.Core.Validation;
using LagoVista.VideoAssembly.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProcessorLauncher
    {
        Task<InvokeResult<VideoProcessorLaunchResult>> LaunchAsync(VideoProcessorLaunchRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class VideoProcessorLaunchRequest
    {
        public VideoProcessorJobType JobType { get; set; }
        public string ProductionId { get; set; }
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string RequestUrl { get; set; }
    }

    public sealed class VideoProcessorLaunchResult
    {
        public string Provider { get; set; }
        public string LaunchId { get; set; }
        public string Namespace { get; set; }
        public string JobName { get; set; }
        public string LaunchedUtc { get; set; }
    }
}
