using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    /// <summary>
    /// Keeps MediaResource as a rich EntityBase while routing its metadata into the
    /// shared Application Data Mongo database under a dedicated MediaResource collection.
    /// Binary media storage remains owned by MediaServicesRepo.
    /// </summary>
    public sealed class MongoMediaServicesRepo : MediaServicesRepo, IMediaServicesRepo
    {
        public const string MediaResourceCollectionName = "MediaResource";

        private readonly IDocumentDBRepoBase<MediaResource> _metadataStore;

        public MongoMediaServicesRepo(
            IAdminLogger adminLogger,
            IMediaServicesConnectionSettings settings,
            ICacheProvider cacheProvider,
            IApplicationDataStorageSettings applicationDataSettings)
            : base(adminLogger, settings, cacheProvider)
        {
            if (applicationDataSettings == null) throw new ArgumentNullException(nameof(applicationDataSettings));

            var storageSettings = new DocumentStorageSettings
            {
                Provider = DocumentStorageProviderType.Mongo,
                DatabaseName = applicationDataSettings.DatabaseName,
                Mongo = new MongoDocumentStorageSettings
                {
                    ConnectionString = applicationDataSettings.ConnectionString,
                    DatabaseName = applicationDataSettings.DatabaseName,
                }
            };

            _metadataStore = DocumentStorageFactory.Create<MediaResource>(
                storageSettings,
                adminLogger,
                cacheProvider,
                collectionNameResolver: new MediaResourceCollectionNameResolver());
        }

        async Task IMediaServicesRepo.AddMediaResourceRecordAsync(MediaResource resource)
        {
            await _metadataStore.CreateDocumentAsync(resource).ConfigureAwait(false);
        }

        async Task IMediaServicesRepo.AddOrUpdateMediaResourceAsync(MediaResource record)
        {
            await _metadataStore.UpsertDocumentAsync(record).ConfigureAwait(false);
        }

        async Task IMediaServicesRepo.UpdateMediaResourceRecordAsync(MediaResource updated)
        {
            await _metadataStore.UpsertDocumentAsync(updated).ConfigureAwait(false);
        }

        async Task IMediaServicesRepo.DeleteMediaRecordAsync(string id)
        {
            await _metadataStore.DeleteDocumentAsync(id).ConfigureAwait(false);
        }

        async Task<MediaResource> IMediaServicesRepo.TryGetMediaResourceRecordAsync(string id)
        {
            var record = await _metadataStore.GetDocumentAsync(id, false).ConfigureAwait(false);
            return await EnsureLegacyRevisionAsync(record).ConfigureAwait(false);
        }

        async Task<MediaResource> IMediaServicesRepo.GetMediaResourceRecordAsync(string id)
        {
            var record = await _metadataStore.GetDocumentAsync(id).ConfigureAwait(false);
            return await EnsureLegacyRevisionAsync(record).ConfigureAwait(false);
        }

        Task<ListResponse<MediaResourceSummary>> IMediaServicesRepo.GetResourcesAsync(string orgId, ListRequest listRequest)
        {
            return _metadataStore.QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => qry.OwnerOrganization.Id == orgId,
                qry => qry.Name,
                listRequest);
        }

        Task<ListResponse<MediaResourceSummary>> IMediaServicesRepo.GetResourcesForLibraryAsync(string orgId, string libraryId, ListRequest listRequest)
        {
            return _metadataStore.QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaLibrary.Id == libraryId,
                qry => qry.Name,
                listRequest);
        }

        Task<ListResponse<MediaResourceSummary>> IMediaServicesRepo.GetResourcesForMediaTypeKeyLibrary(string orgId, string mediaTypeKey, ListRequest listRequest)
        {
            return _metadataStore.QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaTypeKey == mediaTypeKey,
                qry => qry.Name,
                listRequest);
        }

        private async Task<MediaResource> EnsureLegacyRevisionAsync(MediaResource record)
        {
            if (record == null || !String.IsNullOrEmpty(record.CurrentRevision) || !record.IsFileUpload)
                return record;

            var timestamp = UtcTimestamp.Now;
            if (record.History.Count > 0)
            {
                record.CurrentRevision = record.History[0].Id;
            }
            else
            {
                var history = new MediaResourceHistory
                {
                    CreatedBy = record.CreatedBy,
                    CreationDate = timestamp,
                    ContentSize = record.ContentSize,
                    Height = record.Height,
                    Name = "Revision 1",
                    Width = record.Width,
                    Id = Guid.NewGuid().ToId(),
                    StorageReferenceName = record.StorageReferenceName,
                };

                record.CurrentRevision = history.Id;
                record.History.Add(history);
            }

            record.LastUpdatedDate = timestamp;
            await _metadataStore.UpsertDocumentAsync(record).ConfigureAwait(false);
            return record;
        }

        private sealed class MediaResourceCollectionNameResolver : IDocumentCollectionNameResolver
        {
            public string Resolve(string databaseName, Type entityType, string explicitCollectionName = null)
                => MediaResourceCollectionName;

            public bool TryResolve(string databaseName, string entityTypeName, out string collectionName)
            {
                collectionName = MediaResourceCollectionName;
                return String.Equals(entityTypeName, nameof(MediaResource), StringComparison.OrdinalIgnoreCase);
            }

            public string GetFallback(string databaseName) => MediaResourceCollectionName;
        }
    }
}
