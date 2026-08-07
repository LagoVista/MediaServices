using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionTemplateRepo
    {
        Task AddVideoCompositionTemplateAsync(VideoCompositionTemplate template);
        Task UpdateVideoCompositionTemplateAsync(VideoCompositionTemplate template);
        Task DeleteVideoCompositionTemplateAsync(string id);
        Task<VideoCompositionTemplate> GetVideoCompositionTemplateAsync(string id);
        Task<ListResponse<VideoCompositionTemplateSummary>> GetVideoCompositionTemplateSummariesForOrgAsync(string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoCompositionTemplate>> GetFullVideoCompositionTemplatesForOrgAsync(string orgId);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
    }
}
