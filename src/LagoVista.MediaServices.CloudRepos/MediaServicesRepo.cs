using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaServicesRepo : DocumentDBRepoBase<MediaResource>, IMediaServicesRepo
    {
        private const string MediaContainerPrefix = "dtresource-";
        private static readonly TimeSpan MediaReadUrlLifetime = TimeSpan.FromHours(1);

        private readonly ICloudFileStorageClient _fileStorage;

        public MediaServicesRepo(ICloudFileStorageClient fileStorage, IDocumentCloudCachedServices services)
            : base(services)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        private static string GetMediaContainerName(string orgId)
        {
            if (String.IsNullOrWhiteSpace(orgId))
                throw new ArgumentNullException(nameof(orgId));

            return $"{MediaContainerPrefix}{orgId}".ToLowerInvariant();
        }

        public Task AddMediaResourceRecordAsync(MediaResource resource)
        {
            return CreateDocumentAsync(resource);
        }

        public async Task<InvokeResult> AddMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var result = await _fileStorage.AddFileAsync(GetMediaContainerName(orgId), fileName, data, contentType);
            return result.ToInvokeResult();
        }

        public async Task<InvokeResult<string>> AddToContainerAsync(byte[] data, string containerName, string fileName, string contentType, bool isPublic)
        {
            // isPublic is retained on the media-domain contract for compatibility. Public bucket/object
            // access is an object-storage routing/policy concern and is intentionally not encoded here.
            var result = await _fileStorage.AddFileAsync(containerName.ToLowerInvariant(), fileName, data, contentType);
            if (!result.Successful)
                return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());

            return InvokeResult<string>.Create(result.Result.ToString());
        }

        public async Task<InvokeResult> UpdateMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var result = await _fileStorage.AddFileAsync(GetMediaContainerName(orgId), fileName, data, contentType);
            return result.ToInvokeResult();
        }

        public Task AddOrUpdateMediaResourceAsync(MediaResource updated)
        {
            return UpsertDocumentAsync(updated);
        }

        public Task UpdateMediaResourceRecordAsync(MediaResource updated)
        {
            return UpsertDocumentAsync(updated);
        }

        public async Task<MediaResource> TryGetMediaResourceRecordAsync(string id)
        {
            var record = await GetDocumentAsync(id, false);
            if (record == null)
                return null;

            if (String.IsNullOrEmpty(record.CurrentRevision) && record.IsFileUpload)
            {
                var timeStamp = UtcTimestamp.Now;
                if (record.History.Count > 0)
                {
                    record.CurrentRevision = record.History[0].Id;
                }
                else
                {
                    var history = new MediaResourceHistory
                    {
                        CreatedBy = record.CreatedBy,
                        CreationDate = timeStamp,
                        ContentSize = record.ContentSize,
                        Height = record.Height,
                        Name = "Revision 1",
                        Width = record.Width,
                        Id = Guid.NewGuid().ToId(),
                        StorageReferenceName = record.StorageReferenceName
                    };

                    record.CurrentRevision = history.Id;
                    record.History.Add(history);
                }

                record.LastUpdatedDate = timeStamp;
                await UpsertDocumentAsync(record);
            }

            return record;
        }

        public async Task<MediaResource> GetMediaResourceRecordAsync(string id)
        {
            var record = await GetDocumentAsync(id);
            if (String.IsNullOrEmpty(record.CurrentRevision) && record.IsFileUpload)
            {
                var timeStamp = UtcTimestamp.Now;
                if (record.History.Count > 0)
                {
                    record.CurrentRevision = record.History[0].Id;
                }
                else
                {
                    var history = new MediaResourceHistory
                    {
                        CreatedBy = record.CreatedBy,
                        CreationDate = timeStamp,
                        ContentSize = record.ContentSize,
                        Height = record.Height,
                        Name = "Revision 1",
                        Width = record.Width,
                        Id = Guid.NewGuid().ToId(),
                        StorageReferenceName = record.StorageReferenceName
                    };

                    record.CurrentRevision = history.Id;
                    record.History.Add(history);
                }

                record.LastUpdatedDate = timeStamp;
                await UpsertDocumentAsync(record);
            }

            return record;
        }

        public Task DeleteMediaRecordAsync(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public async Task<InvokeResult<string>> GetMediaReadUrlAsync(string blobReferenceName, string org, System.Threading.CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(blobReferenceName))
                return InvokeResult<string>.FromError("A media storage reference name is required.");

            if (String.IsNullOrWhiteSpace(org))
                return InvokeResult<string>.FromError("An organization ID is required.");

            var result = await _fileStorage.CreateReadUrlAsync(GetMediaContainerName(org), blobReferenceName, MediaReadUrlLifetime);
            if (!result.Successful)
                return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());

            return InvokeResult<string>.Create(result.Result.ToString());
        }

        public Task<InvokeResult<byte[]>> GetMediaAsync(string blobReferenceName, string org)
        {
            return _fileStorage.GetFileAsync(GetMediaContainerName(org), blobReferenceName);
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesForLibraryAsync(string orgId, string libraryId, ListRequest listRequest)
        {
            return QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaLibrary.Id == libraryId,
                med => med.Name,
                listRequest);
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string orgId, string mediaTypeKey, ListRequest listRequest)
        {
            return QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaTypeKey == mediaTypeKey,
                med => med.Name,
                listRequest);
        }

        public async Task DeleteMediaAsync(string blobReferenceName, string orgId)
        {
            var result = await _fileStorage.DeleteFileAsync(GetMediaContainerName(orgId), blobReferenceName);
            if (!result.Successful)
                throw new InvalidOperationException($"Could not delete media '{blobReferenceName}' for organization '{orgId}'.");
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<MediaResourceSummary, MediaResource>(
                qry => qry.OwnerOrganization.Id == orgId,
                qry => qry.Name,
                listRequest);
        }
    }
}
