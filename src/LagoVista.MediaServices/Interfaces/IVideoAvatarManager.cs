using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoAvatarManager
    {
        Task<InvokeResult<VideoAvatar>> AddVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoAvatar>> UpdateVideoAvatarAsync(VideoAvatar avatar, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoAvatar>> DeleteFailedVideoAvatarLookAsync(string id, string lookId, EntityHeader org, EntityHeader user);
        Task<VideoAvatar> GetVideoAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<InvokeResult<VideoAvatar>> EnsureProviderAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoAvatar>> RefreshProviderAvatarStatusAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoAvatar>> ReconcileProviderAvatarAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoAvatar> UpdateVideoAvatarProviderStateAsync(string id, VideoAvatarProviderState state);
    }

    public sealed class VideoAvatarProviderState
    {
        public string ProviderAssetId { get; set; }
        public string ProviderAvatarId { get; set; }
        public string ProviderAvatarStatus { get; set; }
        public EntityHeader<VideoAvatarStatus> Status { get; set; }
        public string ErrorMessage { get; set; }
        public UtcTimestamp? LastStatusCheck { get; set; }
    }
}