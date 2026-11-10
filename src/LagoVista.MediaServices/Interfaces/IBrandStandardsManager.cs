using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IBrandStandardsManager
    {
        Task<InvokeResult> AddBrandStandardsAsync(BrandStandards brandStandards, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateBrandStandardsAsync(BrandStandards brandStandards, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteBrandStandardsAsync(string id, EntityHeader org, EntityHeader user);
        Task<BrandStandards> GetBrandStandardsAsync(string id, EntityHeader org, EntityHeader user);
        Task<BrandStandards> GetBrandStandardsByKeyAsync(string key, EntityHeader org, EntityHeader user);
        Task<ListResponse<BrandStandardsSummary>> GetBrandStandardsForOrgAsync(string orgId, ListRequest listRequest, EntityHeader user);
        Task<DependentObjectCheckResult> CheckBrandStandardsInUseAsync(string id, EntityHeader org, EntityHeader user);
        Task<bool> QueryBrandStandardsKeyInUseAsync(string key, EntityHeader org, EntityHeader user);
    }
}
