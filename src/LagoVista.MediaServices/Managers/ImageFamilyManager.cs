using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Threading.Tasks;
using static LagoVista.Core.Models.AuthorizeResult;

namespace LagoVista.MediaServices.Managers
{
    public class ImageFamilyManager : ManagerBase, IImageFamilyManager
    {
        private readonly IImageFamilyRepo _repo;

        public ImageFamilyManager(IImageFamilyRepo repo, IAdminLogger logger, IAppConfig appConfig,
            IDependencyManager dependencyManager, ISecurity security)
            : base(logger, appConfig, dependencyManager, security)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<InvokeResult> AddImageFamilyAsync(ImageFamily imageFamily, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(imageFamily, AuthorizeActions.Create, user, org);
            ValidationCheck(imageFamily, Actions.Create);
            if (await _repo.QueryKeyInUseAsync(imageFamily.Key, org.Id))
                return InvokeResult.FromError("An image family with this key already exists.");

            await _repo.AddImageFamilyAsync(imageFamily);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateImageFamilyAsync(ImageFamily imageFamily, EntityHeader org, EntityHeader user)
        {
            var existing = await _repo.GetImageFamilyAsync(imageFamily.Id);
            await AuthorizeAsync(existing, AuthorizeActions.Update, user, org);

            // Ownership and creation audit fields are server authority, not client-editable.
            imageFamily.OwnerOrganization = existing.OwnerOrganization;
            imageFamily.CreatedBy = existing.CreatedBy;
            imageFamily.CreationDate = existing.CreationDate;

            ValidationCheck(imageFamily, Actions.Update);
            if (await _repo.QueryKeyInUseAsync(imageFamily.Key, org.Id, imageFamily.Id))
                return InvokeResult.FromError("An image family with this key already exists.");

            await _repo.UpdateImageFamilyAsync(imageFamily);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteImageFamilyAsync(string id, EntityHeader org, EntityHeader user)
        {
            var imageFamily = await _repo.GetImageFamilyAsync(id);
            await AuthorizeAsync(imageFamily, AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(imageFamily);
            await _repo.DeleteImageFamilyAsync(id);
            return InvokeResult.Success;
        }

        public async Task<ImageFamily> GetImageFamilyAsync(string id, EntityHeader org, EntityHeader user)
        {
            var imageFamily = await _repo.GetImageFamilyAsync(id);
            await AuthorizeAsync(imageFamily, AuthorizeActions.Read, user, org);
            return imageFamily;
        }

        public async Task<ImageFamily> GetImageFamilyByKeyAsync(string key, EntityHeader org, EntityHeader user)
        {
            var imageFamily = await _repo.GetImageFamilyByKeyAsync(org.Id, key);
            await AuthorizeAsync(imageFamily, AuthorizeActions.Read, user, org);
            return imageFamily;
        }

        public async Task<ListResponse<ImageFamilySummary>> GetImageFamiliesForOrgAsync(string orgId,
            ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(ImageFamily));
            return await _repo.GetImageFamiliesForOrgAsync(orgId, listRequest);
        }

        public async Task<DependentObjectCheckResult> CheckImageFamilyInUseAsync(string id,
            EntityHeader org, EntityHeader user)
        {
            var imageFamily = await _repo.GetImageFamilyAsync(id);
            await AuthorizeAsync(imageFamily, AuthorizeActions.Read, user, org);
            return await CheckForDepenenciesAsync(imageFamily);
        }

        public async Task<bool> QueryImageFamilyKeyInUseAsync(string key, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(ImageFamily));
            return await _repo.QueryKeyInUseAsync(key, org.Id);
        }
    }
}
