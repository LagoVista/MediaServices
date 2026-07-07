using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaSystemDefaultIconGenerationManager
    {
        Task<InvokeResult<LagoVistaIconPublishResult>> GenerateDefaultIconAsync(string entityTypeName, LagoVistaDefaultIconGenerationRequest request, EntityHeader org, EntityHeader user);
    }
}
