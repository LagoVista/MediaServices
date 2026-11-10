using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IImageFamilyRepo
    {
        Task AddImageFamilyAsync(ImageFamily imageFamily);
        Task UpdateImageFamilyAsync(ImageFamily imageFamily);
        Task DeleteImageFamilyAsync(string id);
        Task<ImageFamily> GetImageFamilyAsync(string id);
        Task<ImageFamily> GetImageFamilyByKeyAsync(string orgId, string key);
        Task<ListResponse<ImageFamilySummary>> GetImageFamiliesForOrgAsync(string orgId, ListRequest listRequest);
        Task<bool> QueryKeyInUseAsync(string key, string orgId, string excludingId = null);
    }
}
