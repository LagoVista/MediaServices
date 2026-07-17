using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class VideoCompositionRepo : DocumentDBRepoBase<VideoComposition>, IVideoCompositionRepo
    {
        public VideoCompositionRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
        }

        public Task AddVideoCompositionAsync(VideoComposition composition)
        {
            composition.CurrentInputSha256 = composition.CalculateCurrentInputSha256();
            return CreateDocumentAsync(composition);
        }

        public Task UpdateVideoCompositionAsync(VideoComposition composition)
        {
            composition.CurrentInputSha256 = composition.CalculateCurrentInputSha256();
            return UpsertDocumentAsync(composition);
        }

        public Task DeleteVideoCompositionAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<VideoComposition> GetVideoCompositionAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task<ListResponse<VideoCompositionSummary>> GetVideoCompositionSummariesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoCompositionSummary, VideoComposition>(qry => qry.IsPublic || qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }

        public Task<IEnumerable<VideoComposition>> GetFullVideoCompositionsForOrgAsync(string orgId)
        {
            return QueryAsync(composition => composition.IsPublic || composition.OwnerOrganization.Id == orgId);
        }

        public Task<IEnumerable<VideoComposition>> GetVideoCompositionsUsingMediaResourceAsync(string mediaResourceId, string orgId)
        {
            return QueryAsync(composition => composition.Blocks.Any(block => block.MediaResource.Id == mediaResourceId) && (composition.IsPublic || composition.OwnerOrganization.Id == orgId));
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            return (await QueryAsync(composition => composition.Key == key && (composition.OwnerOrganization.Id == orgId || composition.IsPublic))).Any();
        }
    }
}
