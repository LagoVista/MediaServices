using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class VideoCompositionTemplateRepo : DocumentDBRepoBase<VideoCompositionTemplate>, IVideoCompositionTemplateRepo
    {
        public VideoCompositionTemplateRepo(IDocumentCloudCachedServices services) : base(services)
        {
        }

        public Task AddVideoCompositionTemplateAsync(VideoCompositionTemplate template)
        {
            return CreateDocumentAsync(template);
        }

        public Task UpdateVideoCompositionTemplateAsync(VideoCompositionTemplate template)
        {
            return UpsertDocumentAsync(template);
        }

        public Task DeleteVideoCompositionTemplateAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<VideoCompositionTemplate> GetVideoCompositionTemplateAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task<ListResponse<VideoCompositionTemplateSummary>> GetVideoCompositionTemplateSummariesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoCompositionTemplateSummary, VideoCompositionTemplate>(qry => qry.IsPublic || qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }

        public Task<IEnumerable<VideoCompositionTemplate>> GetFullVideoCompositionTemplatesForOrgAsync(string orgId)
        {
            return QueryAsync(template => template.IsPublic || template.OwnerOrganization.Id == orgId);
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            return (await QueryAsync(template => template.Key == key && (template.OwnerOrganization.Id == orgId || template.IsPublic))).Any();
        }
    }
}
