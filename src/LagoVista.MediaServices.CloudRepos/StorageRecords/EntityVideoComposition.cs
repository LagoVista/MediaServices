using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;

namespace LagoVista.MediaServices.CloudRepos.StorageRecords
{
    /// <summary>
    /// Durable Application Data record owned by the entity video composition feature.
    /// Rich source content remains on the source EntityBase and is loaded explicitly only
    /// when composition generation/synchronization requires it.
    /// </summary>
    internal sealed class EntityVideoComposition : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }

        public string EntityType { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public EntityVideoCompositionInfo VideoCompositionInfo { get; set; }
    }
}
