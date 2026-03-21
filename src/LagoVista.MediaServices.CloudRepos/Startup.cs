using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IMediaServicesConnectionSettings, MediaServicesConnectionSettings>();
        }
    }
}


namespace LagoVista.DependencyInjection
{
    public static class MediaModule
    {
        public static void AddMediaModule(this IServiceCollection services, IConfigurationRoot configRoot, IAdminLogger logger)
        {
            LagoVista.MediaServices.CloudRepos.Startup.ConfigureServices(services);
            LagoVista.MediaServices.Startup.ConfigureServices(services);
            services.AddMetaDataHelper<MediaLibrary>();
        }
    }
}