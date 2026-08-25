using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.CloudRepos.StorageRecords;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class EntityVideoCompositionRepo : IEntityVideoCompositionRepo
    {
        public const int DefaultPageSize = 200;
        public const int MaximumPageSize = 500;

        private readonly IEntityUtilsRepository _entityUtilsRepository;
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IApplicationDataStore _applicationDataStore;

        public EntityVideoCompositionRepo(IEntityUtilsRepository entityUtilsRepository, IEntityTypeResolver entityTypeResolver, IApplicationDataStore applicationDataStore)
        {
            _entityUtilsRepository = entityUtilsRepository ?? throw new ArgumentNullException(nameof(entityUtilsRepository));
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _applicationDataStore = applicationDataStore ?? throw new ArgumentNullException(nameof(applicationDataStore));
        }

        public async Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, string orgId, ListRequest listRequest, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("Organization id is required.", nameof(orgId));

            ValidateSourceType(entityType);

            var request = NormalizeListRequest(listRequest);
            var query = new StorageQuery<EntityVideoComposition>()
                .Where(record => record.Organization.Id, StorageFilterOperator.Equal, orgId.Trim())
                .Where(record => record.EntityType, StorageFilterOperator.Equal, entityType.Trim())
                .OrderBy(record => record.Name)
                .WithPage(new StoragePageRequest(request.PageSize, request.NextRowKey));

            var page = await _applicationDataStore.QueryAsync(query, cancellationToken).ConfigureAwait(false);
            var summaries = page.Items.Select(record => new EntityVideoCompositionSummary
            {
                Id = record.Id.Value,
                Name = record.Name,
                Key = record.Key,
                EntityType = record.EntityType,
                VideoCompositionInfo = record.VideoCompositionInfo
            }).ToList();

            return ListResponse<EntityVideoCompositionSummary>.Create(
                summaries,
                request,
                page.HasMoreRecords,
                null,
                page.ContinuationToken);
        }

        public async Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, string orgId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity id is required.", nameof(entityId));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("Organization id is required.", nameof(orgId));

            var normalizedEntityType = entityType.Trim();
            var normalizedEntityId = entityId.Trim();
            var normalizedOrgId = orgId.Trim();
            var modelType = ValidateSourceType(normalizedEntityType);

            // Rich source content is loaded explicitly only for operations that need it.
            // Listing and composition-state persistence are Application Data only.
            var document = await _entityUtilsRepository.GetEntityByIdAsync(normalizedEntityType, normalizedEntityId, normalizedOrgId, cancellationToken).ConfigureAwait(false);
            if (document == null) return null;

            var model = document.ToObject(modelType);
            var entity = model as EntityBase;
            var source = model as IVideoCompositionSource;

            if (entity == null) throw new InvalidOperationException($"Entity type '{entityType}' did not deserialize to {nameof(EntityBase)}.");
            if (source == null) throw new InvalidOperationException($"Entity type '{entityType}' did not deserialize to {nameof(IVideoCompositionSource)}.");

            var record = await _applicationDataStore.GetAsync<EntityVideoComposition>(new StorageKey(normalizedEntityId, normalizedOrgId), cancellationToken).ConfigureAwait(false);
            source.VideoCompositionInfo = record?.VideoCompositionInfo ?? new EntityVideoCompositionInfo();
            return new EntityVideoCompositionSource(entity, source);
        }

        public async Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, EntityHeader org, string name, string key, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity id is required.", nameof(entityId));
            if (EntityHeader.IsNullOrEmpty(org)) throw new ArgumentException("Organization is required.", nameof(org));
            if (user == null) throw new ArgumentNullException(nameof(user));

            ValidateSourceType(entityType);

            var normalizedEntityId = entityId.Trim();
            var storageKey = new StorageKey(normalizedEntityId, org.Id);
            var record = await _applicationDataStore.GetAsync<EntityVideoComposition>(storageKey, cancellationToken).ConfigureAwait(false);

            if (record == null)
            {
                record = new EntityVideoComposition
                {
                    Id = new NormalizedId32(normalizedEntityId),
                    Organization = org,
                    EntityType = entityType.Trim(),
                    Name = name,
                    Key = key,
                    VideoCompositionInfo = videoCompositionInfo
                };
                await _applicationDataStore.InsertAsync(record, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                record.Organization = org;
                record.EntityType = entityType.Trim();
                record.Name = name;
                record.Key = key;
                record.VideoCompositionInfo = videoCompositionInfo;
                await _applicationDataStore.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            }

            return InvokeResult.Success;
        }

        private Type ValidateSourceType(string entityType)
        {
            if (!_entityTypeResolver.TryGetEntityType(entityType.Trim(), out var modelType) || modelType == null) throw new InvalidOperationException($"Could not resolve entity type '{entityType}'.");
            if (!typeof(EntityBase).IsAssignableFrom(modelType)) throw new InvalidOperationException($"Entity type '{entityType}' does not inherit from {nameof(EntityBase)}.");
            if (!typeof(IVideoCompositionSource).IsAssignableFrom(modelType)) throw new InvalidOperationException($"Entity type '{entityType}' does not implement {nameof(IVideoCompositionSource)}.");
            return modelType;
        }

        private static ListRequest NormalizeListRequest(ListRequest listRequest)
        {
            var request = listRequest ?? new ListRequest();
            if (request.PageIndex < 1) request.PageIndex = 1;
            if (request.PageSize < 1) request.PageSize = DefaultPageSize;
            else if (request.PageSize > MaximumPageSize) request.PageSize = MaximumPageSize;
            return request;
        }
    }
}
