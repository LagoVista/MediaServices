using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Materializes the latest standard composition template for a Trexel without changing
    /// its costly, already-produced presenter media. This is deliberately scoped to
    /// VirtualTeamMember compositions; standalone compositions keep their local blocks.
    /// Block-template references within the master remain live at assembly time.
    /// </summary>
    public static class TrexelVideoCompositionTemplateRefresh
    {
        public static string Apply(VideoComposition composition, VideoCompositionTemplate template)
        {
            if (!TrexelVideoDisclosurePolicy.AppliesTo(composition))
                return null;

            if (template == null || !template.IsActive)
                return "The Trexel's source video composition template is missing or inactive.";

            if (!String.Equals(composition.SourceCompositionTemplate?.Id, template.Id, StringComparison.OrdinalIgnoreCase))
                return "The selected template does not match this Trexel video composition.";

            var blocks = template.Blocks;
            if (blocks == null || blocks.Count != 3 ||
                blocks.Count(block => block.Role == VideoCompositionBlockRole.Intro) != 1 ||
                blocks.Count(block => block.Role == VideoCompositionBlockRole.Content) != 1 ||
                blocks.Count(block => block.Role == VideoCompositionBlockRole.CallToAction) != 1)
                return "The standard Trexel video template must have exactly one opening, presenter, and closing block.";

            var currentContent = composition.Blocks?.SingleOrDefault(block => block.Role == VideoCompositionBlockRole.Content);
            if (currentContent == null)
                return "The existing Trexel composition is missing its presenter block.";

            // Fresh design, placeholders, background and closing copy from the ONE master.
            // Keep stable composition block identities and only the produced video media.
            var freshBlocks = JsonConvert.DeserializeObject<List<VideoCompositionBlock>>(
                JsonConvert.SerializeObject(blocks));
            foreach (var newBlock in freshBlocks)
            {
                var existing = composition.Blocks?.FirstOrDefault(block => block.Role == newBlock.Role);
                if (existing != null && !String.IsNullOrWhiteSpace(existing.Id))
                    newBlock.Id = existing.Id;
            }

            var newContent = freshBlocks.Single(block => block.Role == VideoCompositionBlockRole.Content);
            newContent.MediaResource = currentContent.MediaResource;
            newContent.MediaResourceFileName = currentContent.MediaResourceFileName;
            newContent.MediaResourceMimeType = currentContent.MediaResourceMimeType;
            newContent.ThumbnailUrl = currentContent.ThumbnailUrl;
            if (!newContent.DurationSeconds.HasValue && currentContent.DurationSeconds.HasValue)
                newContent.DurationSeconds = currentContent.DurationSeconds;

            composition.Blocks = freshBlocks;
            composition.DefaultLocale = String.IsNullOrWhiteSpace(template.DefaultLocale)
                ? VideoComposition.DefaultLocaleCode : template.DefaultLocale;
            composition.BackgroundMediaResource = template.BackgroundMediaResource;
            composition.BackgroundAudioMediaResource = template.BackgroundAudioMediaResource;
            composition.BackgroundAudioVolume = template.BackgroundAudioVolume;
            composition.BackgroundAudioFadeInSeconds = template.BackgroundAudioFadeInSeconds;
            composition.BackgroundAudioFadeOutSeconds = template.BackgroundAudioFadeOutSeconds;
            composition.LoopBackgroundAudio = template.LoopBackgroundAudio;
            if (composition.OutputMediaLibrary == null)
                composition.OutputMediaLibrary = template.OutputMediaLibrary;

            composition.SourceCompositionTemplateVersion = template.Version;
            TrexelVideoDisclosurePolicy.EnsureEditableSlots(composition);
            return null;
        }
    }
}
