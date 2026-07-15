using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public sealed class VideoProcessorRequestStore : IVideoProcessorRequestStore
    {
        private const string ContainerPrefix = "video-processor-requests-";
        private static readonly TimeSpan RequestUrlLifetime = TimeSpan.FromMinutes(60);

        private readonly IConnectionSettings _connectionSettings;
        private readonly IAdminLogger _logger;

        public VideoProcessorRequestStore(IMediaServicesConnectionSettings settings, IAdminLogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _connectionSettings = settings.MediaStorageConnection ?? throw new ArgumentNullException(nameof(settings.MediaStorageConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InvokeResult<VideoProcessorStoredRequest>> SaveAsync<TRequest>(string orgId, string jobType, string requestId, string attemptId, TRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(orgId))
            {
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Organization ID is required when storing a video processor request.");
            }

            if (String.IsNullOrWhiteSpace(jobType))
            {
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor job type is required.");
            }

            if (String.IsNullOrWhiteSpace(requestId))
            {
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor request ID is required.");
            }

            if (String.IsNullOrWhiteSpace(attemptId))
            {
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor attempt ID is required.");
            }

            if (request == null)
            {
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor request payload is required.");
            }

            try
            {
                var containerClient = await GetContainerClientAsync(orgId, cancellationToken);
                var storageReferenceName = CreateStorageReferenceName(jobType, requestId, attemptId);
                var blobClient = containerClient.GetBlobClient(storageReferenceName);
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = "application/json; charset=utf-8"
                        }
                    }, cancellationToken);
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    Protocol = SasProtocol.Https,
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.Add(RequestUrlLifetime)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return InvokeResult<VideoProcessorStoredRequest>.Create(new VideoProcessorStoredRequest
                {
                    StorageReferenceName = storageReferenceName,
                    BlobUrl = blobClient.Uri.ToString(),
                    RequestUrl = blobClient.GenerateSasUri(sasBuilder).ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.AddException("VideoProcessorRequestStore_SaveAsync", ex);
                return InvokeResult<VideoProcessorStoredRequest>.FromException("VideoProcessorRequestStore_SaveAsync", ex);
            }
        }

        private async Task<BlobContainerClient> GetContainerClientAsync(string orgId, CancellationToken cancellationToken)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(CreateContainerName(orgId));
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            return containerClient;
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

        private static string CreateStorageReferenceName(string jobType, string requestId, string attemptId)
        {
            var normalizedJobType = NormalizePathPart(jobType);
            var normalizedRequestId = NormalizePathPart(requestId);
            var normalizedAttemptId = NormalizePathPart(attemptId);
            return $"{normalizedJobType}/{normalizedRequestId}/{normalizedAttemptId}.json";
        }

        private static string NormalizePathPart(string value)
        {
            return value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        }
    }
}
