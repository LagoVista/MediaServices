using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using Microsoft.Extensions.Configuration;

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

        public string HeyGenApiKey { get;}

        public MediaServicesConnectionSettings(IConfiguration configuration)
        {
            MediaLibraryConnection = configuration.CreateDefaultDBStorageSettings();
            MediaStorageConnection = configuration.CreateDefaultTableStorageSettings();

            var imageSearchSection = configuration.GetSection("ImageSearch");
            ImageSearchUri = imageSearchSection.Require("Uri");
            ImageSearchClientId = imageSearchSection.Require("ClientId");
            ImageSearchClientSecret = imageSearchSection.Require("Secret");

            var tts = configuration.GetSection("GoogleApiKeys");
            GoogleTextToSpeechAPIKey = tts.Require("TextToSpeech");

            var heyGen = configuration.GetSection("HeyGen");
            HeyGenApiKey = configuration.Require("ApiKey");
        }
    }
}
