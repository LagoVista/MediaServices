using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using static LagoVista.Core.Models.AuthorizeResult;

namespace LagoVista.MediaServices.Managers
{
    public class MediaServicesManager : ManagerBase, IMediaServicesManager
    {
        IMediaServicesRepo _mediaRepo;
        IMediaLibraryRepo _libraryRepo;
        ITextToSpeechService _textSpeechService;

        public MediaServicesManager(IMediaServicesRepo mediaRepo, IMediaLibraryRepo libraryRepo,ITextToSpeechService textToSpeechService, IAdminLogger logger, IAppConfig appConfig, IDependencyManager depmanager, ISecurity security) :
            base(logger, appConfig, depmanager, security)
        {
            _mediaRepo = mediaRepo ?? throw new ArgumentNullException(nameof(mediaRepo));
            _libraryRepo = libraryRepo ?? throw new ArgumentNullException(nameof(libraryRepo));
            _textSpeechService = textToSpeechService ?? throw new ArgumentNullException(nameof(textToSpeechService));
        }

        public async Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            if (resource.MediaLibrary != null && String.IsNullOrEmpty(resource.MediaLibrary.Text))
            {
                var library = await _libraryRepo.GetMediaLibrary(resource.MediaLibrary.Id);
                resource.MediaLibrary.Text = library.Name;
            }

            await AuthorizeAsync(resource, AuthorizeActions.Create, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.AddMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResourceSummary>.Create(resource.CreateSummary());
        }

        public static byte[] ScaleImage(byte[] imageBytes, int maxWidth, int maxHeight, string fileType)
        {
            using (var image = Image.Load(imageBytes))
            {
                var ratioX = (double)maxWidth / image.Width;
                var ratioY = (double)maxHeight / image.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));

                using (var ms = new MemoryStream())
                {
                    switch (fileType.ToLower())
                    {
                        case "png":
                            image.Save(ms, new PngEncoder());
                            break;
                        case "jpg":
                        case "jpeg":
                            image.Save(ms, new PngEncoder());
                            break;
                        case "webp":
                            image.Save(ms, new WebpEncoder());
                            break;
                    }
                    return ms.ToArray();
                }
            }
        }

        public async Task<InvokeResult<MediaResource>> ResizeImageAsync(string id, string fileName, int width, int height, string fileType, EntityHeader org, EntityHeader user)
        {
            if(String.IsNullOrEmpty(fileType))
            {
                return InvokeResult<MediaResource>.FromError("File Type is required");
            }

            if(width < 0 || width > 12000 || height < 0 || height > 12000)
            {
                return InvokeResult<MediaResource>.FromError($"Height and Width must be greater than 0 and less than 12000, provided: width={width}, height={height}.");
            }

            switch(fileType)
            {
                case "png":
                case "jpg":
                case "jpeg":
                case "webp":
                    break;
                default: return InvokeResult<MediaResource>.FromError($"Only file types [png, jpg, jpeg, webp] are supported.");
            }

            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            if (org.Id != resource.OwnerOrganization.Id)
                throw new NotAuthorizedException("Not authorized to esize this image resource.");

            var mediaItem = await _mediaRepo.GetMediaAsync(resource.StorageReferenceName, org.Id);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException("Media Contents", resource.StorageReferenceName);
            }

            var newBuffer = ScaleImage(mediaItem.Result, width, height, fileType);
            resource.SetContentType(fileType);
            resource.LastUpdatedDate = DateTime.UtcNow.ToJSONString();
            resource.LastUpdatedBy = user;
            resource.ContentSize = newBuffer.Length;
            await _mediaRepo.UpdateMediaAsync(newBuffer, org.Id, resource.StorageReferenceName, resource.MimeType);

            var fi = new FileInfo(resource.FileName);
            resource.FileName = $"{fileName}.{fileType}";
            resource.Width = width;
            resource.Height = height;

            await _mediaRepo.UpdateMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResource>.Create(resource);
        }

        public async Task<InvokeResult<MediaResource>> AddResourceMediaAsync(String id, Stream stream, string fileName, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "", string originalUrl = "")
        {
            var mediaResource = new MediaResource();
            mediaResource.Id = id;
            mediaResource.CreationDate = DateTime.UtcNow.ToJSONString();
            mediaResource.LastUpdatedDate = mediaResource.CreationDate;
            mediaResource.CreatedBy = user;
            mediaResource.IsPublic = isPublic;
            mediaResource.LastUpdatedBy = user;
            mediaResource.OwnerOrganization = org;
            if (saveResourceRecord)
            {
                mediaResource.Name = "Auto Inserted";
                mediaResource.Key = "autoinserted";
            }

            await AuthorizeAsync(user, org, "addDeviceTypeResource", $"{{mediaItemId:'{id}'}}");

            // Also sets the blob reference name.
            mediaResource.SetContentType(contentType);
            if (mediaResource.MimeType == "application/octet-stream")
                mediaResource.SetContentType(fileName);

            if(mediaResource.ResourceType.Value == MediaResourceTypes.Other)
                mediaResource.ResourceType = EntityHeader<MediaResourceTypes>.Create(mediaResource.MimeType.StartsWith("image") ? MediaResourceTypes.Picture : MediaResourceTypes.Other);

            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            mediaResource.FileName = fileName;
            mediaResource.ContentSize = stream.Length;

            var result = await _mediaRepo.AddMediaAsync(bytes, org.Id, mediaResource.StorageReferenceName, contentType);
            if (result.Successful)
            {
                if (saveResourceRecord)
                    await _mediaRepo.AddOrUpdateMediaResourceAsync(mediaResource);

                return InvokeResult<MediaResource>.Create(mediaResource);
            }
            else
            {
                return InvokeResult<MediaResource>.FromInvokeResult(result);
            }
        }

        public async Task<InvokeResult<MediaResource>> AddResourceMediaAsync(Uri url, string name, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "")
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {   
                return await AddResourceMediaAsync(Guid.NewGuid().ToId(), stream, name, response.Content.Headers.ContentType.ToString(), org, user, saveResourceRecord, isPublic, license, url.ToString());
            }
        }

        public async Task<InvokeResult> DeleteMediaResourceAsync(string id, EntityHeader org, EntityHeader user)
        {
            var record = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(record, AuthorizeActions.Delete, user, org);

            await _mediaRepo.DeleteMediaRecordAsync(id);
            if (record.IsFileUpload)
            {
                await _mediaRepo.DeleteMediaAsync(record.StorageReferenceName, org.Id);
            }

            return InvokeResult.Success;
        }

        public async Task<MediaResource> GetMediaResourceRecordAsync(string id, EntityHeader org, EntityHeader user)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(resource, AuthorizeActions.Read, user, org);
            if(!String.IsNullOrEmpty(resource.MimeType) && resource.MimeType.StartsWith("image"))
            {
                if(!resource.Width.HasValue)
                {
                    var media = await _mediaRepo.GetMediaAsync(resource.StorageReferenceName, org.Id);
                    if(media.Successful)
                    {
                        var image = Image.Load(media.Result);
                        resource.Width = image.Width;
                        resource.Height = image.Height;
                        await _mediaRepo.UpdateMediaResourceRecordAsync(resource);
                    }
                }
            }

            return resource;
        }

        public async Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(string libraryId, string orgId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaResource));
            return await _mediaRepo.GetResourcesForLibrary(orgId, libraryId, listRequest);
        }

        public async Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string mediaTypeKey, string orgId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaResource));
            return await _mediaRepo.GetResourcesForMediaTypeKeyLibrary(orgId, mediaTypeKey, listRequest);
        }


        public async Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            if (!resource.IsPublic && org.Id != resource.OwnerOrganization.Id)
                throw new NotAuthorizedException("Not authorized to download image resource.");

            var mediaItem = await _mediaRepo.GetMediaAsync(resource.StorageReferenceName, org.Id);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }

            return new MediaItemResponse()
            {
                ContentType = resource.MimeType,
                FileName = resource.FileName,
                ImageBytes = mediaItem.Result
            };
        }

        public async Task<MediaItemResponse> GetPublicResourceRecordAsync(string orgId, string id, string lastModified = null)
        {
            var response = new MediaItemResponse();

            var stopWatch = Stopwatch.StartNew();
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            response.LastModified = resource.LastUpdatedDate;
            if (resource == null)
            {
                Console.WriteLine($"ERROR: Could not find media record for orgid: {orgId} - mediaid: {id}");
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });

            if(resource.LastUpdatedDate == lastModified)
            {
                response.NotModified = true;
            }

            response.NotModified = false;

            stopWatch.Restart();
            var mediaItem = await _mediaRepo.GetMediaAsync(resource.StorageReferenceName, orgId);
            if (!mediaItem.Successful)
            {
                Console.WriteLine($"ERROR: Could not find media/image for orgid: {orgId} - mediaid: {resource.StorageReferenceName}");
                throw new RecordNotFoundException("Media File Contents", resource.StorageReferenceName);
            }
            response.Timings.AddRange(mediaItem.Timings);
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });

            response.ContentType = resource.MimeType;
            response.FileName = resource.FileName;
            response.ImageBytes = mediaItem.Result;

            return response;
        }

        public async Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(resource, AuthorizeActions.Update, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.UpdateMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResourceSummary>.Create(resource.CreateSummary());
        }

        public async Task<InvokeResult<MediaResourceSummary>> GenerateAudioAsync(TextToSpeechRequest request, EntityHeader org, EntityHeader user)
        {
            var response = await _textSpeechService.GenerateAudio(request);

            if (response.Successful)
            {
                var resourceId = Guid.NewGuid().ToId();

                var mediaResource = new MediaResource()
                {
                    Id = resourceId,
                    Name = request.Name,
                    Key = request.Key,
                    OwnerOrganization = org,
                    CreationDate = DateTime.UtcNow.ToJSONString(),
                    LastUpdatedDate = DateTime.UtcNow.ToJSONString(),
                    CreatedBy = user,
                    LastUpdatedBy = user,
                    IsPublic = false,
                    ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Audio),
                    MimeType = "audio/mpeg",
                    FileName = $"{request.Name}.mp3",
                    ContentSize = response.Result.Length,
                    StorageReferenceName = $"{resourceId}.mp3",
                    MediaLibrary = request.Library,
                    TextGenerationRequest = request                   
                };

                await _mediaRepo.AddMediaAsync(response.Result, org.Id, mediaResource.StorageReferenceName, mediaResource.MimeType);
                await _mediaRepo.AddMediaResourceRecordAsync(mediaResource);

                return InvokeResult<MediaResourceSummary>.Create(mediaResource.CreateSummary());
            }
            else
            {
                return InvokeResult<MediaResourceSummary>.FromInvokeResult(response.ToInvokeResult());
            }
        }
    }
}
