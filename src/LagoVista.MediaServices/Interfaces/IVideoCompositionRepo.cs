using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionRepo
    {
        Task AddVideoCompositionAsync(VideoComposition composition);
        Task UpdateVideoCompositionAsync(VideoComposition composition);
        Task DeleteVideoCompositionAsync(string id);
        Task<VideoComposition> GetVideoCompositionAsync(string id);
        Task<ListResponse<VideoCompositionSummary>> GetVideoCompositionSummariesForOrgAsync(string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoComposition>> GetFullVideoCompositionsForOrgAsync(string orgId);
        Task<IEnumerable<VideoComposition>> GetVideoCompositionsUsingMediaResourceAsync(string mediaResourceId, string orgId);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
    }
}
