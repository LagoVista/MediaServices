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
