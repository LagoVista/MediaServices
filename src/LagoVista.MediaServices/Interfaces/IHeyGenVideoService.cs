using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IHeyGenVideoService
    {
        Task<InvokeResult<HeyGenAssetUploadResult>> UploadAssetAsync(Stream stream, string fileName, string contentType, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenVideoSubmission>> SubmitVideoAsync(HeyGenVideoRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenAvatarCreationResult>> CreatePhotoAvatarAsync(HeyGenPhotoAvatarRequest request, CancellationToken cancellationToken = default);
        Task<InvokeResult<HeyGenAvatarStatusResult>> GetAvatarStatusAsync(string avatarId, CancellationToken cancellationToken = default);
    }
}
