using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class VideoAvatarLookSelectionTests
    {
        private static VideoAvatar MakeAvatar()
        {
            return new VideoAvatar
            {
                Name = "Example Trexel",
                Looks = new List<VideoAvatarLook>
                {
                    new VideoAvatarLook { Id = "PRIMARY", Name = "Primary", IsPrimary = true, IsActive = true,
                        Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Ready), ProviderAvatarId = "heygen-primary" },
                    new VideoAvatarLook { Id = "EDITORIAL", Name = "Editorial", IsActive = true,
                        Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Ready), ProviderAvatarId = "heygen-editorial" },
                    new VideoAvatarLook { Id = "INACTIVE", Name = "Retired", IsActive = false,
                        Status = EntityHeader<VideoAvatarStatus>.Create(VideoAvatarStatus.Ready), ProviderAvatarId = "heygen-retired" }
                }
            };
        }

        [TestMethod]
        public void NoSelectionUsesExistingPrimaryLook()
        {
            var selected = VideoAvatarLookSelection.Resolve(MakeAvatar(), null);
            Assert.AreEqual("PRIMARY", selected.Id);
        }

        [TestMethod]
        public void ExplicitEditorialLookIsSelectedWithoutMutatingPrimary()
        {
            var avatar = MakeAvatar();
            var selected = VideoAvatarLookSelection.Resolve(avatar, "editorial");
            Assert.AreEqual("EDITORIAL", selected.Id);
            Assert.IsTrue(avatar.Looks[0].IsPrimary);
        }

        [TestMethod]
        public void MissingOrInactiveExplicitLookNeverFallsBackToPrimary()
        {
            var avatar = MakeAvatar();
            Assert.IsNull(VideoAvatarLookSelection.Resolve(avatar, "MISSING"));
            Assert.IsNull(VideoAvatarLookSelection.Resolve(avatar, "INACTIVE"));
        }

        [TestMethod]
        public void SwitchingLooksRequiresNewRenderWithoutDiscardingExistingOutput()
        {
            var existing = new VideoProduction
            {
                VideoAvatarLookId = "PRIMARY",
                FinalVideoMediaResource = EntityHeader.Create("12345678123412341234123456789012", "Existing output")
            };
            Assert.IsTrue(VideoAvatarLookSelection.RequiresNewRender(existing, "EDITORIAL"));
            Assert.IsFalse(VideoAvatarLookSelection.RequiresNewRender(existing, "primary"));
            Assert.IsFalse(VideoAvatarLookSelection.RequiresNewRender(existing, null));
            Assert.IsNotNull(existing.FinalVideoMediaResource);
            Assert.AreEqual("PRIMARY", existing.VideoAvatarLookId);
        }
    }
}
