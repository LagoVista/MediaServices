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

            if (!String.IsNullOrWhiteSpace(mediaResource.GetCurrentStorageReferenceName()))
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
                return InvokeResult<VideoAssemblySource>.FromError($"Generated video media resource '{mediaResource.Id}' does not have a storage reference name.");
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

            return InvokeResult<VideoAssemblySource>.Create(CreateSource(mediaResource, readUrlResult.Result));
        }

        private static VideoAssemblySource CreateSource(MediaResource mediaResource, string url)
        {
            return new VideoAssemblySource
            {
                Url = url,
                FileName = mediaResource.FileName,
                ContentType = mediaResource.MimeType
            };
        }
    }
}
