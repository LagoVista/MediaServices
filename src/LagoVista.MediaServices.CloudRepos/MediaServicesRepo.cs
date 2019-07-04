using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using LagoVista.Core;
using System.IO;
using System.Threading.Tasks;
using LagoVista.CloudStorage.DocumentDB;
using LagoVista.MediaServices.Models;
using System.Collections.Generic;

namespace LagoVista.MediaServices.CloudRepos
{
    public class MediaServicesRepo : DocumentDBRepoBase<MediaResource>, IMediaServicesRepo
    {
        ILogger _logger;
        IConnectionSettings _blobConnectionSettings;

        public MediaServicesRepo(IAdminLogger adminLogger, IMediaServicesConnectionSettings settings)
             : base(settings.MediaLibraryConnection.Uri, settings.MediaLibraryConnection.AccessKey, settings.MediaLibraryConnection.ResourceName, adminLogger)
        {
            _logger = adminLogger;
            _blobConnectionSettings = settings.MediaStorageConnection;
        }

        private CloudBlobClient CreateBlobClient(IConnectionSettings settings)
        {
            var baseuri = $"https://{settings.AccountId}.blob.core.windows.net";

            var uri = new Uri(baseuri);
            return new CloudBlobClient(uri, new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(settings.AccountId, settings.AccessKey));
        }

        private async Task<InvokeResult<CloudBlobContainer>> GetStorageContainerAsync(string orgId)
        {
            var client = CreateBlobClient(_blobConnectionSettings);
            var containerName = $"dtresource-{orgId}".ToLower();
            Console.WriteLine(containerName);
            var container = client.GetContainerReference(containerName);
            try
            {
                var options = new BlobRequestOptions()
                {
                    MaximumExecutionTime = TimeSpan.FromSeconds(15)
                };

                var opContext = new OperationContext();
                await container.CreateIfNotExistsAsync(options, opContext);
                return InvokeResult<CloudBlobContainer>.Create(container);
            }
            catch (ArgumentException ex)
            {
                _logger.AddException("DeviceMediaRepo_GetStorageContainerAsync", ex);
                return InvokeResult<CloudBlobContainer>.FromException("DeviceMediaRepo_GetStorageContainerAsync_InitAsync", ex);
            }
            catch (StorageException ex)
            {
                _logger.AddException("DeviceMediaRepo_GetStorageContainerAsync", ex);
                return InvokeResult<CloudBlobContainer>.FromException("DeviceMediaRepo_GetStorageContainerAsync", ex);
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

            var blob = container.GetBlockBlobReference(fileName);
            blob.Properties.ContentType = contentType;

            //TODO: Should really encapsulate the idea of retry of an action w/ error reporting
            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            var stream = new MemoryStream(data);
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream);
                    return InvokeResult.Success;
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("DeviceMediaRepo_AddItemAsync", ex);
                        return InvokeResult.FromException("DeviceMediaRepo_AddItemAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "DeviceMediaRepo_AddItemAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }
                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult.Success;

        }

        public async Task<InvokeResult<byte[]>> GetMediaAsync(string id, string org)
        {
            var result = await GetStorageContainerAsync(org);
            if (!result.Successful)
            {
                return InvokeResult<byte[]>.FromInvokeResult(result.ToInvokeResult());
            }

            var container = result.Result;

            var blob = container.GetBlockBlobReference(id);

            var numberRetries = 5;
            var retryCount = 0;
            var completed = false;
            while (retryCount++ < numberRetries && !completed)
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        return InvokeResult<byte[]>.Create(ms.GetBuffer());
                    }
                }
                catch (Exception ex)
                {
                    if (retryCount == numberRetries)
                    {
                        _logger.AddException("DeviceMediaRepo_AddItemAsync", ex);
                        return InvokeResult<byte[]>.FromException("DeviceMediaRepo_AddItemAsync", ex);
                    }
                    else
                    {
                        _logger.AddCustomEvent(LagoVista.Core.PlatformSupport.LogLevel.Warning, "DeviceMediaRepo_GetMediaAsync", "", ex.Message.ToKVP("exceptionMessage"), ex.GetType().Name.ToKVP("exceptionType"), retryCount.ToString().ToKVP("retryCount"));
                    }

                    await Task.Delay(retryCount * 250);
                }
            }

            return InvokeResult<byte[]>.FromError("Could not load media.");
        }

        public async Task<IEnumerable<MediaResourceSummary>> GetResourcesForLibrary(string orgId, string libraryId)
        {
            var items = await base.QueryAsync(qry => qry.IsPublic == true || (qry.OwnerOrganization.Id == orgId && qry.MediaLibrary.Id == libraryId));

            return from item in items
                   select item.CreateSummary();
        }

        public Task UpdateMediaResourceRecordAsync(MediaResource updated)
        {
            return this.UpsertDocumentAsync(updated);
        }

        public Task<MediaResource> GetMediaResourceRecordAsync(string id)
        {
            return this.GetDocumentAsync(id);
        }
    }
}
