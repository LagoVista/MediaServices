using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAssemblyCallbackHandler
    {
        Task<InvokeResult<VideoComposition>> ApplyAsync(VideoProcessorJobCallback callback, string accessToken, CancellationToken cancellationToken = default);
    }
}
