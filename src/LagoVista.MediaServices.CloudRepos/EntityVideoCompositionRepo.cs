using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class EntityVideoCompositionRepo : DocumentDBRepoBase<EntityBase>, IEntityVideoCompositionRepo
    {
        public const int DefaultPageSize = 200;
        public const int MaximumPageSize = 500;

        private readonly IEntityUtilsRepository _entityUtilsRepository;
        private readonly IEntityTypeResolver _entityTypeResolver;

        public EntityVideoCompositionRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services, IEntityUtilsRepository entityUtilsRepository, IEntityTypeResolver entityTypeResolver) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
            _entityUtilsRepository = entityUtilsRepository ?? throw new ArgumentNullException(nameof(entityUtilsRepository));
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
        }

        public Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, string orgId, ListRequest listRequest, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("Organization id is required.", nameof(orgId));
            }

            ValidateSourceType(entityType);

            var request = NormalizeListRequest(listRequest);

            const string sql =
@"SELECT
    c.id AS Id,
    c.Name AS Name,
    c.Key AS Key,
    c.EntityType AS EntityType,
    c.VideoCompositionInfo AS VideoCompositionInfo
FROM c
WHERE c.EntityType = @entityType
AND c.OwnerOrganization.Id = @orgId
ORDER BY c.Name";

            return QueryAsync<EntityVideoCompositionSummary>(
                sql,
                request,
                new CloudStorage.QueryParameter("@entityType", entityType.Trim()),
                new CloudStorage.QueryParameter("@orgId", orgId.Trim()));
        }

        public async Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, string orgId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type is required.", nameof(entityType));
            }

            if (String.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("Entity id is required.", nameof(entityId));
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                throw new ArgumentException("Organization id is required.", nameof(orgId));
            }

            var modelType = ValidateSourceType(entityType);

            var document = await _entityUtilsRepository.GetEntityByIdAsync(
                entityType.Trim(),
                entityId.Trim(),
                orgId.Trim(),
                cancellationToken).ConfigureAwait(false);

            if (document == null)
            {
                return null;
            }

            var model = document.ToObject(modelType);
            var entity = model as EntityBase;
            var source = model as IVideoCompositionSource;

            if (entity == null)
            {
                throw new InvalidOperationException($"Entity type '{entityType}' did not deserialize to {nameof(EntityBase)}.");
            }

            if (source == null)
            {
                throw new InvalidOperationException($"Entity type '{entityType}' did not deserialize to {nameof(IVideoCompositionSource)}.");
            }

            source.VideoCompositionInfo = source.VideoCompositionInfo ?? new EntityVideoCompositionInfo();

            return new EntityVideoCompositionSource(entity, source);
        }

        public Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityId, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("Entity id is required.", nameof(entityId));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var fields = new Dictionary<string, JToken>
            {
                [nameof(IVideoCompositionSource.VideoCompositionInfo)] = videoCompositionInfo == null
                    ? JValue.CreateNull()
                    : JObject.FromObject(videoCompositionInfo)
            };

            return _entityUtilsRepository.PatchEntityFieldsAsync(
                entityId.Trim(),
                fields,
                user,
                cancellationToken);
        }

        private Type ValidateSourceType(string entityType)
        {
            if (!_entityTypeResolver.TryGetEntityType(entityType.Trim(), out var modelType) || modelType == null)
            {
                throw new InvalidOperationException($"Could not resolve entity type '{entityType}'.");
            }

            if (!typeof(EntityBase).IsAssignableFrom(modelType))
            {
                throw new InvalidOperationException($"Entity type '{entityType}' does not inherit from {nameof(EntityBase)}.");
            }

            if (!typeof(IVideoCompositionSource).IsAssignableFrom(modelType))
            {
                throw new InvalidOperationException($"Entity type '{entityType}' does not implement {nameof(IVideoCompositionSource)}.");
            }

            return modelType;
        }

        private static ListRequest NormalizeListRequest(ListRequest listRequest)
        {
            var request = listRequest ?? new ListRequest();

            if (request.PageIndex < 1)
            {
                request.PageIndex = 1;
            }

            if (request.PageSize < 1)
            {
                request.PageSize = DefaultPageSize;
            }
            else if (request.PageSize > MaximumPageSize)
            {
                request.PageSize = MaximumPageSize;
            }

            return request;
        }
    }
}
