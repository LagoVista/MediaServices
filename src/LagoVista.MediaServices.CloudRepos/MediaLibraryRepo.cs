﻿using LagoVista.CloudStorage;
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaLibraryRepo : DocumentDBRepoBase<MediaLibrary>, IMediaLibraryRepo
    {
        private bool _shouldConsolidateCollections;
        public MediaLibraryRepo(IMediaServicesConnectionSettings repoSettings, IAdminLogger logger, ICacheProvider cacheProvider)
            : base(repoSettings.MediaLibraryConnection.Uri, repoSettings.MediaLibraryConnection.AccessKey, repoSettings.MediaLibraryConnection.ResourceName, logger, cacheProvider)
        {
            _shouldConsolidateCollections = repoSettings.ShouldConsolidateCollections;
        }

        protected override bool ShouldConsolidateCollections
        {
            get
            {
                return _shouldConsolidateCollections;
            }
        }

        public Task AddMediaLibraryAsync(MediaLibrary mediaLibrary)
        {
            return this.CreateDocumentAsync(mediaLibrary);
        }

        public Task DeleteStateMachineAsync(string id)
        {
            return this.DeleteDocumentAsync(id);
        }

        public Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForCustomerAsync(string orgId, string customerId, ListRequest listRequest)
        {
            return base.QuerySummaryAsync<MediaLibrarySummary, MediaLibrary>(qry => qry.Customer.Id == customerId &&  qry.OwnerOrganization.Id == orgId, med => med.Name, listRequest);
        }

        public Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId, ListRequest listRequest)
        {
            return base.QuerySummaryAsync<MediaLibrarySummary, MediaLibrary>(qry => qry.IsPublic == true || qry.OwnerOrganization.Id == orgId, med=>med.Name, listRequest);
        }

        public Task<MediaLibrary> GetMediaLibrary(string id)
        {
            return this.GetDocumentAsync(id);
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            var items = await base.QueryAsync(attr => (attr.OwnerOrganization.Id == orgId || attr.IsPublic == true) && attr.Key == key);
            return items.Any(); 
        }

        public Task UpdateMediaLibraryAsync(MediaLibrary mediaLibrary)
        {
            return UpsertDocumentAsync(mediaLibrary);
        }
    }
}
