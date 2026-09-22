using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public sealed class VideoProcessorRequestStore : IVideoProcessorRequestStore
    {
        private const string ContainerPrefix = "video-processor-requests-";
        private static readonly TimeSpan RequestUrlLifetime = TimeSpan.FromMinutes(60);

        private readonly ICloudFileStorageClient _fileStorage;
        private readonly IAdminLogger _logger;

        public VideoProcessorRequestStore(ICloudFileStorageClient fileStorage, IAdminLogger logger)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InvokeResult<VideoProcessorStoredRequest>> SaveAsync<TRequest>(string orgId, string jobType, string requestId, string attemptId, TRequest request, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(orgId))
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Organization ID is required when storing a video processor request.");

            if (String.IsNullOrWhiteSpace(jobType))
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor job type is required.");

            if (String.IsNullOrWhiteSpace(requestId))
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor request ID is required.");

            if (String.IsNullOrWhiteSpace(attemptId))
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor attempt ID is required.");

            if (request == null)
                return InvokeResult<VideoProcessorStoredRequest>.FromError("Video processor request payload is required.");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var containerName = CreateContainerName(orgId);
                var storageReferenceName = CreateStorageReferenceName(jobType, requestId, attemptId);
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var writeResult = await _fileStorage.AddFileAsync(
                    containerName,
                    storageReferenceName,
                    json,
                    "application/json; charset=utf-8");

                if (!writeResult.Successful)
                    return InvokeResult<VideoProcessorStoredRequest>.FromInvokeResult(writeResult.ToInvokeResult());

                var readUrlResult = await _fileStorage.CreateReadUrlAsync(containerName, storageReferenceName, RequestUrlLifetime);
                if (!readUrlResult.Successful)
                    return InvokeResult<VideoProcessorStoredRequest>.FromInvokeResult(readUrlResult.ToInvokeResult());

                return InvokeResult<VideoProcessorStoredRequest>.Create(new VideoProcessorStoredRequest
                {
                    StorageReferenceName = storageReferenceName,
                    BlobUrl = writeResult.Result.ToString(),
                    RequestUrl = readUrlResult.Result.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.AddException("VideoProcessorRequestStore_SaveAsync", ex);
                return InvokeResult<VideoProcessorStoredRequest>.FromException("VideoProcessorRequestStore_SaveAsync", ex);
            }
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
