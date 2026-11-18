using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
