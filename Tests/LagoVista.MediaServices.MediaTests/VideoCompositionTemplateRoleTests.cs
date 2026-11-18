using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class VideoCompositionTemplateRoleTests
    {
        [TestMethod]
        public void ThreeBlockTemplateAllowsVideoIntroBeforeVideoPresenter()
        {
            var blocks = new List<VideoCompositionBlock>
            {
                new VideoCompositionBlock { Key = "brand-intro", SortOrder = 0, Type = VideoCompositionBlockType.Video },
                new VideoCompositionBlock { Key = "presenter", SortOrder = 1, Type = VideoCompositionBlockType.Video },
                new VideoCompositionBlock { Key = "closing", SortOrder = 2, Type = VideoCompositionBlockType.Image }
            };

            var method = typeof(VideoCompositionTemplateManager).GetMethod(
                "AssignBlockRoles", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { blocks });

            Assert.AreEqual(VideoCompositionBlockRole.Intro, blocks[0].Role,
                "An opening video must not be mistaken for the presenter.");
            Assert.AreEqual(VideoCompositionBlockRole.Content, blocks[1].Role);
            Assert.AreEqual(VideoCompositionBlockRole.CallToAction, blocks[2].Role);
        }
    }
}
