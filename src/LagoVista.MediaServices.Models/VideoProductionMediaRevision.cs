using System;
using System.Linq;

namespace LagoVista.MediaServices.Models
{
    /// <summary>
    /// Resolve the exact immutable video revision to the storage reference used
    /// by the video processor. A missing/unready requested revision must never
    /// fall back to the latest video.
    /// </summary>
    public static class VideoProductionMediaRevision
    {
        public static string ResolveStorageReferenceName(MediaResource resource, string revisionId = null, bool thumbnail = false)
        {
            if (resource == null)
                return null;

            if (String.IsNullOrWhiteSpace(revisionId))
            {
                var current = resource.GetCurrentRevision();
                return thumbnail
                    ? current?.ThumbnailStorageReferenceName ?? resource.ThumbnailStorageReferenceName
                    : current?.StorageReferenceName ?? resource.StorageReferenceName;
            }

            var revision = resource.History?.FirstOrDefault(candidate =>
                String.Equals(candidate?.Id, revisionId, StringComparison.OrdinalIgnoreCase));

            if (revision?.Status?.Value != MediaResourceStatus.Ready)
                return null;

            return thumbnail ? revision.ThumbnailStorageReferenceName : revision.StorageReferenceName;
        }
    }
}
