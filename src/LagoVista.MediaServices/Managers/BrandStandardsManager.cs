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
    public class BrandStandardsManager : ManagerBase, IBrandStandardsManager
    {
        private readonly IBrandStandardsRepo _repo;

        public BrandStandardsManager(IBrandStandardsRepo repo, IAdminLogger logger, IAppConfig appConfig,
            IDependencyManager dependencyManager, ISecurity security)
            : base(logger, appConfig, dependencyManager, security)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<InvokeResult> AddBrandStandardsAsync(BrandStandards brandStandards, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(brandStandards, AuthorizeActions.Create, user, org);
            ValidationCheck(brandStandards, Actions.Create);
            if (await _repo.QueryKeyInUseAsync(brandStandards.Key, org.Id))
                return InvokeResult.FromError("Brand standards with this key already exists.");

            await _repo.AddBrandStandardsAsync(brandStandards);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateBrandStandardsAsync(BrandStandards brandStandards, EntityHeader org, EntityHeader user)
        {
            var existing = await _repo.GetBrandStandardsAsync(brandStandards.Id);
            await AuthorizeAsync(existing, AuthorizeActions.Update, user, org);

            // Ownership and creation audit fields are server authority, not client-editable.
            brandStandards.OwnerOrganization = existing.OwnerOrganization;
            brandStandards.CreatedBy = existing.CreatedBy;
            brandStandards.CreationDate = existing.CreationDate;

            ValidationCheck(brandStandards, Actions.Update);
            if (await _repo.QueryKeyInUseAsync(brandStandards.Key, org.Id, brandStandards.Id))
                return InvokeResult.FromError("Brand standards with this key already exists.");

            await _repo.UpdateBrandStandardsAsync(brandStandards);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteBrandStandardsAsync(string id, EntityHeader org, EntityHeader user)
        {
            var brandStandards = await _repo.GetBrandStandardsAsync(id);
            await AuthorizeAsync(brandStandards, AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(brandStandards);
            await _repo.DeleteBrandStandardsAsync(id);
            return InvokeResult.Success;
        }

        public async Task<BrandStandards> GetBrandStandardsAsync(string id, EntityHeader org, EntityHeader user)
        {
            var brandStandards = await _repo.GetBrandStandardsAsync(id);
            await AuthorizeAsync(brandStandards, AuthorizeActions.Read, user, org);
            return brandStandards;
        }

        public async Task<BrandStandards> GetBrandStandardsByKeyAsync(string key, EntityHeader org, EntityHeader user)
        {
            var brandStandards = await _repo.GetBrandStandardsByKeyAsync(org.Id, key);
            await AuthorizeAsync(brandStandards, AuthorizeActions.Read, user, org);
            return brandStandards;
        }

        public async Task<ListResponse<BrandStandardsSummary>> GetBrandStandardsForOrgAsync(string orgId,
            ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(BrandStandards));
            return await _repo.GetBrandStandardsForOrgAsync(orgId, listRequest);
        }

        public async Task<DependentObjectCheckResult> CheckBrandStandardsInUseAsync(string id,
            EntityHeader org, EntityHeader user)
        {
            var brandStandards = await _repo.GetBrandStandardsAsync(id);
            await AuthorizeAsync(brandStandards, AuthorizeActions.Read, user, org);
            return await CheckForDepenenciesAsync(brandStandards);
        }

        public async Task<bool> QueryBrandStandardsKeyInUseAsync(string key, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, org.Id, typeof(BrandStandards));
            return await _repo.QueryKeyInUseAsync(key, org.Id);
        }
    }
}
