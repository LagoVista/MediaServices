using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    /// <summary>
    /// Session-opening state for the persistent Image Family Studio MCP App.
    /// The ImageFamily record remains the source of truth; this is not a
    /// second persisted workflow model.
    /// </summary>
    public class MediaImageFamilyStudioOpen
    {
        public string InitialFamilyId { get; set; }
        public ImageFamily Family { get; set; }
        public List<MediaImageCreationFamily> Families { get; set; } = new List<MediaImageCreationFamily>();
        public List<MediaGalleryLibrary> Libraries { get; set; } = new List<MediaGalleryLibrary>();
    }
}
