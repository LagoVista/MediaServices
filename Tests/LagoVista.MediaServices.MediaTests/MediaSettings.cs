using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using System;

namespace LagoVista.MediaServices.MediaTests
{
    public class MediaSettings : IMediaServicesConnectionSettings
    {
        bool _production;
        public MediaSettings(bool production)
        {
            _production = production;
        }

        public IConnectionSettings MediaLibraryConnection => _production ? CloudStorage.Utils.TestConnections.ProductionDocDB : CloudStorage.Utils.TestConnections.DevDocDB;

        public IConnectionSettings MediaStorageConnection => _production ? CloudStorage.Utils.TestConnections.ProductionTableStorageDB : CloudStorage.Utils.TestConnections.DevTableStorageDB;

        public string ImageSearchClientId
        {
            get
            {
                var clientSecret = Environment.GetEnvironmentVariable("OPENVERSE_IMAGE_CLIENT_ID", EnvironmentVariableTarget.User);
                if (String.IsNullOrEmpty(clientSecret))
                {
                    throw new ArgumentNullException("Must define env var [OPENVERSE_IMAGE_CLIENT_ID]");
                }

                return clientSecret;
            }
        }


        public string ImageSearchClientSecret
        {
            get
            {
                var clientSecret = Environment.GetEnvironmentVariable("OPENVERSE_IMAGE_CLIENT_SECRET", EnvironmentVariableTarget.User);
                if (String.IsNullOrEmpty(clientSecret))
                {
                    throw new ArgumentNullException("Must define env var [OPENVERSE_IMAGE_CLIENT_SECRET]");
                }

                return clientSecret;
            }
        }

        public bool ShouldConsolidateCollections => true;

        public string ImageSearchUri => "https://https://api.openverse.org";

        public string GoogleTextToSpeechAPIKey { get; }
    }
}
