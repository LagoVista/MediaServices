using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface IVideoProductionManager
    {
        Task<InvokeResult> AddVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user);
        Task<InvokeResult> UpdateVideoProductionAsync(VideoProduction production, EntityHeader org, EntityHeader user);
        Task<InvokeResult> DeleteVideoProductionAsync(string id, EntityHeader org, EntityHeader user);
        Task<VideoProduction> GetVideoProductionAsync(string id, EntityHeader org, EntityHeader user);
        Task<ListResponse<VideoProductionSummary>> GetVideoProductionsForOrgAsync(EntityHeader org, EntityHeader user, ListRequest listRequest);
        Task<InvokeResult<VideoProduction>> EstimateVideoProductionCostAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoProduction>> GeneratePreviewAudioAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoProduction>> SubmitVideoProductionAsync(string id, EntityHeader org, EntityHeader user);
        Task<InvokeResult<VideoProduction>> RefreshVideoProductionStatusAsync(string id, EntityHeader org, EntityHeader user);
    }
}
