using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IImageAssemblyManager
    {
        Task<InvokeResult<ImageAssemblyStatus>> CreateAsync(int width, int height, EntityHeader org);
        Task<InvokeResult<ImageAssemblyStatus>> AppendAsync(string id, int x, int y, string imageData, EntityHeader org);
        Task<InvokeResult<MediaResource>> PublishAsync(string id, string name, string mediaLibraryId, bool isPublic, EntityHeader org, EntityHeader user);
    }

    public class ImageAssemblyStatus
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Blocks { get; set; }
        public bool Complete { get; set; }
    }
}
