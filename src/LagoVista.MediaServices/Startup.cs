// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: b384eaed91119e30ba4135623a7585ed4051c81ca405b5255eaa229b61803d0c
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Services;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]

namespace LagoVista.MediaServices
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IMediaServicesManager, MediaServicesManager>();
            services.AddTransient<IMediaLibraryManager, MediaLibraryManager>();
            services.AddTransient<IMediaSearchManager, MediaSearchManager>();
            services.AddTransient<ITextToSpeechService, TextToSpeechService>();
        }
    }
}
