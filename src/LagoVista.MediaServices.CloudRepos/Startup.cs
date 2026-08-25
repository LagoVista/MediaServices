using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.CloudRepos.StorageRecords;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
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
            services.AddApplicationDataStorageConnection();
            services.ConfigureApplicationData<EntityVideoComposition>(storage => storage
                .Index(x => x.EntityType)
                .Index(x => x.Name));

            services.AddTransient<IMediaServicesRepo, MongoMediaServicesRepo>();
            services.AddTransient<IVideoProcessorStorageUrlService, VideoProcessorStorageUrlService>();
            services.AddTransient<IVideoProcessorRequestStore, VideoProcessorRequestStore>();
            services.AddTransient<IVideoProcessorCallbackRegistrationStore, VideoProcessorCallbackRegistrationStore>();
            services.AddSingleton(new VideoProcessorLauncherOptions());
            services.AddTransient<IVideoProcessorLauncher, KubernetesVideoProcessorLauncher>();

            services.AddTransient<IVideoAvatarRepo, VideoAvatarRepo>();
            services.AddTransient<IVideoProductionRepo, VideoProductionRepo>();
            services.AddTransient<IVideoCompositionRepo, VideoCompositionRepo>();
            services.AddTransient<IVideoCompositionTemplateRepo, VideoCompositionTemplateRepo>();
            services.AddTransient<IEntityVideoCompositionRepo, EntityVideoCompositionRepo>();
            services.AddTransient<IVideoCompositionBlockTemplateRepo, VideoCompositionBlockTemplateRepo>();

            services.AddTransient<IMediaLibraryRepo, MediaLibraryRepo>();
            services.AddTransient<IHeyGenVideoService, HeyGenVideoService>();
            services.AddTransient<ILagoVistaIconAssetPublisher, LagoVistaIconAssetPublisher>();
            services.AddTransient<ILagoVistaIconCatalogManager, LagoVistaIconCatalogManager>();
            services.AddSingleton<IMediaServicesConnectionSettings, MediaServicesConnectionSettings>();
            services.AddSingleton(new VideoProcessorLauncherOptions());
            services.AddTransient<IVideoProcessorLauncher, KubernetesVideoProcessorLauncher>();

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
