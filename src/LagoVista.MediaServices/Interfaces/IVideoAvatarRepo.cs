using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAvatarRepo
    {
        Task AddVideoAvatarAsync(VideoAvatar avatar);
        Task<VideoAvatar> UpdateVideoAvatarAsync(VideoAvatar avatar);
        Task DeleteVideoAvatarAsync(string id);
        Task<VideoAvatar> GetVideoAvatarAsync(string id);
        Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarSummariesForOrgAsync(string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoAvatar>> GetFullVideoAvatarsForOrgAsync(string orgId);
        Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarSummariesForSubjectAsync(string subjectEntityType, string subjectEntityId, string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoAvatar>> GetFullVideoAvatarsForSubjectAsync(string subjectEntityType, string subjectEntityId, string orgId);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
        Task<VideoAvatar> UpdateVideoAvatarProviderStateAsync(string id, VideoAvatarProviderState state);

        Task ReleaseProviderCreationLockAsync(string avatarId, string token);
        Task<bool> AttemptAcquireProviderCreationLockAsync(string avatarId, string token);
    }
}
