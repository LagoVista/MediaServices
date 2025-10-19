// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 13d902e366e2825eb59a71219f23640311949c2b35f6b3a4f772420d19b873b0
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using LagoVista.MediaServices.Managers;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class ImageSearchTests
    {

        EntityHeader _org = EntityHeader.Create("123456", "org");
        EntityHeader _user = EntityHeader.Create("123456", "user");

        IMediaSearchManager _searchManager;

        [TestInitialize]
        public void Init()
        {
            _searchManager = new MediaSearchManager(new MediaSettings(false), new AdminLogger(new ConsoleLogWriter()), new Mock<IAppConfig>().Object, new Mock<IDependencyManager>().Object, new Mock<ISecurity>().Object);
        }

        [TestMethod]
        public async Task QueryResults()
        {
            var pageRequest = new ListRequest()
            {
                PageIndex = 5,
                PageSize = 20,
            };

            var results = await _searchManager.SearchImagesAsync("F-22 Raptor", "smithsonian_air_and_space_museum", pageRequest, _org, _user);
            Assert.IsTrue(results.Successful);
            foreach (var result in results.Model)
            {
                Console.WriteLine($"Title: {result.Title} - {result.ImageUrl} - {result.Thumbnail} ");
            }
        }
    }
}
