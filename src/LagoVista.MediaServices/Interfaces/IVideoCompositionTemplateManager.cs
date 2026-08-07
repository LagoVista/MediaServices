using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoCompositionTemplateManager
    {
        Task<InvokeResult> AddVideoCompositionTemplateAsync(VideoCompositionTemplate template, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoCompositionTemplate>> CreateFromCompositionAsync(string compositionId, CreateVideoCompositionTemplateFromCompositionRequest request, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoCompositionTemplateAsync(VideoCompositionTemplate template, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoCompositionTemplateAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoCompositionTemplate> GetVideoCompositionTemplateAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoCompositionTemplateSummary>> GetVideoCompositionTemplatesForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, EntityHeader org);
    }
}
