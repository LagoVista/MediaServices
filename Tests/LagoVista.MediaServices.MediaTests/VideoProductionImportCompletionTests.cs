using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class VideoProductionImportCompletionTests
    {
        [TestMethod]
        public void CompletedProviderImportIsAuthoritativeAfterLateProviderRefresh()
        {
            var production = new VideoProduction
            {
                IsReady = true,
                ProviderVideoId = "heygen-v1",
                ProviderVideoImportCompletedProviderVideoId = "heygen-v1",
                FinalVideoMediaResource = EntityHeader.Create("A17E0000000000000000000000000102", "Victor Presenter"),
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderCompleted)
            };

            Assert.IsTrue(production.HasImportedCurrentProviderVideo(),
                "A late ProviderCompleted status must not hide an imported presenter video.");

            production.Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.Rendering);
            Assert.IsTrue(production.HasImportedCurrentProviderVideo(),
                "Rendered media remains imported even if an out-of-order status update says Rendering.");
        }

        [TestMethod]
        public void OlderProviderImportDoesNotCountForANewVideo()
        {
            var production = new VideoProduction
            {
                IsReady = true,
                ProviderVideoId = "heygen-v2",
                ProviderVideoImportCompletedProviderVideoId = "heygen-v1",
                FinalVideoMediaResource = EntityHeader.Create("A17E0000000000000000000000000102", "Old presenter"),
            };
            Assert.IsFalse(production.HasImportedCurrentProviderVideo());
            production.ProviderVideoImportCompletedProviderVideoId = "heygen-v2";
            production.IsReady = false;
            Assert.IsFalse(production.HasImportedCurrentProviderVideo());
        }
        [TestMethod]
        public void LegacyImportedPresenterRecoversOutputHashFromExactSubmittedRun()
        {
            var originalInput = new string('a', 64);
            var production = CompletedPresenter(originalInput);

            Assert.IsFalse(production.IsCurrent);
            Assert.IsTrue(production.RestoreCompletedOutputInputSha256());
            Assert.AreEqual(originalInput, production.OutputInputSha256);
            Assert.IsTrue(production.IsCurrent,
                "Legacy media must become current without paying for another provider render.");
            Assert.IsFalse(production.RestoreCompletedOutputInputSha256(),
                "The recovery is idempotent and should only persist once.");
        }

        [TestMethod]
        public void LaterScriptChangeDoesNotMisidentifyOldVideoAsCurrent()
        {
            var production = CompletedPresenter(new string('a', 64));
            production.CurrentInputSha256 = new string('b', 64);

            Assert.IsTrue(production.RestoreCompletedOutputInputSha256());
            Assert.IsFalse(production.IsCurrent,
                "The recovered output hash must remain tied to the old run.");
        }

        [TestMethod]
        public void CannotRecoverHashFromDifferentProviderGeneration()
        {
            var production = CompletedPresenter(new string('a', 64));
            production.Runs[0].ProviderVideoId = "heygen-different-video";

            Assert.IsFalse(production.RestoreCompletedOutputInputSha256());
            Assert.IsFalse(production.IsCurrent);
            Assert.IsNull(production.OutputInputSha256);
        }

        private static VideoProduction CompletedPresenter(string submittedHash)
        {
            return new VideoProduction
            {
                IsReady = true,
                ProviderVideoId = "heygen-v1",
                ProviderVideoImportCompletedProviderVideoId = "heygen-v1",
                FinalVideoMediaResource = EntityHeader.Create("A17E0000000000000000000000000102", "Victor presenter"),
                Status = EntityHeader<VideoProductionStatus>.Create(VideoProductionStatus.ProviderVideoReady),
                CurrentInputSha256 = submittedHash,
                Runs = new List<VideoProductionRun>
                {
                    new VideoProductionRun
                    {
                        ProviderVideoId = "heygen-v1",
                        InputSha256 = submittedHash
                    }
                }
            };
        }

    }
}
