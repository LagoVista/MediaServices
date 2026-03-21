using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaServicesConnectionSettings : IMediaServicesConnectionSettings
    {
        public IConnectionSettings MediaLibraryConnection { get; }

        public IConnectionSettings MediaStorageConnection { get; }

        public string ImageSearchUri { get; }

        public string ImageSearchClientId { get; }

        public string ImageSearchClientSecret { get; }

        public string GoogleTextToSpeechAPIKey { get; }

        public bool ShouldConsolidateCollections { get; }

        public MediaServicesConnectionSettings(IConfiguration configuration)
        {
            MediaLibraryConnection = configuration.CreateDefaultDBStorageSettings();
            MediaStorageConnection = configuration.CreateDefaultTableStorageSettings();

            var imageSearchSection = configuration.GetRequiredSection("ImageSearch");
            ImageSearchUri = imageSearchSection.Require("Uri");
            ImageSearchClientId = imageSearchSection.Require("ClientId");
            ImageSearchClientSecret = imageSearchSection.Require("Secret");

            var tts = configuration.GetRequiredSection("GoogleApiKeys");
            GoogleTextToSpeechAPIKey = tts.Require("TextToSpeech");
        }
    }
}
