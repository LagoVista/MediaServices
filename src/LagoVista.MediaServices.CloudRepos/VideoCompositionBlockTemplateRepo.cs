using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class VideoCompositionBlockTemplateRepo : DocumentDBRepoBase<VideoCompositionBlockTemplate>, IVideoCompositionBlockTemplateRepo
    {
        public VideoCompositionBlockTemplateRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
        }

        public Task AddVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template)
        {
            return CreateDocumentAsync(template);
        }

        public Task UpdateVideoCompositionBlockTemplateAsync(VideoCompositionBlockTemplate template)
        {
            return UpsertDocumentAsync(template);
        }

        public Task DeleteVideoCompositionBlockTemplateAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<VideoCompositionBlockTemplate> GetVideoCompositionBlockTemplateAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task<ListResponse<VideoCompositionBlockTemplateSummary>> GetVideoCompositionBlockTemplateSummariesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoCompositionBlockTemplateSummary, VideoCompositionBlockTemplate>(qry => qry.IsPublic || qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            return (await QueryAsync(template => template.Key == key && (template.OwnerOrganization.Id == orgId || template.IsPublic))).Any();
        }
    }
}
