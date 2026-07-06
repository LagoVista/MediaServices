using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaIconGenerationManager
    {
        InvokeResult<string> BuildPrompt(LagoVistaIconGenerationRequest request);
        Task<InvokeResult<LagoVistaIconPublishResult>> GenerateAsync(LagoVistaIconGenerationRequest request, EntityHeader org, EntityHeader user);
        Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync(LagoVistaIconAssetPublishRequest publishRequest, EntityHeader org, EntityHeader user);
    }
}
