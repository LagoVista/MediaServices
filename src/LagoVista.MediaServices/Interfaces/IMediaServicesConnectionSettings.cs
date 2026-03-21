// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 6360dd9a5358d8b908f25c3a0cac7d64169bb94c2cc082262562c696781b6f2c
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IMediaServicesConnectionSettings
    {
        IConnectionSettings MediaLibraryConnection { get; }
        IConnectionSettings MediaStorageConnection { get; }

        string ImageSearchUri { get; }
        string ImageSearchClientId { get; }
        string ImageSearchClientSecret { get;  }        
        string GoogleTextToSpeechAPIKey { get; }

        bool ShouldConsolidateCollections { get; }
    }
}
