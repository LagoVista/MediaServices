using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.IO;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesManager
    {
        Task<InvokeResult<MediaResource>> AddResourceMediaAsync(string id, Stream media, string contentType, EntityHeader org, EntityHeader user);

        Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user);
    }
}
