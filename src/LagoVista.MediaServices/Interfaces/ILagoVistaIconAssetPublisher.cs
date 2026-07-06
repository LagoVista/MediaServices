using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaIconAssetPublisher
    {
        Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync(LagoVistaIconAssetPublishRequest publishRequest);
    }
}
