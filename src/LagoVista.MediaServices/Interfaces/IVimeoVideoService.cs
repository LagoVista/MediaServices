using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVimeoVideoService
    {
        Task<InvokeResult<VimeoVideo>> CreatePullUploadAsync(string accessToken, VimeoPullUploadRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<VimeoVideo>> CreateTusUploadAsync(string accessToken, VimeoTusUploadRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<VimeoVideo>> GetVideoAsync(string accessToken, string videoUri, CancellationToken cancellationToken = default);
        Task<InvokeResult> AddVideoToFolderAsync(string videoUri, string folderUri, string accessToken, CancellationToken cancellationToken = default);
    }
}