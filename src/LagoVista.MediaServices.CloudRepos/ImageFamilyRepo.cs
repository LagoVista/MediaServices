using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class ImageFamilyRepo : DocumentDBRepoBase<ImageFamily>, IImageFamilyRepo
    {
        public ImageFamilyRepo(IDocumentCloudCachedServices services) : base(services)
        {
        }

        public Task AddImageFamilyAsync(ImageFamily imageFamily)
        {
            return CreateDocumentAsync(imageFamily);
        }

        public Task UpdateImageFamilyAsync(ImageFamily imageFamily)
        {
            return UpsertDocumentAsync(imageFamily);
        }

        public Task DeleteImageFamilyAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<ImageFamily> GetImageFamilyAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public async Task<ImageFamily> GetImageFamilyByKeyAsync(string orgId, string key)
        {
            var items = await QueryAsync(family =>
                (family.OwnerOrganization.Id == orgId || family.IsPublic == true) && family.Key == key);
            return items.FirstOrDefault(family => family.OwnerOrganization.Id == orgId) ?? items.FirstOrDefault();
        }

        public Task<ListResponse<ImageFamilySummary>> GetImageFamiliesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<ImageFamilySummary, ImageFamily>(
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
