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
        public const string ValueProposition = "Pay for outcomes, not hours worked.";

        // Stable, per-block label IDs are the editable slots in the composition.
        // IDs are 32-character hexadecimal strings, compatible with existing label IDs.
        public const string OpeningIdentitySlot = "A17E0000000000000000000000000001";
        public const string PresenterIdentitySlot = "A17E0000000000000000000000000002";
        public const string ClosingIdentitySlot = "A17E0000000000000000000000000003";
        public const string ClosingValueSlot = "A17E0000000000000000000000000004";

        private static readonly string[] PolicySlotIds =
        {
            OpeningIdentitySlot,
            PresenterIdentitySlot,
            ClosingIdentitySlot,
            ClosingValueSlot
        };

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

            var closing = composition.Blocks?.FirstOrDefault(block => block.Role == VideoCompositionBlockRole.CallToAction);
            if (closing != null)
            {
                closing.CompositionLabels = closing.CompositionLabels ?? new List<VideoCompositionTextLabel>();
                if (!closing.CompositionLabels.Any(label => label.Id == ClosingValueSlot))
                {
                    closing.CompositionLabels.Add(new VideoCompositionTextLabel
                    {
                        Id = ClosingValueSlot,
                        Text = ValueProposition,
                        Binding = VideoCompositionLabelBinding.None,
                        X = 960,
                        Y = 740,
                        MaxWidth = 1600,
                        FontSize = 44,
                        Bold = true,
                        Alignment = VideoCompositionTextAlignment.Center,
                        Effect = VideoCompositionTextEffect.DropShadow,
                        EffectColor = "#000000"
                    });
                }
            }
        }

        /// <summary>
        /// Referenced block templates replace their underlying block at render time.
        /// Preserve only the explicit per-composition Trexel slots over the reference.
        /// </summary>
        public static void MergeEditableSlots(VideoCompositionBlock resolved, VideoCompositionBlock stored)
        {
            if (resolved == null || stored?.CompositionLabels == null) return;

            var overrides = stored.CompositionLabels
                .Where(label => PolicySlotIds.Contains(label.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (overrides.Count == 0) return;

            resolved.CompositionLabels = resolved.CompositionLabels ?? new List<VideoCompositionTextLabel>();
            resolved.CompositionLabels.RemoveAll(label =>
                PolicySlotIds.Contains(label.Id, StringComparer.OrdinalIgnoreCase));
            resolved.CompositionLabels.AddRange(overrides);
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
