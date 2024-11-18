using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.MediaTests
{
    [TestClass]
    public class TextToSpeachTest
    {
        [TestMethod]
        public async Task TextToSpeach()
        {

            var tts = new TextToSpeechRequest();
            tts.Text = "Hi there, let's see how this works.  At some point we can upload some SAML, but not today.";
            tts.Ssml = "";
            tts.Gender = "MALE";
            tts.Language = "en-US";
            tts.Voice = "en-US-Casual-K";

            var textService = new LagoVista.MediaServices.Services.TextToSpeechService(new Settings());
            var result = await textService.GenerateAudio(tts);
            Assert.IsTrue(result.Successful);

            System.IO.File.WriteAllBytes(@$"X:\Output{DateTime.Now.Ticks}.mp3", result.Result);
        }
    }

    class Settings : IMediaServicesConnectionSettings
    {
        public IConnectionSettings MediaLibraryConnection => throw new NotImplementedException();

        public IConnectionSettings MediaStorageConnection => throw new NotImplementedException();

        public string ImageSearchUri => throw new NotImplementedException();

        public string ImageSearchClientId => throw new NotImplementedException();

        public string ImageSearchClientSecret => throw new NotImplementedException();

        public string GoogleTextToSpeechAPIKey => System.Environment.GetEnvironmentVariable("GOOGLE_API_SPEECH_TO_TEXT");

        public bool ShouldConsolidateCollections => throw new NotImplementedException();
    }



}
