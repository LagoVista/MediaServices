using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]

namespace LagoVista.MediaServices.CloudRepos
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            ErrorCodes.Register(typeof(Resources.ErrorCodes));

            services.AddTransient<IDeviceAdminManager, DeviceAdminManager>();
            services.AddTransient<IDeviceTypeManager, DeviceTypeManager>();
            services.AddTransient<IEquipmentManager, EquipmentManager>();
        }
    }
}
