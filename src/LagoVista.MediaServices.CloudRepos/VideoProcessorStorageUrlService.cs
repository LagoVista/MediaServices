using LagoVista.CloudStorage.Interfaces;
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
        private static readonly TimeSpan WriteUrlLifetime = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan ReadUrlLifetime = TimeSpan.FromMinutes(60);

        private readonly ICloudFileStorageClient _fileStorage;
        private readonly IAdminLogger _logger;

        public VideoProcessorStorageUrlService(ICloudFileStorageClient fileStorage, IAdminLogger logger)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<InvokeResult<VideoProcessorStorageDestination>> CreateWriteDestinationAsync(string orgId, string storageReferenceName, string contentType, CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRequest(orgId, storageReferenceName);
            if (!validationResult.Successful)
                return validationResult.ToInvokeResult<VideoProcessorStorageDestination>();

            if (String.IsNullOrWhiteSpace(contentType))
                return InvokeResult<VideoProcessorStorageDestination>.FromError("Content type is required when creating a video processor write destination.");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _fileStorage.CreateWriteUrlAsync(CreateContainerName(orgId), storageReferenceName, contentType, WriteUrlLifetime);
                if (!result.Successful)
                    return InvokeResult<VideoProcessorStorageDestination>.FromInvokeResult(result.ToInvokeResult());

                var uploadUrl = result.Result.ToString();
                return InvokeResult<VideoProcessorStorageDestination>.Create(new VideoProcessorStorageDestination
                {
                    StorageReferenceName = storageReferenceName,
                    BlobUrl = result.Result.GetLeftPart(UriPartial.Path),
                    UploadUrl = uploadUrl
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
                return validationResult.ToInvokeResult<string>();

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _fileStorage.CreateReadUrlAsync(CreateContainerName(orgId), storageReferenceName, ReadUrlLifetime);
                if (!result.Successful)
                    return InvokeResult<string>.FromInvokeResult(result.ToInvokeResult());

                return InvokeResult<string>.Create(result.Result.ToString());
            }
            catch (Exception ex)
            {
                _logger.AddException("VideoProcessorStorageUrlService_CreateReadUrlAsync", ex);
                return InvokeResult<string>.FromException("VideoProcessorStorageUrlService_CreateReadUrlAsync", ex);
            }
        }

        private static string CreateContainerName(string orgId)
        {
            var normalizedOrgId = orgId.Trim().ToLowerInvariant().Replace("_", "-");
            return $"{ContainerPrefix}{normalizedOrgId}";
        }

        private static InvokeResult ValidateRequest(string orgId, string storageReferenceName)
        {
            if (String.IsNullOrWhiteSpace(orgId))
                return InvokeResult.FromError("Organization ID is required when creating a video processor storage URL.");

            if (String.IsNullOrWhiteSpace(storageReferenceName))
                return InvokeResult.FromError("Storage reference name is required when creating a video processor storage URL.");

            return InvokeResult.Success;
        }
    }
}
