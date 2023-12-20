using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using System;
using System.Linq;
using LagoVista.Core;
using System.IO;
using System.Threading.Tasks;
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;
using LagoVista.CloudStorage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LagoVista.Core.Models.UIMetaData;

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

        private async Task<InvokeResult<BlobContainerClient>> GetStorageContainerAsync(string orgId)
        {
            var client = CreateBlobClient(_blobConnectionSettings);

            var containerName = $"dtresource-{orgId}".ToLower();

            var containerClient = client.GetBlobContainerClient(containerName);


            try
            {
                await containerClient.CreateIfNotExistsAsync();
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

        public Task<MediaResource> GetMediaResourceRecordAsync(string id)
        {
            return this.GetDocumentAsync(id);
        }

        public Task DeleteMediaRecordAsync(string id)
        {
            return this.DeleteDocumentAsync(id);
        }


        public async Task<InvokeResult<byte[]>> GetMediaAsync(string blobReferenceName, string org)
        {
            var result = await GetStorageContainerAsync(org);
            if (!result.Successful)
            {
                return InvokeResult<byte[]>.FromInvokeResult(result.ToInvokeResult());
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
                    var content = await blobClient.DownloadContentAsync();
                    return InvokeResult<byte[]>.Create(content.Value.Content.ToArray());
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("MediaServicesRepo_AddItemAsync", ex);
                        return InvokeResult<byte[]>.FromException("MediaServicesRepo_AddItemAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "MediaServicesRepo_GetMediaAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }

                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not load media.");
        }

        public  Task<ListResponse<MediaResourceSummary>> GetResourcesForLibrary(string orgId, string libraryId, ListRequest listRequest)
        {
            return base.QuerySummaryAsync<MediaResourceSummary, MediaResource>(qry => qry.IsPublic == true || (qry.OwnerOrganization.Id == orgId && qry.MediaLibrary.Id == libraryId), med=>med.Name, listRequest);
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
    }
}
