using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;

namespace LagoVista.MediaServices.CloudRepos.StorageRecords
{
    /// <summary>
    /// Durable Application Data state that binds a rich source entity to its
    /// video-composition workflow. The source entity remains authoritative for
    /// its name, key, content and authorization metadata.
    /// </summary>
    internal sealed class EntityVideoComposition : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }

        public string EntityType { get; set; }
        public EntityVideoCompositionInfo VideoCompositionInfo { get; set; }
    }
}
