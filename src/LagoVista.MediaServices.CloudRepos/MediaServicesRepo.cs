// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 826c150e8f7f6fcafa4cd121967be2dacecb4fb698f8cde9e5ed0fe1b5015706
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaServicesRepo : DocumentDBRepoBase<MediaResource>, IMediaServicesRepo
    {
        private readonly ILogger _logger;
        private readonly ICloudFileStorageClient _fileStorage;

        public MediaServicesRepo(ICloudFileStorageClient fileStorage, IDocumentCloudCachedServices services) : base(services)
        {
            _logger = services.AdminLogger;
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        private static string GetStorageContainerName(string suffix, string prefix = "dtresource-")
        {
            if (String.IsNullOrWhiteSpace(suffix))
                throw new ArgumentNullException(nameof(suffix));

            return $"{prefix}{suffix}".ToLowerInvariant();
        }

        public Task AddMediaResourceRecordAsync(MediaResource resource)
        {
            return this.CreateDocumentAsync(resource);
        }

        public async Task<InvokeResult> AddMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var sw = Stopwatch.StartNew();
            var result = await _fileStorage.AddFileAsync(GetStorageContainerName(orgId), fileName, data, contentType);

            if (result.Successful)
            {
                _logger.AddCustomEvent(
                    LogLevel.Message,
                    "MediaServicesRepo_AddMediaAsync",
                    $"Uploaded file {fileName} to S3 storage in {sw.Elapsed.TotalMilliseconds}ms.",
                    sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"),
                    contentType.ToKVP("contentType"));
            }

            return result.ToInvokeResult();
        }

        public async Task<InvokeResult<string>> AddToContainerAsync(byte[] data, string containerName, string fileName, string contentType, bool isPublic)
        {
            // Public/private bucket policy is an infrastructure concern for S3/SeaweedFS.
            // Keep the flag for contract compatibility without leaking provider-specific ACL APIs here.
            _ = isPublic;

            var sw = Stopwatch.StartNew();
            var result = await _fileStorage.AddFileAsync(GetStorageContainerName(containerName, String.Empty), fileName, data, contentType);
            if (!result.Successful)
                return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());

            _logger.AddCustomEvent(
                LogLevel.Message,
                "MediaServicesRepo_AddToContainerAsync",
                $"Uploaded file {fileName} to S3 storage in {sw.Elapsed.TotalMilliseconds}ms.",
                sw.Elapsed.TotalMilliseconds.ToString().ToKVP("ms"),
                contentType.ToKVP("contentType"));

            return InvokeResult<string>.Create(result.Result.ToString());
        }

        public async Task<InvokeResult> UpdateMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var result = await _fileStorage.AddFileAsync(GetStorageContainerName(orgId), fileName, data, contentType);
            return result.ToInvokeResult();
        }

        public Task AddOrUpdateMediaResourceAsync(MediaResource updated)
        {
            return this.UpsertDocumentAsync(updated);
        }

        public Task UpdateMediaResourceRecordAsync(MediaResource updated)
        {
            return this.UpsertDocumentAsync(updated);
        }

        public async Task<MediaResource> TryGetMediaResourceRecordAsync(string id)
        {
            var record = await this.GetDocumentAsync(id, false);
            if (record == null)
                return null;

            if (string.IsNullOrEmpty(record.CurrentRevision) && record.IsFileUpload)
            {
                var timeStamp = UtcTimestamp.Now;
                if (record.History.Count > 0)
                {
                    record.CurrentRevision = record.History[0].Id;
                }
                else
                {
                    var history = new MediaResourceHistory()
                    {
                        CreatedBy = record.CreatedBy,
                        CreationDate = timeStamp,
                        FileName = record.FileName,
                        MimeType = record.MimeType,
                        ContentSize = record.ContentSize,
                        ContentSha256 = record.ContentSha256,
                        Height = record.Height,
                        Name = $"Revision 1",
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
            var record = await this.GetDocumentAsync(id);
            if (string.IsNullOrEmpty(record.CurrentRevision) && record.IsFileUpload)
            {
                var timeStamp = UtcTimestamp.Now;
                if (record.History.Count > 0)
                {
                    record.CurrentRevision = record.History[0].Id;
                }
                else
                {
                    var history = new MediaResourceHistory()
                    {
                        CreatedBy = record.CreatedBy,
                        CreationDate = timeStamp,
                        FileName = record.FileName,
                        MimeType = record.MimeType,
                        ContentSize = record.ContentSize,
                        ContentSha256 = record.ContentSha256,
                        Height = record.Height,
                        Name = $"Revision 1",
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
            return this.DeleteDocumentAsync(id);
        }

        public Task<InvokeResult<string>> GetMediaReadUrlAsync(string blobReferenceName, string org, System.Threading.CancellationToken cancellationToken = default)
        {
            return GetMediaReadUrlAsync(blobReferenceName, org, VideoProcessorStorageUrlScope.Public, cancellationToken);
        }

        public Task<InvokeResult<string>> GetMediaReadUrlAsync(string blobReferenceName, string org, VideoProcessorStorageUrlScope scope, System.Threading.CancellationToken cancellationToken = default)
        {
            return GetMediaReadUrlAsync(blobReferenceName, org, TimeSpan.FromHours(1), scope, cancellationToken);
        }

        public async Task<InvokeResult<string>> GetMediaReadUrlAsync(string blobReferenceName, string org, TimeSpan lifetime, VideoProcessorStorageUrlScope scope, System.Threading.CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(blobReferenceName))
                return InvokeResult<string>.FromError("A media storage reference name is required.");

            if (String.IsNullOrWhiteSpace(org))
                return InvokeResult<string>.FromError("An organization ID is required.");

            if (lifetime <= TimeSpan.Zero)
                return InvokeResult<string>.FromError("A positive media read URL lifetime is required.");

            cancellationToken.ThrowIfCancellationRequested();

            var result = await _fileStorage.CreateReadUrlAsync(
                GetStorageContainerName(org),
                blobReferenceName,
                lifetime,
                scope == VideoProcessorStorageUrlScope.Internal ? CloudStorageUrlScope.Internal : CloudStorageUrlScope.Public);

            if (!result.Successful)
                return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());

            return InvokeResult<string>.Create(result.Result.ToString());
        }

        public async Task<InvokeResult<byte[]>> GetMediaAsync(string blobReferenceName, string org)
        {
            var sw = Stopwatch.StartNew();
            var result = await _fileStorage.GetFileAsync(GetStorageContainerName(org), blobReferenceName);

            if (!result.Successful)
                return InvokeResult<byte[]>.FromInvokeResult(result.ToInvokeResult());

            var ms = sw.Elapsed.TotalMilliseconds;
            var timings = new List<ResultTiming>
            {
                new ResultTiming() { Key = "GetS3Object", Ms = ms }
            };

            _logger.Trace(
                $"[MediaServicesRepo_GetMediaAsync] Downloaded media from S3: {blobReferenceName} in {ms} ms",
                ms.ToString().ToKVP("totalMs"));

            return InvokeResult<byte[]>.Create(result.Result, timings);
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesForLibraryAsync(string orgId, string libraryId, ListRequest listRequest)
        {
            return base.QuerySummaryAsync<MediaResourceSummary, MediaResource>(qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaLibrary.Id == libraryId, med => med.Name, listRequest);
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string orgId, string mediaTypeKey, ListRequest listRequest)
        {
            return base.QuerySummaryAsync<MediaResourceSummary, MediaResource>(qry => (qry.IsPublic == true || qry.OwnerOrganization.Id == orgId) && qry.MediaTypeKey == mediaTypeKey, med => med.Name, listRequest);
        }

        public async Task DeleteMediaAsync(string blobReferenceName, string orgId)
        {
            var result = await _fileStorage.DeleteFileAsync(GetStorageContainerName(orgId), blobReferenceName);
            if (!result.Successful)
            {
                _logger.AddCustomEvent(
                    LogLevel.Warning,
                    "MediaServicesRepo_DeleteMediaAsync",
                    "Could not delete media from S3 storage.",
                    blobReferenceName.ToKVP("storageReferenceName"),
                    orgId.ToKVP("organizationId"));
            }
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<MediaResourceSummary, MediaResource>(qry => qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }
    }
}
