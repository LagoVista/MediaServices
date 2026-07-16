using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.VideoAssembly.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class VideoAssemblyMediaSourceResolver : IVideoAssemblyMediaSourceResolver
    {
        private readonly IMediaServicesRepo _mediaRepo;
        private readonly IVideoProcessorStorageUrlService _videoProcessorStorageUrlService;

        public VideoAssemblyMediaSourceResolver(IMediaServicesRepo mediaRepo, IVideoProcessorStorageUrlService videoProcessorStorageUrlService)
        {
            _mediaRepo = mediaRepo ?? throw new ArgumentNullException(nameof(mediaRepo));
            _videoProcessorStorageUrlService = videoProcessorStorageUrlService ?? throw new ArgumentNullException(nameof(videoProcessorStorageUrlService));
        }

        public async Task<InvokeResult<VideoAssemblySource>> ResolveAsync(MediaResource mediaResource, string orgId, CancellationToken cancellationToken = default)
        {
            if (mediaResource == null)
            {
                return InvokeResult<VideoAssemblySource>.FromError("A media resource is required to resolve a video assembly source.");
            }

            if (String.IsNullOrWhiteSpace(orgId))
            {
                return InvokeResult<VideoAssemblySource>.FromError("An organization ID is required to resolve a video assembly source.");
            }

            if (mediaResource.IsFileUpload)
            {
                return await ResolveUploadedMediaAsync(mediaResource, orgId, cancellationToken);
            }

            if (mediaResource.ResourceType?.Value == MediaResourceTypes.RawVideo ||
                mediaResource.ResourceType?.Value == MediaResourceTypes.Video)
            {
                return await ResolveGeneratedVideoAsync(mediaResource, orgId, cancellationToken);
            }

            if (!String.IsNullOrWhiteSpace(mediaResource.Link))
            {
                return InvokeResult<VideoAssemblySource>.Create(CreateSource(mediaResource, mediaResource.Link));
            }

            return InvokeResult<VideoAssemblySource>.FromError($"Media resource '{mediaResource.Id}' does not have a downloadable assembly source.");
        }

        private async Task<InvokeResult<VideoAssemblySource>> ResolveUploadedMediaAsync(MediaResource mediaResource, string orgId, CancellationToken cancellationToken)
        {
            var storageReferenceName = mediaResource.GetCurrentStorageReferenceName();
            if (String.IsNullOrWhiteSpace(storageReferenceName))
            {
                return InvokeResult<VideoAssemblySource>.FromError($"Uploaded media resource '{mediaResource.Id}' does not have a storage reference name.");
            }

            var readUrlResult = await _mediaRepo.GetMediaReadUrlAsync(storageReferenceName, orgId, cancellationToken);
            if (!readUrlResult.Successful)
            {
                return InvokeResult<VideoAssemblySource>.FromInvokeResult(readUrlResult.ToInvokeResult());
            }

            return InvokeResult<VideoAssemblySource>.Create(CreateSource(mediaResource, readUrlResult.Result));
        }

        private async Task<InvokeResult<VideoAssemblySource>> ResolveGeneratedVideoAsync(MediaResource mediaResource, string orgId, CancellationToken cancellationToken)
        {
            var storageReferenceName = mediaResource.GetCurrentStorageReferenceName();

            if (String.IsNullOrWhiteSpace(storageReferenceName))
            {
                var storageReferenceResult = ResolveLegacyGeneratedVideoStorageReference(mediaResource, orgId);
                if (!storageReferenceResult.Successful)
                {
                    return storageReferenceResult.ToInvokeResult<VideoAssemblySource>();
                }

                storageReferenceName = storageReferenceResult.Result;
                mediaResource.StorageReferenceName = storageReferenceName;
                mediaResource.FileName = String.IsNullOrWhiteSpace(mediaResource.FileName) ? storageReferenceName : mediaResource.FileName;
                mediaResource.MimeType = String.IsNullOrWhiteSpace(mediaResource.MimeType) ? "video/mp4" : mediaResource.MimeType;

                await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);
            }

            var readUrlResult = await _videoProcessorStorageUrlService.CreateReadUrlAsync(orgId, storageReferenceName, cancellationToken);
            if (!readUrlResult.Successful)
            {
                return InvokeResult<VideoAssemblySource>.FromInvokeResult(readUrlResult.ToInvokeResult());
            }

            if (!Uri.TryCreate(readUrlResult.Result, UriKind.Absolute, out var readUri))
            {
                return InvokeResult<VideoAssemblySource>.FromError($"Generated media resource '{mediaResource.Id}' did not produce a valid absolute read URL.");
            }

            if (String.IsNullOrWhiteSpace(readUri.Query) || readUri.Query.IndexOf("sig=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return InvokeResult<VideoAssemblySource>.FromError($"Generated media resource '{mediaResource.Id}' did not produce a signed read URL.");
            }

            return InvokeResult<VideoAssemblySource>.Create(CreateSource(mediaResource, readUrlResult.Result, storageReferenceName));
        }

        private static InvokeResult<string> ResolveLegacyGeneratedVideoStorageReference(MediaResource mediaResource, string orgId)
        {
            if (String.IsNullOrWhiteSpace(mediaResource.Link))
            {
                return InvokeResult<string>.FromError($"Generated video media resource '{mediaResource.Id}' does not have a storage reference name or blob URL.");
            }

            if (!Uri.TryCreate(mediaResource.Link, UriKind.Absolute, out var blobUri))
            {
                return InvokeResult<string>.FromError($"Generated video media resource '{mediaResource.Id}' has an invalid blob URL.");
            }

            var expectedContainerName = $"video-processor-{orgId.Trim().ToLowerInvariant().Replace("_", "-")}";
            var pathSegments = blobUri.AbsolutePath.Trim('/').Split('/');

            if (pathSegments.Length != 2 || !String.Equals(pathSegments[0], expectedContainerName, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult<string>.FromError($"Generated video media resource '{mediaResource.Id}' does not reference the expected video processor container.");
            }

            var storageReferenceName = Uri.UnescapeDataString(pathSegments[1]);
            if (String.IsNullOrWhiteSpace(storageReferenceName))
            {
                return InvokeResult<string>.FromError($"Generated video media resource '{mediaResource.Id}' blob URL does not contain a storage reference name.");
            }

            return InvokeResult<string>.Create(storageReferenceName);
        }

        private static VideoAssemblySource CreateSource(MediaResource mediaResource, string url, string storageReferenceName = null)
        {
            return new VideoAssemblySource
            {
                Url = url,
                FileName = String.IsNullOrWhiteSpace(mediaResource.FileName) ? storageReferenceName : mediaResource.FileName,
                ContentType = String.IsNullOrWhiteSpace(mediaResource.MimeType) && mediaResource.ResourceType?.Value == MediaResourceTypes.RawVideo ? "video/mp4" : mediaResource.MimeType
            };
        }
    }
}
