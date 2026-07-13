using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProductionRepo
    {
        Task AddVideoProductionAsync(VideoProduction production);
        Task UpdateVideoProductionAsync(VideoProduction production);
        Task DeleteVideoProductionAsync(string id);
        Task<VideoProduction> GetVideoProductionAsync(string id);
        Task<ListResponse<VideoProductionSummary>> GetVideoProductionSummariesForOrgAsync(string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoProduction>> GetFullVideoProductionsForOrgAsync(string orgId);
        Task<ListResponse<VideoProductionSummary>> GetVideoProductionSummariesForTargetAsync(string targetEntityType, string targetEntityId, string orgId, ListRequest listRequest);
        Task<IEnumerable<VideoProduction>> GetFullVideoProductionsForTargetAsync(string targetEntityType, string targetEntityId, string orgId);
        Task<IEnumerable<VideoProduction>> GetVideoProductionsByProviderVideoIdAsync(string providerVideoId, string orgId);
        Task<bool> QueryKeyInUseAsync(string key, string orgId);
    }
}
