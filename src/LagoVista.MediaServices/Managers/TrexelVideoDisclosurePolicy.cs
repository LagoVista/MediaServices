using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Mandatory, renderable AI identity on Trexel advertisements. The text labels remain
    /// editable in the composition; the policy is checked at assembly and publication.
    /// Other MediaServices compositions are unaffected.
    /// </summary>
    public static class TrexelVideoDisclosurePolicy
    {
        public const string Version = "1";
        public const string Identity = "AI Virtual Team Member";

        // Stable, per-block label IDs are the editable slots in the composition.
        // IDs are 32-character hexadecimal strings, compatible with existing label IDs.
        public const string OpeningIdentitySlot = "A17E0000000000000000000000000001";
        public const string PresenterIdentitySlot = "A17E0000000000000000000000000002";
        public const string ClosingIdentitySlot = "A17E0000000000000000000000000003";

        public static bool AppliesTo(VideoComposition composition) =>
            composition != null &&
            String.Equals(composition.SourceEntityType, "VirtualTeamMember", StringComparison.OrdinalIgnoreCase);

        public static void EnsureEditableSlots(VideoComposition composition)
        {
            if (!AppliesTo(composition)) return;

            composition.VideoDisclosurePolicyVersion = Version;
            EnsureIdentity(composition, VideoCompositionBlockRole.Intro, OpeningIdentitySlot, 90);
            EnsureIdentity(composition, VideoCompositionBlockRole.Content, PresenterIdentitySlot, 980);
            EnsureIdentity(composition, VideoCompositionBlockRole.CallToAction, ClosingIdentitySlot, 90);
            // Marketing copy and its layout belong to the composition template.
            // Only the mandatory AI identity disclosure is managed by this policy.
        }

        /// <summary>
        /// A top-level composition can supply bound Title/Subtitle/CallToAction labels
        /// even when its block uses a live scene template. Overlay its authored labels
        /// on top of the scene template by stable ID; keep all other live template labels.
        /// The top-level master is refreshed before this merge.
        /// </summary>
        public static void MergeEditableSlots(VideoCompositionBlock resolved, VideoCompositionBlock stored)
        {
            if (resolved == null || stored?.CompositionLabels == null) return;

            resolved.CompositionLabels = resolved.CompositionLabels ?? new List<VideoCompositionTextLabel>();
            foreach (var label in stored.CompositionLabels)
            {
                resolved.CompositionLabels.RemoveAll(item =>
                    String.Equals(item.Id, label.Id, StringComparison.OrdinalIgnoreCase));
                resolved.CompositionLabels.Add(label);
            }
        }

        /// <summary>
        /// The identity must be explicitly present throughout all three blocks; marketing
        /// copy can be changed without weakening this non-optional disclosure.
        /// Returns null for a compliant composition or an actionable validation error.
        /// </summary>
        public static string Validate(VideoComposition composition)
        {
            if (!AppliesTo(composition)) return null;

            if (!String.Equals(composition.VideoDisclosurePolicyVersion, Version, StringComparison.Ordinal))
                return $"Trexel video disclosure policy v{Version} must be applied before assembly or publication.";

            var required = new[]
            {
                (Role: VideoCompositionBlockRole.Intro, Slot: OpeningIdentitySlot),
                (Role: VideoCompositionBlockRole.Content, Slot: PresenterIdentitySlot),
                (Role: VideoCompositionBlockRole.CallToAction, Slot: ClosingIdentitySlot)
            };

            foreach (var item in required)
            {
                var blocks = composition.Blocks?.Where(block => block.Role == item.Role).ToList();
                if (blocks == null || blocks.Count != 1)
                    return $"Trexel video requires exactly one {item.Role} block for AI disclosure.";

                var label = blocks[0].CompositionLabels?.FirstOrDefault(x =>
                    String.Equals(x.Id, item.Slot, StringComparison.OrdinalIgnoreCase));
                if (label == null ||
                    label.Binding != VideoCompositionLabelBinding.None ||
                    String.IsNullOrWhiteSpace(label.Text) ||
                    label.Text.IndexOf(Identity, StringComparison.OrdinalIgnoreCase) < 0)
                    return $"Trexel {item.Role} block must show the editable '{Identity}' disclosure.";

                // Visible throughout the block, including the entire presenter segment.
                if (label.DelaySeconds > 0.5 || label.DelaySeconds < 0 ||
                    label.VisibleDurationSeconds.HasValue ||
                    label.FontSize < 24 ||
                    label.X < 0 || label.X > 1920 ||
                    label.Y < 0 || label.Y > 1080)
                    return $"Trexel {item.Role} disclosure must be on-screen, legible, and visible for the full block.";
            }

            return null;
        }

        private static void EnsureIdentity(VideoComposition composition, VideoCompositionBlockRole role, string slotId, int y)
        {
            var block = composition.Blocks?.FirstOrDefault(item => item.Role == role);
            if (block == null) return;

            block.CompositionLabels = block.CompositionLabels ?? new List<VideoCompositionTextLabel>();
            if (block.CompositionLabels.Any(label => String.Equals(label.Id, slotId, StringComparison.OrdinalIgnoreCase)))
                return;

            block.CompositionLabels.Add(new VideoCompositionTextLabel
            {
                Id = slotId,
                Text = Identity,
                Binding = VideoCompositionLabelBinding.None,
                X = 1760,
                Y = y,
                FontSize = 30,
                Bold = true,
                MaxWidth = 1100,
                Alignment = VideoCompositionTextAlignment.Right,
                Effect = VideoCompositionTextEffect.DropShadow,
                EffectColor = "#000000"
            });
        }
    }
}
