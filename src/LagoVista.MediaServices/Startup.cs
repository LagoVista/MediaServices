using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Models;
using LagoVista.MediaServices.Services;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddTransient<IVideoAvatarManager, VideoAvatarManager>();
            services.AddTransient<IVideoProductionManager, VideoProductionManager>();
            services.AddTransient<IVideoCompositionManager, VideoCompositionManager>();
            services.AddTransient<IVideoCompositionTemplateManager, VideoCompositionTemplateManager>();
            services.AddTransient<IVideoCompositionBlockTemplateManager, VideoCompositionBlockTemplateManager>();
            services.AddTransient<IVideoAssemblyMediaSourceResolver, VideoAssemblyMediaSourceResolver>();
            services.AddTransient<IVideoAssemblyRequestManager, VideoAssemblyRequestManager>();
            services.AddTransient<IVideoAssemblyCallbackHandler, VideoAssemblyCallbackHandler>();
            services.AddTransient<IVideoMediaImportManager, VideoMediaImportManager>();
            services.AddSingleton<IVimeoVideoService, VimeoVideoService>();
            services.AddTransient<ILagoVistaIconStyleProfileProvider, LagoVistaIconStyleProfileProvider>();
            services.AddTransient<ILagoVistaIconPromptBuilder, LagoVistaIconPromptBuilder>();
            services.AddTransient<ILagoVistaIconGenerationManager, LagoVistaIconGenerationManager>();
            services.AddTransient<ILagoVistaEntityInstanceIconGenerationManager, LagoVistaEntityInstanceIconGenerationManager>();
            services.AddTransient<ILagoVistaSystemDefaultIconGenerationManager, LagoVistaSystemDefaultIconGenerationManager>();

        }
    }
}
