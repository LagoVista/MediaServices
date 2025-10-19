// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: c80e01bb23cacf37d464b7025f8b4b48f2fa8a25957e3ef9fa9dd41ae24348d5
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.CloudRepos;
using LagoVista.MediaServices.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public partial class ResourceMediaTests
    {
        IMediaServicesConnectionSettings _settings = new MediaSettings(false);

        [TestMethod]
        public async Task Should_Upload_DeviceType_ResourceMedia()
        {
            var repo = new MediaServicesRepo(new AdminLogger(new Utils.LogWriter()), _settings, null);
            var rnd = new Random();
            var byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            var result = await repo.AddMediaAsync(byteArray, "testingorg", "mybytes.bin", "application/octet-stream");
            Assert.IsTrue(result.Successful);
        }

        [TestMethod]
        public async Task Should_Upload_ThenUpdate_DeviceType_ResourceMedia()
        {
            var repo = new MediaServicesRepo(new AdminLogger(new Utils.LogWriter()), _settings, null);
            var rnd = new Random();
            var byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            var result = await repo.AddMediaAsync(byteArray, "testingorg", "mybytes.bin", "application/octet-stream");
            Assert.IsTrue(result.Successful);

            byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            result = await repo.UpdateMediaAsync(byteArray, "testingorg", "mybytes.bin", "application/octet-stream");
            Assert.IsTrue(result.Successful);

            var media = await repo.GetMediaAsync("mybytes.bin", "testingorg");
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                Assert.AreEqual(byteArray[idx], media.Result[idx]);
            }
        }

        [TestMethod]
        public async Task Should_Upload_And_Get_DeviceType_ResourceMedia()
        {
            var repo = new MediaServicesRepo(new AdminLogger(new Utils.LogWriter()), _settings, null);
            var rnd = new Random();
            var byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            var result = await repo.AddMediaAsync(byteArray, "testingorg", "mybytes.bin", "application/octet-stream");
            Assert.IsTrue(result.Successful);

            var media = await repo.GetMediaAsync("mybytes.bin", "testingorg");
            Assert.IsTrue(result.Successful);

            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                Assert.AreEqual(byteArray[idx], media.Result[idx]);
            }

        }
    }
}
