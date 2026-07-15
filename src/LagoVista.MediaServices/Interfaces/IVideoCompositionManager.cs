using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionManager
    {
        Task<InvokeResult> AddVideoCompositionAsync(VideoComposition composition, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoCompositionAsync(VideoComposition composition, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoCompositionAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoComposition> GetVideoCompositionAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoCompositionSummary>> GetVideoCompositionsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, EntityHeader org);
    }
}
