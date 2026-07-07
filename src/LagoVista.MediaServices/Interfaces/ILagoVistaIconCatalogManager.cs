using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaIconCatalogManager
    {
        Task<InvokeResult<LagoVistaIconMasterCatalogDocument>> GetMasterCatalogAsync(string orgNamespace);
        Task<InvokeResult<LagoVistaIconCatalogDocument>> GetFamilyCatalogAsync(string orgNamespace, string familyKey);
    }
}
