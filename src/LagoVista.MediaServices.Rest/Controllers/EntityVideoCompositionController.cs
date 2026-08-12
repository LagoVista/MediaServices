using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Rest.Controllers
{
    [ConfirmedUser]
    public class EntityVideoCompositionController : LagoVistaBaseController
    {
        private readonly IEntityVideoCompositionManager _manager;
        private readonly IEntityVideoProductionOrchestrator _productionOrchestrator;

        public EntityVideoCompositionController(IEntityVideoCompositionManager manager, IEntityVideoProductionOrchestrator productionOrchestrator, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _productionOrchestrator = productionOrchestrator ?? throw new ArgumentNullException(nameof(productionOrchestrator));
        }

        [HttpGet("/api/media/entityvideocomposition/sources/{entityType}")]
        public Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, CancellationToken cancellationToken = default)
        {
            return _manager.GetSourcesAsync(entityType, OrgEntityHeader, UserEntityHeader, GetListRequestFromHeader(), cancellationToken);
        }

        [HttpGet("/api/media/entityvideocomposition/source/{entityType}/{entityId}")]
        public Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            return _manager.GetSourceAsync(entityType, entityId, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/entityvideocomposition")]
        public Task<InvokeResult<VideoComposition>> CreateCompositionAsync([FromBody] CreateEntityVideoCompositionRequest request, CancellationToken cancellationToken = default)
        {
            return _manager.CreateCompositionAsync(request, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/entityvideocomposition/{entityType}/{entityId}/sync")]
        public Task<InvokeResult<VideoComposition>> SyncCompositionAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            return _manager.SyncCompositionAsync(entityType, entityId, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPost("/api/media/entityvideocomposition/production")]
        public Task<InvokeResult<EntityVideoProductionWorkspace>> PrepareProductionAsync([FromBody] PrepareEntityVideoProductionRequest request, CancellationToken cancellationToken = default)
        {
            return _productionOrchestrator.PrepareAsync(request, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }

        [HttpPut("/api/media/entityvideocomposition/{entityType}/{entityId}/info")]
        public Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, [FromBody] PatchEntityVideoCompositionInfoRequest request, CancellationToken cancellationToken = default)
        {
            return _manager.PatchVideoCompositionInfoAsync(entityType, entityId, request?.VideoCompositionInfo, OrgEntityHeader, UserEntityHeader, cancellationToken);
        }
    }
}
