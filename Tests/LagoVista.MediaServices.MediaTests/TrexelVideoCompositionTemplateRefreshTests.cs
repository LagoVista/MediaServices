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
