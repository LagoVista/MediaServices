using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaIconPromptBuilder
    {
        InvokeResult<string> BuildPrompt(LagoVistaIconGenerationRequest request, LagoVistaIconStyleProfile profile);
    }

    public interface ILagoVistaIconStyleProfileProvider
    {
        InvokeResult<LagoVistaIconStyleProfile> GetProfile(string styleProfileKey);
    }
}
