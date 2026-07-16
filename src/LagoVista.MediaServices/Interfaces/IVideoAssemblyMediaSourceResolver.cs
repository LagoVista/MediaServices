using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAssemblyMediaSourceResolver
    {
        Task<InvokeResult<VideoAssemblySource>> ResolveAsync(MediaResource mediaResource, string orgId, CancellationToken cancellationToken = default);
    }
}
