using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionBlockTemplateManager
    {
        Task<InvokeResult> AddVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoCompositionBlockTemplateFromBlockAsync(string id, VideoCompositionBlock block, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoCompositionBlockTemplateAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoCompositionBlockTemplate> GetVideoCompositionBlockTemplateAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoCompositionBlockTemplateSummary>> GetVideoCompositionBlockTemplatesForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, EntityHeader org);
        Task<DetailResponse<VideoCompositionBlockTemplate>> CreateTemplateFromBlockAsync(CreateVideoCompositionBlockTemplateRequest request, EntityHeader org, EntityHeader user);
        Task<DetailResponse<VideoCompositionBlock>> CreateBlockFromTemplateAsync(string templateId, EntityHeader org, EntityHeader user);
    }
}
