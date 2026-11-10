using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IBrandStandardsRepo
    {
        Task AddBrandStandardsAsync(BrandStandards brandStandards);
        Task UpdateBrandStandardsAsync(BrandStandards brandStandards);
        Task DeleteBrandStandardsAsync(string id);
        Task<BrandStandards> GetBrandStandardsAsync(string id);
        Task<BrandStandards> GetBrandStandardsByKeyAsync(string orgId, string key);
        Task<ListResponse<BrandStandardsSummary>> GetBrandStandardsForOrgAsync(string orgId, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, string orgId, string excludingId = null);
    }
}
