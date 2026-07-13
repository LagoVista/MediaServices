using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAvatarManager
    {
        Task<InvokeResult> AddVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoAvatar> GetVideoAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<InvokeResult<VideoAvatar>> EnsureProviderAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoAvatar>> RefreshProviderAvatarStatusAsync(string id, EntityHeader org, EntityHeader user);
    }
}
