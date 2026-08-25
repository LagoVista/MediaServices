using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.CloudRepos.StorageRecords;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class EntityVideoCompositionRepo : IEntityVideoCompositionRepo
    {
        public const int DefaultPageSize = 200;
        public const int MaximumPageSize = 500;

        private readonly IDocumentCollection _documentCollection;
        private readonly IEntityUtilsRepository _entityUtilsRepository;
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IApplicationDataStore _applicationDataStore;

        public EntityVideoCompositionRepo(IMediaServicesConnectionSettings settings, IDocumentCollectionFactory documentCollectionFactory, IEntityUtilsRepository entityUtilsRepository, IEntityTypeResolver entityTypeResolver, IApplicationDataStore applicationDataStore)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (documentCollectionFactory == null) throw new ArgumentNullException(nameof(documentCollectionFactory));

            _documentCollection = documentCollectionFactory.Create(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName);
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
            var normalizedEntityType = entityType.Trim();
            var normalizedOrgId = orgId.Trim();
            var documents = await _documentCollection.QueryAsync<EntityVideoCompositionDocument>(document => document.EntityType == normalizedEntityType && document.OwnerOrganization.Id == normalizedOrgId, document => document.Name, request, cancellationToken).ConfigureAwait(false);
            var compositionInfo = await LoadCompositionInfoAsync(normalizedEntityType, normalizedOrgId, cancellationToken).ConfigureAwait(false);

            var summaries = documents.Model.Select(document => new EntityVideoCompositionSummary
            {
                Id = document.Id,
                Name = document.Name,
                Key = document.Key,
                EntityType = document.EntityType,
                VideoCompositionInfo = compositionInfo.TryGetValue(document.Id, out var info) ? info : null
            });

            return ListResponse<EntityVideoCompositionSummary>.Create(request, summaries);
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

        public async Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, string orgId, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity id is required.", nameof(entityId));
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentException("Organization id is required.", nameof(orgId));
            if (user == null) throw new ArgumentNullException(nameof(user));

            var normalizedEntityType = entityType.Trim();
            var normalizedEntityId = entityId.Trim();
            var normalizedOrgId = orgId.Trim();
            ValidateSourceType(normalizedEntityType);

            var key = new StorageKey(normalizedEntityId, normalizedOrgId);
            var record = await _applicationDataStore.GetAsync<EntityVideoComposition>(key, cancellationToken).ConfigureAwait(false);
            if (record == null)
            {
                record = new EntityVideoComposition
                {
                    Id = new NormalizedId32(normalizedEntityId),
                    Organization = EntityHeader.Create(normalizedOrgId, normalizedOrgId),
                    EntityType = normalizedEntityType,
                    VideoCompositionInfo = videoCompositionInfo
                };
                await _applicationDataStore.InsertAsync(record, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!String.Equals(record.EntityType, normalizedEntityType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Entity video composition '{normalizedEntityId}' is stored as '{record.EntityType}', not '{normalizedEntityType}'.");

                record.VideoCompositionInfo = videoCompositionInfo;
                await _applicationDataStore.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            }

            return InvokeResult.Success;
        }

        private async Task<Dictionary<string, EntityVideoCompositionInfo>> LoadCompositionInfoAsync(string entityType, string orgId, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, EntityVideoCompositionInfo>(StringComparer.OrdinalIgnoreCase);
            string continuationToken = null;

            do
            {
                var query = new StorageQuery<EntityVideoComposition>()
                    .Where(record => record.Organization.Id, StorageFilterOperator.Equal, orgId)
                    .Where(record => record.EntityType, StorageFilterOperator.Equal, entityType)
                    .WithPage(new StoragePageRequest(500, continuationToken));
                var page = await _applicationDataStore.QueryAsync(query, cancellationToken).ConfigureAwait(false);
                foreach (var record in page.Items)
                    result[record.Id.Value] = record.VideoCompositionInfo;
                continuationToken = page.ContinuationToken;
            }
            while (!String.IsNullOrWhiteSpace(continuationToken));

            return result;
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

        private sealed class EntityVideoCompositionDocument
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            public string Name { get; set; }
            public string Key { get; set; }
            public string EntityType { get; set; }
            public EntityHeader OwnerOrganization { get; set; }
        }
    }
}
