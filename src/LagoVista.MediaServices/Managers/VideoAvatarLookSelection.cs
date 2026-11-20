using LagoVista.MediaServices.Models;
using System;
using System.Linq;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Resolves the video producer's intended Look. An explicit selection must never
    /// silently fall back to the primary Look if it becomes inactive or unavailable.
    /// </summary>
    public static class VideoAvatarLookSelection
    {
        public static VideoAvatarLook Resolve(VideoAvatar avatar, string selectedLookId)
        {
            var active = avatar?.Looks?.Where(look => look != null && look.IsActive).ToList();
            if (active == null || active.Count == 0)
                return null;

            if (!String.IsNullOrWhiteSpace(selectedLookId))
                return active.FirstOrDefault(look => String.Equals(look.Id, selectedLookId, StringComparison.OrdinalIgnoreCase));

            return active.FirstOrDefault(look => look.IsPrimary) ?? active.First();
        }

        public static bool RequiresNewRender(VideoProduction production, string selectedLookId)
        {
            return production != null &&
                   !String.IsNullOrWhiteSpace(selectedLookId) &&
                   !String.Equals(production.VideoAvatarLookId, selectedLookId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
