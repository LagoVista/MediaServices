using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionBlockTemplateRepo
    {
        Task AddVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template);
        Task UpdateVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template);
        Task DeleteVideoCompositionBlockTemplateAsync(string id);
        Task<VideoCompositionBlockTemplate> GetVideoCompositionBlockTemplateAsync(string id);
        Task<ListResponse<VideoCompositionBlockTemplateSummary>> GetVideoCompositionBlockTemplateSummariesForOrgAsync(string orgId, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
    }
}
