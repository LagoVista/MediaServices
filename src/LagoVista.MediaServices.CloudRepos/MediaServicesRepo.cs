using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using System;
using LagoVista.Core;
using System.IO;
using System.Threading.Tasks;
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Diagnostics;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaServicesRepo : DocumentDBRepoBase<MediaResource>, IMediaServicesRepo
    {
        ILogger _logger;
        IConnectionSettings _blobConnectionSettings;

        public MediaServicesRepo(IAdminLogger adminLogger, IMediaServicesConnectionSettings settings, ICacheProvider cacheProvider)
             : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, adminLogger, cacheProvider)
        {
            _logger = adminLogger;
            _blobConnectionSettings = settings.MediaStorageConnection;
        }

        protected override bool ShouldConsolidateCollections => true;

        private BlobServiceClient CreateBlobClient(IConnectionSettings settings)
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={settings.AccountId};AccountKey={settings.AccessKey}";
            return new BlobServiceClient(connectionString);
        }

        private async Task<InvokeResult<BlobContainerClient>> GetStorageContainerAsync(string suffix, string prefix = "dtresource-", bool isPublic = false)
        {
            var client = CreateBlobClient(_blobConnectionSettings);

            var containerName = $"{prefix}{suffix}".ToLower();

            var containerClient = client.GetBlobContainerClient(containerName);

            try
            {
                var accessType = isPublic ? PublicAccessType.BlobContainer : PublicAccessType.None;
                await containerClient.CreateIfNotExistsAsync(accessType);
                return InvokeResult<BlobContainerClient>.Create(containerClient);
            }
            catch (ArgumentException ex)
            {
                _logger.AddException("MediaServicesRepo_GetStorageContainerAsync", ex);
                return InvokeResult<BlobContainerClient>.FromException("MediaServicesRepo_GetStorageContainerAsync_InitAsync", ex);
            }
            catch (Exception ex)
            {
                _logger.AddException("MediaServicesRepo_GetStorageContainerAsync", ex);
                return InvokeResult<BlobContainerClient>.FromException("MediaServicesRepo_GetStorageContainerAsync", ex);
            }
        }

        public Task AddMediaResourceRecordAsync(MediaResource resource)
        {
            return this.CreateDocumentAsync(resource);
        }

        public async Task<InvokeResult> AddMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var result = await GetStorageContainerAsync(orgId);
            if (!result.Successful)
            {
                return result.ToInvokeResult();
            }

            var container = result.Result;
            var blob = container.GetBlobClient(fileName);

            var sw = Stopwatch.StartNew();

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            var stream = new MemoryStream(data);
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var header = new BlobHttpHeaders { ContentType = contentType };

                    stream.Seek(0, SeekOrigin.Begin);
                    var blobResult = await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = header });
                    var statusCode = blobResult.GetRawResponse().Status;

                    if (statusCode < 200 || statusCode > 299)
                        throw new InvalidOperationException($"Invalid response Code {statusCode}");


                    var elapsedMs = sw.Elapsed.TotalMilliseconds;

                    _logger.AddCustomEvent(LogLevel.Message, "MediaServicesRepo_AddItemAsync", $"Uploaded file {fileName} in {elapsedMs}ms, attempts {retryCount};", elapsedMs.ToString().ToKVP("ms"), contentType.ToKVP("contentType"), retryCount.ToString().ToKVP("retryCount"));


                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("MediaServicesRepo_AddItemAsync", ex);
                        return InvokeResult.FromException("MediaServicesRepo_AddItemAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "MediaServicesRepo_AddItemAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult<string>> AddToContainerAsync(byte[] data, string containerName, string fileName, string contentType, bool isPublic)
        {
            var client = CreateBlobClient(_blobConnectionSettings);

            var result = await GetStorageContainerAsync(containerName, string.Empty, isPublic);
            if (!result.Successful)
            {
                return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());
            }

            var container = result.Result;
            var blob = container.GetBlobClient(fileName);

            var sw = Stopwatch.StartNew();

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            var stream = new MemoryStream(data);
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var header = new BlobHttpHeaders { ContentType = contentType };

                    stream.Seek(0, SeekOrigin.Begin);
                    var blobResult = await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = header });
                    var statusCode = blobResult.GetRawResponse().Status;

                    if (statusCode < 200 || statusCode > 299)
                        throw new InvalidOperationException($"Invalid response Code {statusCode}");

                    var elapsedMs = sw.Elapsed.TotalMilliseconds;

                    _logger.AddCustomEvent(LogLevel.Message, "MediaServicesRepo_AddToContainerAsync", $"Uploaded file {fileName} in {elapsedMs}ms, attempts {retryCount};", elapsedMs.ToString().ToKVP("ms"), contentType.ToKVP("contentType"), retryCount.ToString().ToKVP("retryCount"));
                    var fileUrl = $"https://{_blobConnectionSettings.AccountId}.blob.core.windows.net/{containerName}/{fileName}";
                    return InvokeResult<string>.Create(fileUrl);
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("MediaServicesRepo_AddToContainerAsync", ex);
                        return InvokeResult<string>.FromException("MediaServicesRepo_AddToContainerAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LogLevel.Warning, "MediaServicesRepo_AddToContainerAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<string>.FromError("too many attempts");
        }

        public async Task<InvokeResult> UpdateMediaAsync(byte[] data, string orgId, string fileName, string contentType)
        {
            var result = await GetStorageContainerAsync(orgId);
            if (!result.Successful)
            {
                return result.ToInvokeResult();
            }

            var container = result.Result;
            var blob = container.GetBlobClient(fileName);

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            var stream = new MemoryStream(data);
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var header = new BlobHttpHeaders { ContentType = contentType };

                    stream.Seek(0, SeekOrigin.Begin);
                    var blobResult = await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = header });
                    var statusCode = blobResult.GetRawResponse().Status;

                    if (statusCode < 200 || statusCode > 299)
                        throw new InvalidOperationException($"Invalid response Code {statusCode}");

                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("MediaServicesRepo_AddItemAsync", ex);
                        return InvokeResult.FromException("MediaServicesRepo_AddItemAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "MediaServicesRepo_AddItemAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult.Success;
        }

        public Task AddOrUpdateMediaResourceAsync(MediaResource updated)
        {
            return this.UpsertDocumentAsync(updated);
        }

        public Task UpdateMediaResourceRecordAsync(MediaResource updated)
        {
            return this.UpsertDocumentAsync(updated);
        }

        public async Task<MediaResource> GetMediaResourceRecordAsync(string id)
        {
            var record = await this.GetDocumentAsync(id);
            if (string.IsNullOrEmpty(record.CurrentRevision) && record.IsFileUpload)
            {
                var timeStamp = DateTime.UtcNow.ToJSONString();
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
                        ContentSize = record.ContentSize,
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

        public async Task<InvokeResult<byte[]>> GetMediaAsync(string blobReferenceName, string org)
        {
            var sw = Stopwatch.StartNew();
            var timings = new List<ResultTiming>();

            var result = await GetStorageContainerAsync(org);
            timings.Add(new ResultTiming() { Key = "GetStorageContainer", Ms = sw.Elapsed.TotalMilliseconds });
            sw.Restart();
            if (!result.Successful)
            {
                return InvokeResult<byte[]>.FromInvokeResult(result.ToInvokeResult());
            }

            var container = result.Result;

            var blobClient = container.GetBlobClient(blobReferenceName);
            timings.Add(new ResultTiming() { Key = "GetBlockClient", Ms = sw.Elapsed.TotalMilliseconds });
            sw.Restart();

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    var content = await blobClient.DownloadContentAsync();
                    var buffer = content.Value.Content.ToArray();
                    var ms = sw.Elapsed.TotalMilliseconds;
                    timings.Add(new ResultTiming() { Key = "DownloadContentSize", Ms = ms });

                    _logger.Trace($"[MediaServicesRepo_GetMediaAsync] Downloaded Image: {blobReferenceName} in {ms} ms", ms.ToString().ToKVP("totalMs"), retryCount.ToString().ToKVP("retryCount"));

                    return InvokeResult<byte[]>.Create(buffer, timings);
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("[MediaServicesRepo_AddItemAsync]", ex);
                        return InvokeResult<byte[]>.FromException("[MediaServicesRepo_AddItemAsync]", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "[MediaServicesRepo_GetMediaAsync]", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }

                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not load media.");
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
            var result = await GetStorageContainerAsync(orgId);
            if (!result.Successful)
            {
                throw new Exception("Could not get storage container.");
            }

            var container = result.Result;

            var blobClient = container.GetBlobClient(blobReferenceName);

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    await blobClient.DeleteAsync();
                    completed = true;
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("MediaServicesRepo_DeleteMediaAsync", ex);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Could not get blob reference {blobReferenceName} for organization id {orgId}.");
                        Console.ResetColor();

                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "MediaServicesRepo_DeleteMediaAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }

                    await Task.Delay(retryCount * 250);
                }
            }
        }

        public Task<ListResponse<MediaResourceSummary>> GetResourcesAsync(string orgId, ListRequest listRequest)
        {
            return QuerySummaryAsync<MediaResourceSummary, MediaResource>(qry => qry.OwnerOrganization.Id == orgId, qry => qry.Name, listRequest);
        }
    }
}
