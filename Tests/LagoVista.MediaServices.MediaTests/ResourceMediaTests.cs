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
    public class ResourceMediaTests
    {
        [TestMethod]
        public async Task Should_Upload_DeviceType_ResourceMedia()
        {
            Mock<IMediaServicesConnectionSettings> _resourceMediaConnections = new Mock<IMediaServicesConnectionSettings>();

            _resourceMediaConnections.Setup(dep => dep.MediaStorageConnection).Returns(new ConnectionSettings()
            {
                AccountId = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCOUNTID", EnvironmentVariableTarget.User),
                AccessKey = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCESSKEY", EnvironmentVariableTarget.User)
            });


            var repo = new MediaServicesRepo(new AdminLogger(new Utils.LogWriter()), _resourceMediaConnections.Object);
            var rnd = new Random();
            var byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            var result = await repo.AddMediaAsync(byteArray, "testinorg", "MyBytes.bin", "bin");
            Assert.IsTrue(result.Successful);
        }

        [TestMethod]
        public async Task Should_Upload_And_Get_DeviceType_ResourceMedia()
        {
            Mock<IMediaServicesConnectionSettings> _resourceMediaConnections = new Mock<IMediaServicesConnectionSettings>();

            _resourceMediaConnections.Setup(dep => dep.MediaStorageConnection).Returns(new ConnectionSettings()
            {
                AccountId = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCOUNTID", EnvironmentVariableTarget.User),
                AccessKey = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCESSKEY", EnvironmentVariableTarget.User)
            });

            var repo = new MediaServicesRepo(new AdminLogger(new Utils.LogWriter()), _resourceMediaConnections.Object);
            var rnd = new Random();
            var byteArray = new byte[1024];
            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                byteArray[idx] = Convert.ToByte(rnd.Next() & 0xff);
            }

            var result = await repo.AddMediaAsync(byteArray, "testingorg", "MyBytes.bin", "bin");
            Assert.IsTrue(result.Successful);

            var media = await repo.GetMediaAsync("MyBytes.bin", "testingorg");
            Assert.IsTrue(result.Successful);

            for (var idx = 0; idx < byteArray.Length; ++idx)
            {
                Assert.AreEqual(byteArray[idx], media.Result[idx]);
            }

        }
    }
}
