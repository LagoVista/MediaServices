using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using RingCentral;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class MediaSearchManager : ManagerBase, IMediaSearchManager
    {
        private readonly IMediaServicesConnectionSettings _settings;
        private readonly IAdminLogger _adminLogger;

        public MediaSearchManager(IMediaServicesConnectionSettings settings, IAdminLogger logger, IAppConfig appConfig, IDependencyManager depmanager, ISecurity security)
            : base(logger, appConfig, depmanager, security)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _adminLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ListResponse<ImageSearchResult>> SearchImagesAsync(string term, string source, ListRequest listRequest, EntityHeader org, EntityHeader user)
        {
            _adminLogger.Trace($"[MediaSearchManager__SearchImagesAsync] Search Images Term: {term}, Source: {source}");

            term = Uri.EscapeDataString(term);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ImageSearchClientSecret);
                var qs = $"?q={term}&s={source}&page={listRequest.PageIndex}&page_size={listRequest.PageSize}";
                var url = $"{_settings.ImageSearchUri}/v1/images{qs}";

                _adminLogger.Trace($"[MediaSearchManager__SearchImagesAsync] Search Url: {url}");

                var responsJSON = await client.GetStringAsync(url);

                var response = JsonConvert.DeserializeObject<ImageSearchResults>(responsJSON);

                _adminLogger.Trace($"[MediaSearchManager__SearchImagesAsync] Result Count: {response.ResultCount}");

                var listResponse = ListResponse<ImageSearchResult>.Create(response.Results);
                listResponse.PageIndex = response.Page;
                listResponse.PageCount = response.PageCount;
                listResponse.PageSize = response.PageSize;
                listResponse.HasMoreRecords = response.PageCount > response.Page;

                return listResponse;
            }
        }
    }
}
