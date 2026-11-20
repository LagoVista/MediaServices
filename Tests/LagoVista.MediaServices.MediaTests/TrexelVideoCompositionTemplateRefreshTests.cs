using LagoVista.Core.Models;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class TrexelVideoCompositionTemplateRefreshTests
    {
        [TestMethod]
        public void RefreshReusesPresenterMediaAndUpdatesMasterLabels()
        {
            var composition = MakeComposition();
            var mediaId = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content).MediaResource.Id;
            var priorContentId = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content).Id;
            var oldHash = composition.CalculateCurrentInputSha256();

            var master = MakeTemplate();
            var error = TrexelVideoCompositionTemplateRefresh.Apply(composition, master);

            Assert.IsNull(error);
            Assert.AreEqual(2, composition.SourceCompositionTemplateVersion);
            Assert.AreEqual("New master opening", composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Intro).CompositionLabels.First().Text);
            Assert.AreEqual("Template-owned marketing copy",
                composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.CallToAction).CompositionLabels.Single(x => x.Text == "Template-owned marketing copy").Text);
            Assert.AreEqual(mediaId, composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content).MediaResource.Id);
            Assert.AreEqual(priorContentId, composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content).Id);
            Assert.AreEqual("1", composition.VideoDisclosurePolicyVersion);
            Assert.AreNotEqual(oldHash, composition.CalculateCurrentInputSha256());
            Assert.IsNull(TrexelVideoDisclosurePolicy.Validate(composition), "AI disclosure must be present on all three refreshed blocks.");
        }

        [TestMethod]
        public void RefreshDoesNotReinsertPreviouslyInjectedMarketingCopy()
        {
            var composition = MakeComposition();
            var closing = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.CallToAction);
            closing.CompositionLabels.Add(new VideoCompositionTextLabel
            {
                Id = "A17E0000000000000000000000000004",
                Text = "Pay for outcomes, not hours worked."
            });

            var template = MakeTemplate();
            template.Blocks.Single(x => x.Role == VideoCompositionBlockRole.CallToAction).CompositionLabels.Clear();

            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            closing = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.CallToAction);
            Assert.IsFalse(closing.CompositionLabels.Any(x => x.Text == "Pay for outcomes, not hours worked."));
            Assert.IsFalse(closing.CompositionLabels.Any(x => x.Id == "A17E0000000000000000000000000004"));
            Assert.IsTrue(closing.CompositionLabels.Any(x =>
                x.Id == TrexelVideoDisclosurePolicy.ClosingIdentitySlot &&
                x.Text == TrexelVideoDisclosurePolicy.Identity));
        }

        [TestMethod]
        public void PersistedAssemblyDisclosureIsValidEvenWhenTemplateHashesDiffer()
        {
            // A later master-template edit does not invalidate the disclosures
            // that were materialized into the saved composition during assembly.
            var composition = MakeComposition();
            TrexelVideoDisclosurePolicy.EnsureEditableSlots(composition);
            composition.IsReady = true;
            composition.CurrentInputSha256 = new string('a', 64);
            composition.OutputInputSha256 = new string('b', 64);

            Assert.IsFalse(composition.IsCurrent);
            Assert.IsNull(TrexelVideoDisclosurePolicy.Validate(composition));

            // The three AI identity disclosures still must be present.
            var presenter = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            presenter.CompositionLabels.RemoveAll(x => x.Id == TrexelVideoDisclosurePolicy.PresenterIdentitySlot);
            Assert.IsNotNull(TrexelVideoDisclosurePolicy.Validate(composition));
        }

        [TestMethod]
        public void RefreshRejectsBrokenMasterWithoutModifyingExistingVideo()
        {
            var composition = MakeComposition();
            var master = MakeTemplate();
            master.Blocks.RemoveAll(x => x.Role == VideoCompositionBlockRole.CallToAction);

            var originalHash = composition.CalculateCurrentInputSha256();
            var error = TrexelVideoCompositionTemplateRefresh.Apply(composition, master);

            Assert.IsFalse(String.IsNullOrWhiteSpace(error));
            Assert.AreEqual(originalHash, composition.CalculateCurrentInputSha256());
        }

        [TestMethod]
        public void RefreshIsNoOpForUnrelatedCompositions()
        {
            var composition = MakeComposition();
            composition.SourceEntityType = "MediaResource";
            var originalHash = composition.CalculateCurrentInputSha256();

            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, MakeTemplate()));
            Assert.AreEqual(originalHash, composition.CalculateCurrentInputSha256());
        }

        [TestMethod]
        public void RefreshPreservesOnlyCompositionOwnedSupportingGraphic()
        {
            var composition = MakeComposition();
            var presenter = composition.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content);
            presenter.OverlayImages.Add(new VideoCompositionBlockImage
            {
                Id = TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot,
                MediaResource = EntityHeader.Create("A17E0000000000000000000000000601", "Task graphic"),
                Scale = 1,
                PositionX = 0.5,
                PositionY = 0.5,
                DelaySeconds = 16,
                VisibleDurationSeconds = 5,
                FadeInSeconds = 0.5,
                FadeOutSeconds = 0.5
            });
            presenter.OverlayImages.Add(new VideoCompositionBlockImage
            {
                Id = "A17E0000000000000000000000000602",
                MediaResource = EntityHeader.Create("A17E0000000000000000000000000603", "Stale local art")
            });

            var master = MakeTemplate();
            master.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content)
                .OverlayImages.Add(new VideoCompositionBlockImage
                {
                    Id = "A17E0000000000000000000000000604",
                    MediaResource = EntityHeader.Create("A17E0000000000000000000000000605", "Master artwork")
                });

            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, master));
            presenter = composition.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content);
            Assert.AreEqual(2, presenter.OverlayImages.Count, "Keep one master image and one Trexel-owned graphic.");
            Assert.IsTrue(presenter.OverlayImages.Any(i => i.MediaResource?.Text == "Master artwork"));
            Assert.IsFalse(presenter.OverlayImages.Any(i => i.MediaResource?.Text == "Stale local art"));

            var graphic = presenter.OverlayImages.Single(i =>
                i.Id == TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot);
            Assert.AreEqual("Task graphic", graphic.MediaResource.Text);
            Assert.AreEqual(16d, graphic.DelaySeconds);
            Assert.AreEqual(5d, graphic.VisibleDurationSeconds.Value);
            Assert.AreEqual(0.5d, graphic.FadeInSeconds);
        }

        [TestMethod]
        public void RebuildingAgainDoesNotDuplicateTrexelGraphic()
        {
            var composition = MakeComposition();
            var presenter = composition.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content);
            presenter.OverlayImages.Add(new VideoCompositionBlockImage
            {
                Id = TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot,
                MediaResource = EntityHeader.Create("A17E0000000000000000000000000601", "Task graphic"),
                Scale = 1,
                VisibleDurationSeconds = 5
            });

            var template = MakeTemplate();
            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            Assert.AreEqual(1, composition.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content)
                .OverlayImages.Count(i => i.Id == TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot));
        }

        [TestMethod]
        public void ResolvedPresenterTemplateKeepsMasterArtworkAndTrexelGraphic()
        {
            // Assembly uses a separate live block template. Its artwork is
            // authoritative, while only the Trexel-specific graphic is overlaid.
            var fromComposition = new VideoCompositionBlock
            {
                Role = VideoCompositionBlockRole.Content,
                OverlayImages = new List<VideoCompositionBlockImage>
                {
                    new VideoCompositionBlockImage
                    {
                        Id = TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot,
                        MediaResource = EntityHeader.Create("A17E0000000000000000000000000601", "Task graphic"),
                        Scale = 1,
                        VisibleDurationSeconds = 5
                    }
                }
            };
            var resolved = new VideoCompositionBlock
            {
                Role = VideoCompositionBlockRole.Content,
                OverlayImages = new List<VideoCompositionBlockImage>
                {
                    new VideoCompositionBlockImage
                    {
                        Id = "A17E0000000000000000000000000604",
                        MediaResource = EntityHeader.Create("A17E0000000000000000000000000605", "Master artwork")
                    }
                }
            };

            TrexelVideoCompositionTemplateRefresh.OverlaySupportingGraphic(resolved, fromComposition);
            TrexelVideoCompositionTemplateRefresh.OverlaySupportingGraphic(resolved, fromComposition);
            Assert.AreEqual(2, resolved.OverlayImages.Count);
            Assert.IsTrue(resolved.OverlayImages.Any(i => i.MediaResource?.Text == "Master artwork"));
            Assert.IsTrue(resolved.OverlayImages.Any(i => i.MediaResource?.Text == "Task graphic"));
        }

        [TestMethod]
        public void RefreshWithoutGraphicLeavesPresentersMasterOverlaysUnchanged()
        {
            var composition = MakeComposition();
            var template = MakeTemplate();
            template.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content)
                .OverlayImages.Add(new VideoCompositionBlockImage
                {
                    Id = "A17E0000000000000000000000000604",
                    MediaResource = EntityHeader.Create("A17E0000000000000000000000000605", "Master artwork")
                });
            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            var images = composition.Blocks.Single(b => b.Role == VideoCompositionBlockRole.Content).OverlayImages;
            Assert.AreEqual(1, images.Count);
            Assert.AreEqual("Master artwork", images.Single().MediaResource.Text);
        }

        [TestMethod]
        public void ReassemblyRefitsOversizedCroppedPresenterAfterTemplateRefresh()
        {
            var composition = MakeComposition();
            var original = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            original.PresenterScale = 0.42; // Historically saved layout: 1117px for 758x1050.
            original.PresenterPositionX = 0.68;
            original.PresenterPositionY = 1.0;
            var presenterMediaId = original.MediaResource.Id;

            var template = MakeTemplate();
            template.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content).PresenterScale = 0.42;
            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));

            // The assembly preparation path runs this after the master and any
            // referenced block templates are resolved, not before refresh.
            var content = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            TrexelPresenterLayout.Apply(content, 758, 1050);

            var renderedHeight = TrexelPresenterLayout.CompositionCanvasWidth *
                content.PresenterScale * 1050.0 / 758.0;
            Assert.AreEqual(864.0, renderedHeight, 0.001);
            Assert.AreEqual(0.5, content.PresenterPositionX, 0.000001);
            Assert.AreEqual(1.0, content.PresenterPositionY, 0.000001);
            Assert.AreEqual(presenterMediaId, content.MediaResource.Id);
            Assert.AreEqual(38.0, content.DurationSeconds.Value, 0.000001);
        }

        [TestMethod]
        public void RepeatedReassemblyRefitsFromLatestPresenterDimensions()
        {
            var composition = MakeComposition();
            var template = MakeTemplate();

            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            var content = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            TrexelPresenterLayout.Apply(content, 758, 1050);
            var portraitScale = content.PresenterScale;

            // The next reassembly refreshes the template again; it must
            // recalculate from the current cropped media dimensions each time.
            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.Apply(composition, template));
            content = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            TrexelPresenterLayout.Apply(content, 1104, 1070);

            var renderedHeight = TrexelPresenterLayout.CompositionCanvasWidth *
                content.PresenterScale * 1070.0 / 1104.0;
            Assert.AreEqual(864.0, renderedHeight, 0.001);
            Assert.AreNotEqual(portraitScale, content.PresenterScale);
            Assert.AreEqual(1.0, content.PresenterPositionY, 0.000001);
        }

        [TestMethod]
        public void ExtremelyWidePresenterIsLimitedByCanvasWidth()
        {
            var content = new VideoCompositionBlock
            {
                Role = VideoCompositionBlockRole.Content,
                PresenterScale = 4.0
            };

            TrexelPresenterLayout.Apply(content, 4000, 400);

            Assert.AreEqual(1.0, content.PresenterScale, 0.000001);
            Assert.AreEqual(0.5, content.PresenterPositionX, 0.000001);
            Assert.AreEqual(1.0, content.PresenterPositionY, 0.000001);
        }

        [TestMethod]
        public void SwitchingTrexelTemplateKeepsPresenterAndSupportingGraphic()
        {
            var composition = MakeComposition();
            composition.Id = "A17E0000000000000000000000000099";
            composition.OutputMediaResource = EntityHeader.Create("A17E0000000000000000000000000700", "Previous assembly");
            composition.PublishedVideoMediaResource = EntityHeader.Create("A17E0000000000000000000000000701", "Previous Vimeo publication");
            var presenter = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            presenter.OverlayImages.Add(new VideoCompositionBlockImage
            {
                Id = TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot,
                MediaResource = EntityHeader.Create("A17E0000000000000000000000000601", "Trexel task graphic")
            });
            var mediaId = presenter.MediaResource.Id;
            var template = MakeTemplate();
            template.Id = "A17E0000000000000000000000000199";
            template.Version = 9;
            template.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Intro)
                .CompositionLabels.First().Text = "New template opening";

            Assert.IsNull(TrexelVideoCompositionTemplateRefresh.ApplySelectedTemplate(composition, template));
            Assert.AreEqual(template.Id, composition.SourceCompositionTemplate.Id);
            Assert.AreEqual(9, composition.SourceCompositionTemplateVersion);
            Assert.AreEqual("A17E0000000000000000000000000099", composition.Id);
            Assert.AreEqual("New template opening",
                composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Intro).CompositionLabels.First().Text);
            presenter = composition.Blocks.Single(x => x.Role == VideoCompositionBlockRole.Content);
            Assert.AreEqual(mediaId, presenter.MediaResource.Id);
            Assert.AreEqual(38.0, presenter.DurationSeconds.Value, 0.001);
            Assert.AreEqual(1, presenter.OverlayImages.Count(x =>
                x.Id == TrexelVideoCompositionTemplateRefresh.SupportingGraphicOverlaySlot));
            Assert.AreEqual("A17E0000000000000000000000000700", composition.OutputMediaResource.Id);
            Assert.AreEqual("A17E0000000000000000000000000701", composition.PublishedVideoMediaResource.Id);
            Assert.IsNull(TrexelVideoDisclosurePolicy.Validate(composition));
        }

        [TestMethod]
        public void InvalidTemplateSwitchLeavesExistingCompositionUntouched()
        {
            var composition = MakeComposition();
            var originalHash = composition.CalculateCurrentInputSha256();
            var originalTemplateId = composition.SourceCompositionTemplate.Id;
            var template = MakeTemplate();
            template.Id = "A17E0000000000000000000000000199";
            template.Blocks.RemoveAll(x => x.Role == VideoCompositionBlockRole.CallToAction);

            var error = TrexelVideoCompositionTemplateRefresh.ApplySelectedTemplate(composition, template);

            Assert.IsFalse(String.IsNullOrWhiteSpace(error));
            Assert.AreEqual(originalTemplateId, composition.SourceCompositionTemplate.Id);
            Assert.AreEqual(originalHash, composition.CalculateCurrentInputSha256());
        }

        [TestMethod]
        public void SwitchingTemplateIsLimitedToTrexelCompositions()
        {
            var composition = MakeComposition();
            composition.SourceEntityType = "MediaResource";
            var originalTemplateId = composition.SourceCompositionTemplate.Id;
            var template = MakeTemplate();
            template.Id = "A17E0000000000000000000000000199";

            Assert.IsFalse(String.IsNullOrWhiteSpace(
                TrexelVideoCompositionTemplateRefresh.ApplySelectedTemplate(composition, template)));
            Assert.AreEqual(originalTemplateId, composition.SourceCompositionTemplate.Id);
        }

        private static VideoComposition MakeComposition()
        {
            return new VideoComposition
            {
                SourceEntityType = "VirtualTeamMember",
                SourceCompositionTemplate = EntityHeader.Create("A17E0000000000000000000000000100", "Master"),
                SourceCompositionTemplateVersion = 1,
                Blocks = new List<VideoCompositionBlock>
                {
                    new VideoCompositionBlock { Key = "opening", SortOrder = 0, Role = VideoCompositionBlockRole.Intro, Type = VideoCompositionBlockType.Image },
                    new VideoCompositionBlock
                    {
                        Key = "presenter", SortOrder = 1, Role = VideoCompositionBlockRole.Content,
                        Type = VideoCompositionBlockType.Video,
                        MediaResource = EntityHeader.Create("A17E0000000000000000000000000101", "Victor video"),
                        DurationSeconds = 38
                    },
                    new VideoCompositionBlock { Key = "closing", SortOrder = 2, Role = VideoCompositionBlockRole.CallToAction, Type = VideoCompositionBlockType.Image }
                }
            };
        }

        private static VideoCompositionTemplate MakeTemplate()
        {
            return new VideoCompositionTemplate
            {
                Id = "A17E0000000000000000000000000100",
                Name = "AppTrex Standard Trexel Video",
                Key = "apptrex-standard-trexel-video",
                Version = 2,
                IsActive = true,
                DefaultLocale = "en-US",
                Blocks = new List<VideoCompositionBlock>
                {
                    new VideoCompositionBlock
                    {
                        Key = "opening", SortOrder = 0, Role = VideoCompositionBlockRole.Intro,
                        Type = VideoCompositionBlockType.Image,
                        CompositionLabels = new List<VideoCompositionTextLabel>
                        {
                            new VideoCompositionTextLabel { Text = "New master opening" }
                        }
                    },
                    new VideoCompositionBlock
                    {
                        Key = "presenter", SortOrder = 1, Role = VideoCompositionBlockRole.Content,
                        Type = VideoCompositionBlockType.Video
                    },
                    new VideoCompositionBlock
                    {
                        Key = "closing", SortOrder = 2, Role = VideoCompositionBlockRole.CallToAction,
                        Type = VideoCompositionBlockType.Image,
                        CompositionLabels = new List<VideoCompositionTextLabel>
                        {
                            new VideoCompositionTextLabel
                            {
                                Id = "A17E0000000000000000000000000010",
                                Text = "Template-owned marketing copy"
                            }
                        }
                    }
                }
            };
        }
    }
}
