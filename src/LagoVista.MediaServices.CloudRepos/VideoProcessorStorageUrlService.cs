using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public sealed class VideoProcessorStorageUrlService : IVideoProcessorStorageUrlService
    {
        private const string ContainerPrefix = "video-processor-";
        private static readonly TimeSpan WriteUrlLifetime = TimeSpan.FromHours(6);
        private static readonly TimeSpan ReadUrlLifetime = TimeSpan.FromHours(2);

        private readonly IConnectionSettings _connectionSettings;
        private readonly IAdminLogger _logger;

        public VideoProcessorStorageUrlService(IMediaServicesConnectionSettings settings, IAdminLogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _connectionSettings = settings.MediaStorageConnection ?? throw new ArgumentNullException(nameof(settings.MediaStorageConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InvokeResult<VideoProcessorStorageDestination>> CreateWriteDestinationAsync(string orgId, string storageReferenceName, string contentType, CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRequest(orgId, storageReferenceName);
            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<VideoProcessorStorageDestination>();
            }

            if (String.IsNullOrWhiteSpace(contentType))
            {
                return InvokeResult<VideoProcessorStorageDestination>.FromError("Content type is required when creating a video processor write destination.");
            }

            try
            {
                var blobClientResult = await GetBlobClientAsync(orgId, storageReferenceName, cancellationToken);
                if (!blobClientResult.Successful)
                {
                    return blobClientResult.ToInvokeResult<VideoProcessorStorageDestination>();
                }

                var blobClient = blobClientResult.Result;
                var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                var expiresOn = DateTimeOffset.UtcNow.Add(WriteUrlLifetime);
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    StartsOn = startsOn,
                    ExpiresOn = expiresOn,
                    ContentType = contentType
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

                return InvokeResult<VideoProcessorStorageDestination>.Create(new VideoProcessorStorageDestination
                {
                    StorageReferenceName = storageReferenceName,
                    BlobUrl = blobClient.Uri.ToString(),
                    UploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.AddException("VideoProcessorStorageUrlService_CreateWriteDestinationAsync", ex);
                return InvokeResult<VideoProcessorStorageDestination>.FromException("VideoProcessorStorageUrlService_CreateWriteDestinationAsync", ex);
            }
        }

        public async Task<InvokeResult<string>> CreateReadUrlAsync(string orgId, string storageReferenceName, CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRequest(orgId, storageReferenceName);
            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<string>();
            }

            try
            {
                var blobClientResult = await GetBlobClientAsync(orgId, storageReferenceName, cancellationToken);
                if (!blobClientResult.Successful)
                {
                    return blobClientResult.ToInvokeResult<string>();
                }

                var blobClient = blobClientResult.Result;
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.Add(ReadUrlLifetime)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return InvokeResult<string>.Create(blobClient.GenerateSasUri(sasBuilder).ToString());
            }
            catch (Exception ex)
            {
                _logger.AddException("VideoProcessorStorageUrlService_CreateReadUrlAsync", ex);
                return InvokeResult<string>.FromException("VideoProcessorStorageUrlService_CreateReadUrlAsync", ex);
            }
        }

        private async Task<InvokeResult<BlobClient>> GetBlobClientAsync(string orgId, string storageReferenceName, CancellationToken cancellationToken)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerName = CreateContainerName(orgId);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            return InvokeResult<BlobClient>.Create(containerClient.GetBlobClient(storageReferenceName));
        }

        private BlobServiceClient CreateBlobServiceClient()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_connectionSettings.AccountId};AccountKey={_connectionSettings.AccessKey}";
            return new BlobServiceClient(connectionString);
        }

        private static string CreateContainerName(string orgId)
        {
            var normalizedOrgId = orgId.Trim().ToLowerInvariant().Replace("_", "-");
            return $"{ContainerPrefix}{normalizedOrgId}";
        }

        private static InvokeResult ValidateRequest(string orgId, string storageReferenceName)
        {
            if (String.IsNullOrWhiteSpace(orgId))
            {
                return InvokeResult.FromError("Organization ID is required when creating a video processor storage URL.");
            }

            if (String.IsNullOrWhiteSpace(storageReferenceName))
            {
                return InvokeResult.FromError("Storage reference name is required when creating a video processor storage URL.");
            }

            return InvokeResult.Success;
        }
    }
}
