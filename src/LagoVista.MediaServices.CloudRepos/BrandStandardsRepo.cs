using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class BrandStandardsRepo : DocumentDBRepoBase<BrandStandards>, IBrandStandardsRepo
    {
        public BrandStandardsRepo(IDocumentCloudCachedServices services) : base(services)
        {
        }

        public Task AddBrandStandardsAsync(BrandStandards brandStandards)
        {
            return CreateDocumentAsync(brandStandards);
        }

        public Task UpdateBrandStandardsAsync(BrandStandards brandStandards)
        {
            return UpsertDocumentAsync(brandStandards);
        }

        public Task DeleteBrandStandardsAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<BrandStandards> GetBrandStandardsAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public async Task<BrandStandards> GetBrandStandardsByKeyAsync(string orgId, string key)
        {
            var items = await QueryAsync(family =>
                (family.OwnerOrganization.Id == orgId || family.IsPublic == true) && family.Key == key);
            return items.FirstOrDefault(family => family.OwnerOrganization.Id == orgId) ?? items.FirstOrDefault();
        }

        public Task<ListResponse<BrandStandardsSummary>> GetBrandStandardsForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<BrandStandardsSummary, BrandStandards>(
                family => family.OwnerOrganization.Id == orgId || family.IsPublic == true,
                family => family.Name, listRequest);
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId, string excludingId = null)
        {
            var items = await QueryAsync(family =>
                (family.OwnerOrganization.Id == orgId || family.IsPublic == true) &&
                family.Key == key && family.Id != excludingId);
            return items.Any();
        }
    }
}
