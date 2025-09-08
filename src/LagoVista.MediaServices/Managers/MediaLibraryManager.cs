using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static LagoVista.Core.Models.AuthorizeResult;

namespace LagoVista.MediaServices.Managers
{
    public class MediaLibraryManager : ManagerBase, IMediaLibraryManager
    {
        IMediaLibraryRepo _repo;
        public MediaLibraryManager(IMediaLibraryRepo repo, IAdminLogger logger, IAppConfig appConfig,
                        IDependencyManager depmanager, ISecurity security) : base(logger, appConfig, depmanager, security)
        {
            _repo = repo;
        }

        public async Task<InvokeResult> AddMediaLibraryAsync(MediaLibrary mediaLibrary, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(mediaLibrary, AuthorizeActions.Create, user, org);
            ValidationCheck(mediaLibrary, Actions.Create);
            await _repo.AddMediaLibraryAsync(mediaLibrary);

            return InvokeResult.Success;
        }

        public async Task<DependentObjectCheckResult> CheckMediaLibraryInUseAsync(string id, EntityHeader org, EntityHeader user)
        {
            var mediaLibrary = await _repo.GetMediaLibraryAsync(id);
            await AuthorizeAsync(mediaLibrary, AuthorizeActions.Read, user, org);
            return await base.CheckForDepenenciesAsync(mediaLibrary);
        }

        public async Task<InvokeResult> DeleteMediaLibraryAsync(string id, EntityHeader org, EntityHeader user)
        {
            var mediaLibrary = await _repo.GetMediaLibraryAsync(id);
            await ConfirmNoDepenenciesAsync(mediaLibrary);
            await AuthorizeAsync(mediaLibrary, AuthorizeActions.Delete, user, org);
            await _repo.DeleteStateMachineAsync(id);
            return InvokeResult.Success;
        }

        public async Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForCustomerAsync(string orgId, string customerId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaLibrary));
            return await _repo.GetMediaLibrariesForCustomerAsync(orgId, customerId, listRequest);
        }

        public async Task<ListResponse<MediaLibrarySummary>> GetMediaLibrariesForOrgsAsync(string orgId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaLibrary));
            return await _repo.GetMediaLibrariesForOrgsAsync(orgId, listRequest);
        }

        public async Task<MediaLibrary> GetMediaLibraryAsync(string id, EntityHeader org, EntityHeader user)
        {
            var mediaLibrary = await _repo.GetMediaLibraryAsync(id);
            await AuthorizeAsync(mediaLibrary, AuthorizeActions.Read, user, org);
            return mediaLibrary;
        }

        public Task<bool> QueryMediaLibraryKeyInUseAsync(string key, string orgId)
        {
            return _repo.QueryKeyInUseAsync(key, orgId);
        }

        public async Task<InvokeResult> UpdateMediaLibraryAsync(MediaLibrary mediaLibrary, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(mediaLibrary, AuthorizeActions.Update, user, org);
            ValidationCheck(mediaLibrary, Actions.Update);
            await _repo.UpdateMediaLibraryAsync(mediaLibrary);

            return InvokeResult.Success;
        }
    }
}
