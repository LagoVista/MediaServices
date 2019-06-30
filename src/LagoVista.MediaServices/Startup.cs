﻿using LagoVista.Core.Interfaces;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
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
        }
    }
}
