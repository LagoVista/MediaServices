using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IImageFamilyManager
    {
        Task<InvokeResult> AddImageFamilyAsync(ImageFamily imageFamily, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateImageFamilyAsync(ImageFamily imageFamily, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteImageFamilyAsync(string id, EntityHeader org, EntityHeader user);
        Task<ImageFamily> GetImageFamilyAsync(string id, EntityHeader org, EntityHeader user);
        Task<ImageFamily> GetImageFamilyByKeyAsync(string key, EntityHeader org, EntityHeader user);
        Task<ListResponse<ImageFamilySummary>> GetImageFamiliesForOrgAsync(string orgId, ListRequest listRequest, EntityHeader user);
        Task<DependentObjectCheckResult> CheckImageFamilyInUseAsync(string id, EntityHeader org, EntityHeader user);
        Task<bool> QueryImageFamilyKeyInUseAsync(string key, EntityHeader org, EntityHeader user);
    }
}
