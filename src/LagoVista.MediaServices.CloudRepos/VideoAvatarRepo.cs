using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
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
        public VideoAvatarRepo(IMediaServicesConnectionSettings settings, IDocumentCloudCachedServices services) : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, services)
        {
        }

        public Task AddVideoAvatarAsync(VideoAvatar avatar)
        {
            return CreateDocumentAsync(avatar);
        }

        public Task DeleteVideoAvatarAsync(string id)
        {
            return DeleteDocumentAsync(id);
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

        public Task UpdateVideoAvatarAsync(VideoAvatar avatar)
        {
            return UpsertDocumentAsync(avatar);
        }
    }
}