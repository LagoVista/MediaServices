// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: decb13ceff9c283ead33a42713877685545c17490e2425f6b7ffceee79df3cdb
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging;
using LagoVista.MediaServices.Interfaces;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]

namespace LagoVista.MediaServices.CloudRepos
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IMediaServicesRepo, MediaServicesRepo>();
            services.AddTransient<IMediaLibraryRepo, MediaLibraryRepo>();
        }
    }
}
