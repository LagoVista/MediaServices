using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class VideoAvatarRepo : DocumentDBRepoBase<VideoAvatar>, IVideoAvatarRepo
    {
        private readonly ICacheProvider _cacheProvider;
        private static readonly TimeSpan ProviderCreationLockDuration = TimeSpan.FromMinutes(5);

        public VideoAvatarRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
            _cacheProvider = services?.CacheProvider ?? throw new ArgumentNullException(nameof(services.CacheProvider));
        }

        public Task AddVideoAvatarAsync(VideoAvatar avatar)
        {
            return CreateDocumentAsync(avatar);
        }

        public Task DeleteVideoAvatarAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task<bool> AttemptAcquireProviderCreationLockAsync(string avatarId, string token)
        {
            if (String.IsNullOrWhiteSpace(avatarId))
            {
                throw new ArgumentNullException(nameof(avatarId));
            }

            if (String.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            return _cacheProvider.AttemptAcquireLockAsync(CreateProviderCreationLockKey(avatarId), token, ProviderCreationLockDuration);
        }

        public async Task ReleaseProviderCreationLockAsync(string avatarId, string token)
        {
            if (String.IsNullOrWhiteSpace(avatarId) || String.IsNullOrWhiteSpace(token))
            {
                return;
            }

            await _cacheProvider.ReleaseLockAsync(CreateProviderCreationLockKey(avatarId), token);
        }

        private static string CreateProviderCreationLockKey(string avatarId)
        {
            return $"video-avatar:provider-create:{avatarId}".ToLowerInvariant();
        }

        public Task<VideoAvatar> GetVideoAvatarAsync(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarSummariesForOrgAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<VideoAvatarSummary, VideoAvatar>(qry => qry.IsPublic || qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }

        public Task<ListResponse<VideoAvatarSummary>> GetVideoAvatarSummariesForSubjectAsync(string subjectEntityType, string subjectEntityId, string orgId, ListRequest listRequest)
        {
            throw new NotImplementedException();
//            return QuerySummaryAsync<VideoAvatarSummary, VideoAvatar>(qry => qry.SubjectEntityType == subjectEntityType && qry.SubjectEntityId == subjectEntityId && (qry.IsPublic || qry.OwnerOrganization.Id == orgId), qry => qry.Name, listRequest);
        }

        public Task<IEnumerable<VideoAvatar>> GetFullVideoAvatarsForOrgAsync(string orgId)
        {
            return QueryAsync(avatar => avatar.IsPublic || avatar.OwnerOrganization.Id == orgId);
        }

        public Task<IEnumerable<VideoAvatar>> GetFullVideoAvatarsForSubjectAsync(string subjectEntityType, string subjectEntityId, string orgId)
        {
            throw new NotImplementedException();
            //return QueryAsync(avatar => avatar.SubjectEntityType == subjectEntityType && avatar.SubjectEntityId == subjectEntityId && (avatar.IsPublic || avatar.OwnerOrganization.Id == orgId));
        }

        public async Task<bool> QueryKeyInUseAsync(string key, string orgId)
        {
            return (await QueryAsync(avatar => avatar.Key == key && (avatar.OwnerOrganization.Id == orgId || avatar.IsPublic))).Any();
        }

        public async Task<VideoAvatar> UpdateVideoAvatarAsync(VideoAvatar source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var current = await GetVideoAvatarAsync(source.Id);

            if (current == null)
            {
                throw new InvalidOperationException($"Could not find video avatar '{source.Id}'.");
            }

            ApplyUserEditableFields(source, current);

            await UpsertDocumentAsync(current);

            return current;
        }

        public async Task<VideoAvatar> UpdateVideoAvatarProviderStateAsync(string id, VideoAvatarProviderState state)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var avatar = await GetVideoAvatarAsync(id);

            if (avatar == null)
            {
                throw new InvalidOperationException($"Could not find video avatar '{id}'.");
            }

            avatar.ProviderAssetId = state.ProviderAssetId;
            avatar.ProviderAvatarId = state.ProviderAvatarId;
            avatar.ProviderAvatarStatus = state.ProviderAvatarStatus;
            avatar.Status = state.Status;
            avatar.ErrorMessage = state.ErrorMessage;
            avatar.LastStatusCheck = state.LastStatusCheck;

            await UpsertDocumentAsync(avatar);

            return avatar;
        }

        private static void ApplyUserEditableFields(VideoAvatar source, VideoAvatar target)
        {
            target.Name = source.Name;
            target.Key = source.Key;
            target.IsDefault = source.IsDefault;
            target.Icon = source.Icon;
            target.Description = source.Description;
            target.AvatarImage = source.AvatarImage;
            target.EditorialImages = source.EditorialImages;
            target.Voices = source.Voices;
        }
    }
}