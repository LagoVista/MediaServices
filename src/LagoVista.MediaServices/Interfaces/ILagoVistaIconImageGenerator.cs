using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaIconImageGenerator
    {
        Task<InvokeResult<LagoVistaIconGeneratedImage>> GenerateAsync(LagoVistaIconGenerationRequest request, LagoVistaIconStyleProfile profile, string prompt);
    }
}
