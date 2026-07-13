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
    public class VideoProductionRepo : DocumentDBRepoBase<VideoProduction>, IVideoProductionRepo
    {
        public VideoProductionRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
        }

        public Task AddVideoProductionAsync(VideoProduction production)
        {
            return CreateDocumentAsync(production);
        }

        public Task DeleteVideoProductionAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<VideoProduction> GetVideoProductionAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task<ListResponse<VideoProductionSummary>> GetVideoProductionSummariesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoProductionSummary, VideoProduction>(qry => qry.IsPublic || qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }

        public Task<ListResponse<VideoProductionSummary>> GetVideoProductionSummariesForTargetAsync(string targetEntityType, string targetEntityId, string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoProductionSummary, VideoProduction>(qry => qry.TargetEntityType == targetEntityType && qry.TargetEntityId == targetEntityId && (qry.IsPublic || qry.OwnerOrganization.Id == orgId), qry => qry.Name, listRequest);
        }

        public Task<IEnumerable<VideoProduction>> GetFullVideoProductionsForOrgAsync(string orgId)
        {
            return QueryAsync(production => production.IsPublic || production.OwnerOrganization.Id == orgId);
        }

        public Task<IEnumerable<VideoProduction>> GetFullVideoProductionsForTargetAsync(string targetEntityType, string targetEntityId, string orgId)
        {
            return QueryAsync(production => production.TargetEntityType == targetEntityType && production.TargetEntityId == targetEntityId && (production.IsPublic || production.OwnerOrganization.Id == orgId));
        }

        public Task<IEnumerable<VideoProduction>> GetVideoProductionsByProviderVideoIdAsync(string providerVideoId, string orgId)
        {
            return QueryAsync(production => production.ProviderVideoId == providerVideoId && (production.IsPublic || production.OwnerOrganization.Id == orgId));
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            return (await QueryAsync(production => production.Key == key && (production.OwnerOrganization.Id == orgId || production.IsPublic))).Any();
        }

        public Task UpdateVideoProductionAsync(VideoProduction production)
        {
            return UpsertDocumentAsync(production);
        }
    }
}