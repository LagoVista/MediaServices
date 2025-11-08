// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 22076a3ccdb539e06a8b711beda312e2cfc7e8c44b07503ae79924284e64baa0
// IndexVersion: 2
// --- END CODE INDEX META ---
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
using OpenTelemetry.Resources;
using RingCentral;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        IAppConfig _appConfig;
        ICategoryManager _categoryManager;

        public MediaServicesManager(IMediaServicesRepo mediaRepo, ICategoryManager categoryManager, IMediaLibraryRepo libraryRepo,ITextToSpeechService textToSpeechService, IAdminLogger logger, IAppConfig appConfig, IDependencyManager depmanager, ISecurity security) :
            base(logger, appConfig, depmanager, security)
        {
            _mediaRepo = mediaRepo ?? throw new ArgumentNullException(nameof(mediaRepo));
            _libraryRepo = libraryRepo ?? throw new ArgumentNullException(nameof(libraryRepo));
            _textSpeechService = textToSpeechService ?? throw new ArgumentNullException(nameof(textToSpeechService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _categoryManager = categoryManager ?? throw new ArgumentNullException(nameof(categoryManager));
        }

        private void GenerateDownloadLink(MediaResourceSummary summary)
        {
            if(!String.IsNullOrEmpty(summary.Link))
                summary.Link = $"{_appConfig.WebAddress.TrimEnd('/')}/{summary.Link.TrimStart('/')}";  
        }

        public async Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync(MediaResource resource, EntityHeader org, EntityHeader user)
        {
            if (resource.MediaLibrary != null && String.IsNullOrEmpty(resource.MediaLibrary.Text))
            {
                var library = await _libraryRepo.GetMediaLibraryAsync(resource.MediaLibrary.Id);
                resource.MediaLibrary.Text = library.Name;
            }

            resource.SetContentType(resource.FileName);

            await AuthorizeAsync(resource, AuthorizeActions.Create, user, org);
            ValidationCheck(resource, Actions.Create);

            await _mediaRepo.AddMediaResourceRecordAsync(resource);
            var summary = resource.CreateSummary();
            GenerateDownloadLink(summary);
            return InvokeResult<MediaResourceSummary>.Create(summary);
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
            var originalWidth = resource.Width;
            var originalHeight = resource.Height;

            if (org.Id != resource.OwnerOrganization.Id)
                throw new NotAuthorizedException("Not authorized to esize this image resource.");

            var mediaItem = await _mediaRepo.GetMediaAsync(resource.GetCurrentStorageReferenceName(), org.Id);
            if (!mediaItem.Successful)
            {
                throw new RecordNotFoundException("Media Contents", resource.GetCurrentStorageReferenceName());
            }

            var newBuffer = ScaleImage(mediaItem.Result, width, height, fileType);
            // this will generate a new storage reference name.
            resource.SetContentType(fileType);
            resource.LastUpdatedDate = DateTime.UtcNow.ToJSONString();
            resource.LastUpdatedBy = user;
            resource.ContentSize = newBuffer.Length;

            var history = new MediaResourceHistory()
            {
                StorageReferenceName = resource.StorageReferenceName,
                CreatedBy = user,
                Name = $"Revision {resource.History.Count + 1}",
                CreationDate = resource.LastUpdatedDate,
                ContentSize = resource.ContentSize,
                Width = width,
                Height = height,
                Notes = $"Resized from {originalWidth}x{originalHeight} to {width}x{height}",
            };

            resource.History.Insert(0, history);
            resource.CurrentRevision = history.Id;

            await _mediaRepo.AddMediaAsync(newBuffer, org.Id, history.StorageReferenceName, resource.MimeType);

            var fi = new FileInfo(resource.FileName);
            resource.FileName = $"{fileName}.{fileType}";
            resource.Width = width;
            resource.Height = height;

            await _mediaRepo.UpdateMediaResourceRecordAsync(resource);

            return InvokeResult<MediaResource>.Create(resource);
        }

        private async Task<MediaLibrary> CreateOrGetLibraryIfNecessary(string entityTypeName, string timeStamp, EntityHeader org, EntityHeader user)
        {
            var library = await _libraryRepo.GetMediaLibraryByKeyAsync(org.Id, entityTypeName.ToLower());
            if (library == null)
            {
                library = new MediaLibrary();
                library.OwnerOrganization = org;
                library.CreatedBy = user;
                library.LastUpdatedBy = user;
                library.CreationDate = timeStamp;
                library.LastUpdatedDate = timeStamp;
                library.Name = $"{entityTypeName} Images";
                library.Key = $"{entityTypeName.ToLower()}";
                await _libraryRepo.AddMediaLibraryAsync(library);
            }

            return library;
        }

        public async Task<EntityHeader> CreateCategoryIfNecssary(string categoryKey, string categoryName, string timeStamp, EntityHeader org, EntityHeader user)
        {
            Console.WriteLine("=========>>>> Create Category If Necessary!");

            var category = EntityHeader.Create(categoryKey, categoryKey, categoryName);

            var categories = await _categoryManager.GetCategoriesAsync(nameof(MediaResource).ToLower(), ListRequest.CreateForAll(), org, user);
            Console.WriteLine($"=========>>>> Found [{categories.Model.Count()}] records for {nameof(MediaResource).ToLower()} ");


            var existingCategory = categories.Model.FirstOrDefault(ctg => ctg.Key == categoryKey);
            if (existingCategory == null)
            {
                Console.WriteLine($"=========>>>> Did not find match for [{categoryKey}]");

                var newCategory = new Category()
                {
                    CreationDate = timeStamp,
                    LastUpdatedDate = timeStamp,
                    LastUpdatedBy = user,
                    Name = category.Text,
                    Key = category.Key,
                    CreatedBy = user,
                    OwnerOrganization = org,
                    CategoryType = nameof(MediaResource).ToLower(),
                    Category = category,
                };

                await _categoryManager.AddCategoryAsync(newCategory, org, user);

                Console.WriteLine($"=========>>>> Did not find match for [{categoryKey}]");

            }

            return category;
        }

        public async Task<InvokeResult<MediaResource>> AddResourceMediaAsync(String id, Stream stream, string fileName, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, 
            string license = "", string url = "", string responseId = "", string originalPrompt = "", string revisedPrompt = "", string entityTypeName = "", string entityFieldName = "", string size = "", string resourceName = "")
        {
            var timeStamp = DateTime.UtcNow.ToJSONString();

            var mediaResource = new MediaResource();
            mediaResource.Id = id;
            mediaResource.CreationDate = timeStamp;
            mediaResource.LastUpdatedDate = mediaResource.CreationDate;
            mediaResource.CreatedBy = user;
            mediaResource.IsPublic = isPublic;
            mediaResource.LastUpdatedBy = user;
            mediaResource.OwnerOrganization = org;
            if (saveResourceRecord)
            {
                mediaResource.Name = string.IsNullOrEmpty(fileName) ? (String.IsNullOrEmpty(resourceName) ? $"Auto Inserted {DateTime.UtcNow.ToString()}" : resourceName) : fileName;
                mediaResource.Key = $"autoinserted{DateTime.UtcNow.Ticks}";
            }

            if (!String.IsNullOrEmpty(entityTypeName) && !String.IsNullOrEmpty(entityFieldName))
            {
                var categoryKey = $"{entityTypeName.ToLower()}{entityFieldName.ToLower()}";
                var categoryName = $"{entityTypeName} {entityFieldName}";
                mediaResource.MediaLibrary = (await CreateOrGetLibraryIfNecessary(entityTypeName, timeStamp, org, user)).ToEntityHeader();
                mediaResource.Category = await CreateCategoryIfNecssary(categoryKey, categoryName, timeStamp, org, user);
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

            if (!String.IsNullOrEmpty(size))
            {
                var part = size.Split('x');
                if (part.Length != 2)
                    return InvokeResult<MediaResource>.FromError($"Invalid Size: {size}");

                if (int.TryParse(part[0], out int w))
                {
                    mediaResource.Width = w;
                }
                else
                    return InvokeResult<MediaResource>.FromError($"Invalid Width on Size: {size}");

                if (int.TryParse(part[1], out int h))
                {
                    mediaResource.Height = h;
                }
                else
                    return InvokeResult<MediaResource>.FromError($"Invalid Heigth on Size: {size}");
            }

            var history = new MediaResourceHistory()
            {
                StorageReferenceName = mediaResource.StorageReferenceName,
                OriginalPrompt = originalPrompt,
                RevisedPrompt = revisedPrompt,
                ResponseId = responseId,
                CreatedBy = user,
                Name = "Revision 1",
                CreationDate = mediaResource.LastUpdatedDate,
                ContentSize = mediaResource.ContentSize,
                Width = mediaResource.Width,
                Height = mediaResource.Height,
            };

            mediaResource.CurrentRevision = history.Id;
            mediaResource.History.Insert(0, history);

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

        public async Task<InvokeResult<MediaResource>> AddResourceMediaRevisionAsync(String id, Stream stream, string fileName, string contentType, EntityHeader org, EntityHeader user, bool saveResourceRecord = false, bool isPublic = false, string license = "", string url = ""
            , string responseId = "", string originalPrompt = "", string revisedPrompt = "", string size = "")
        {
            var mediaResource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(mediaResource, AuthorizeActions.Update, user, org);
            
            mediaResource.LastUpdatedDate = DateTime.UtcNow.ToJSONString();
            mediaResource.LastUpdatedBy = user;

            mediaResource.SetContentType(contentType);
            if (mediaResource.MimeType == "application/octet-stream")
                mediaResource.SetContentType(fileName);
            if (mediaResource.ResourceType.Value == MediaResourceTypes.Other)
                mediaResource.ResourceType = EntityHeader<MediaResourceTypes>.Create(mediaResource.MimeType.StartsWith("image") ? MediaResourceTypes.Picture : MediaResourceTypes.Other);

            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            mediaResource.FileName = fileName;
            mediaResource.ContentSize = stream.Length;

            if (!String.IsNullOrEmpty(size))
            {
                var part = size.Split('x');
                if(part.Length != 2)
                    return InvokeResult<MediaResource>.FromError($"Invalid Size: {size}");
              
                if(int.TryParse(part[0], out int w))
                {
                    mediaResource.Width = w;
                }
                else
                    return InvokeResult<MediaResource>.FromError($"Invalid Width on Size: {size}");

                if (int.TryParse(part[1], out int h))
                {
                    mediaResource.Height = h;
                }
                else
                    return InvokeResult<MediaResource>.FromError($"Invalid Heigth on Size: {size}");
            }

            var history = new MediaResourceHistory()
            {
                StorageReferenceName = mediaResource.StorageReferenceName,
                OriginalPrompt = originalPrompt,
                RevisedPrompt = revisedPrompt,
                ResponseId = responseId,
                CreatedBy = user,
                CreationDate = mediaResource.LastUpdatedDate,
                ContentSize = mediaResource.ContentSize,
                Name = $"Revision {mediaResource.History.Count + 1}",
                Width = mediaResource.Width,
                Height = mediaResource.Height,
                
            };

            mediaResource.CurrentRevision = history.Id;
            mediaResource.History.Insert(0, history);

            var result = await _mediaRepo.AddMediaAsync(bytes, org.Id, mediaResource.StorageReferenceName, contentType);
            if(result.Successful)
            {
                if(saveResourceRecord)
                {
                    await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);
                }
          
                return InvokeResult<MediaResource>.Create(mediaResource);
            }
            else
            {
                return InvokeResult<MediaResource>.FromInvokeResult(result.ToInvokeResult());
            }
        }


        public async Task<InvokeResult<ImageDetails>> AddImageAsPngAsync(Stream stream, string containerName, bool isPublic, int width, int height)
        {
            var fileName = $"{Guid.NewGuid().ToId()}.png";

            var imageDetails = new ImageDetails()
            {
                Id = Guid.NewGuid().ToId(),
                Width = width,
                Height = height
            };

            var bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);

            var newBuffer = ScaleImage(bytes, width, height, "png");

            var result = await _mediaRepo.AddToContainerAsync(newBuffer, containerName, fileName, "image/png", isPublic);
            if (result.Successful)
            {
                imageDetails.ImageUrl = result.Result;
                return InvokeResult<ImageDetails>.Create(imageDetails);
            }
            else
            {
                return InvokeResult<ImageDetails>.FromInvokeResult(result.ToInvokeResult());
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
                foreach(var history in record.History)
                {
                    await _mediaRepo.DeleteMediaAsync(history.StorageReferenceName, org.Id);
                }
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
                    var media = await _mediaRepo.GetMediaAsync(resource.GetCurrentStorageReferenceName(), org.Id);
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
            var results = await _mediaRepo.GetResourcesForLibraryAsync(orgId, libraryId, listRequest);
            foreach (var res in results.Model)
            {
                GenerateDownloadLink(res);
            }
            return results;
        }

        public async Task<ListResponse<MediaResourceSummary>> GetMediaResourceSummariesAsync(ListRequest listRequest, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, org, typeof(MediaResource));
            var results = await _mediaRepo.GetResourcesAsync(org.Id, listRequest);
            foreach (var res in results.Model)
            {
                GenerateDownloadLink(res);
            }
            return results;
        }

        public async Task<ListResponse<MediaResourceSummary>> GetResourcesForMediaTypeKeyLibrary(string mediaTypeKey, string orgId, ListRequest listRequest, EntityHeader user)
        {
            await AuthorizeOrgAccessAsync(user, orgId, typeof(MediaResource));
            var results = await _mediaRepo.GetResourcesForMediaTypeKeyLibrary(orgId, mediaTypeKey, listRequest);
            foreach (var res in results.Model)
            {
                GenerateDownloadLink(res);
            }

            return results;
        }


        public async Task<MediaItemResponse> GetResourceMediaAsync(string id, EntityHeader org, EntityHeader user)
        {
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            if (!resource.IsPublic && org.Id != resource.OwnerOrganization.Id)
                throw new NotAuthorizedException("Not authorized to download image resource.");

            var mediaItem = await _mediaRepo.GetMediaAsync(resource.GetCurrentStorageReferenceName(), org.Id);
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
            if (resource == null)
            {
                Console.WriteLine($"ERROR: Could not find media record for orgid: {orgId} - mediaid: {id}");
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }

            response.LastModified = resource.LastUpdatedDate;
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });

            var entityLastUpdated = resource.LastUpdatedDate.ToDateTime();
            var rountedLastupdate = entityLastUpdated.Subtract(TimeSpan.FromMilliseconds(entityLastUpdated.Millisecond)).ToJSONString();
            response.NotModified = false;

            if (rountedLastupdate == lastModified)
            {
                response.NotModified = true;
            }

            stopWatch.Restart();
            var mediaItem = await _mediaRepo.GetMediaAsync(resource.GetCurrentStorageReferenceName(), orgId);
            if (!mediaItem.Successful)
            {
                Console.WriteLine($"ERROR: Could not find media/image for orgid: {orgId} - mediaid: {resource.StorageReferenceName}");
                throw new RecordNotFoundException("Media File Contents", resource.GetCurrentStorageReferenceName());
            }
            response.Timings.AddRange(mediaItem.Timings);
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });
            response.AiResponseId = resource.ResponseId;
            response.ContentType = resource.MimeType;
            response.FileName = resource.FileName;
            response.ImageBytes = mediaItem.Result;

            return response;
        }


        public async Task<MediaItemResponse> GetMediaRevisionAsync(string id, string revisionId, EntityHeader org, EntityHeader user)
        {
            var response = new MediaItemResponse();

            var stopWatch = Stopwatch.StartNew();
            var resource = await _mediaRepo.GetMediaResourceRecordAsync(id);

            if (resource == null)
            {
                Console.WriteLine($"ERROR: Could not find media record formediaid: {id}");
                throw new RecordNotFoundException(nameof(MediaResource), id);
            }
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });
            stopWatch.Restart();

            var revision = resource.History.FirstOrDefault(rev=> rev.Id == revisionId); 

            await AuthorizeAsync(resource, AuthorizeActions.Read, user, org);
     
            var mediaItem = await _mediaRepo.GetMediaAsync(revision.StorageReferenceName, org.Id);
            if (!mediaItem.Successful)
            {
                Console.WriteLine($"ERROR: Could not find media/image for orgid: {org.Id} - mediaid: {resource.StorageReferenceName}");
                throw new RecordNotFoundException("Media File Contents", resource.GetCurrentStorageReferenceName());
            }
            response.Timings.AddRange(mediaItem.Timings);
            response.Timings.Add(new ResultTiming() { Key = "GetMediaResourceRecord", Ms = stopWatch.Elapsed.TotalMilliseconds });

            response.AiResponseId = revision.ResponseId;
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

        public async Task<InvokeResult<MediaResourceSummary>> UpdateGeneratedAudioAsync(string requestId, TextToSpeechRequest request, EntityHeader org, EntityHeader user)
        {
            var mediaResource = await _mediaRepo.GetMediaResourceRecordAsync(requestId);
            var response = await _textSpeechService.GenerateAudio(request);

            if (response.Successful)
            {
                mediaResource.Name = request.Name;
                mediaResource.LastUpdatedDate = DateTime.UtcNow.ToJSONString();
                mediaResource.LastUpdatedBy = user;
                mediaResource.TextGenerationRequest = request;


                mediaResource.RegenerateStorageReferenceName();

                var history = new MediaResourceHistory()
                {
                    StorageReferenceName = mediaResource.StorageReferenceName,
                    CreatedBy = user,
                    Name = $"Revision {mediaResource.History.Count + 1}",
                    CreationDate = mediaResource.LastUpdatedDate,
                    ContentSize = mediaResource.ContentSize,
                    OriginalPrompt = request.Text,
                    Notes = $"Updated Audio",
                    TextGenerationRequest = request,
                };

                mediaResource.History.Insert(0, history);

                await _mediaRepo.AddMediaAsync(response.Result, org.Id, mediaResource.GetCurrentStorageReferenceName(), mediaResource.MimeType);
                await _mediaRepo.UpdateMediaResourceRecordAsync(mediaResource);
                var summary = mediaResource.CreateSummary();
                GenerateDownloadLink(summary);
                return InvokeResult<MediaResourceSummary>.Create(summary);
            }
            else
            {
                return InvokeResult<MediaResourceSummary>.FromInvokeResult(response.ToInvokeResult());
            }
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

                mediaResource.SetContentType("mp3");

                var history = new MediaResourceHistory()
                {
                    StorageReferenceName = mediaResource.StorageReferenceName,
                    CreatedBy = user,
                    Name = $"Revision {mediaResource.History.Count + 1}",
                    CreationDate = mediaResource.LastUpdatedDate,
                    ContentSize = mediaResource.ContentSize,
                    OriginalPrompt = request.Text,
                    Notes = $"Generated Audio",
                    TextGenerationRequest = request,
                };

                mediaResource.History.Add(history);

                await _mediaRepo.AddMediaAsync(response.Result, org.Id, mediaResource.GetCurrentStorageReferenceName(), mediaResource.MimeType);
                await _mediaRepo.AddMediaResourceRecordAsync(mediaResource);
                var summary = mediaResource.CreateSummary();
                GenerateDownloadLink(summary);
                return InvokeResult<MediaResourceSummary>.Create(summary);
            }
            else
            {
                return InvokeResult<MediaResourceSummary>.FromInvokeResult(response.ToInvokeResult());
            }
        }

        public async Task<InvokeResult<MediaResourceSummary>> UpdateAudioAsync(string resourceId, TextToSpeechRequest request, EntityHeader org, EntityHeader user)
        {
            var response = await _textSpeechService.GenerateAudio(request);
            if (response.Successful)
            {
                var resource = await _mediaRepo.GetMediaResourceRecordAsync(resourceId);
                if (resource.OwnerOrganization.Id != org.Id)
                    throw new NotAuthorizedException("Org mismatch");

                resource.RegenerateStorageReferenceName();

                var history = new MediaResourceHistory()
                {
                    StorageReferenceName = resource.StorageReferenceName,
                    CreatedBy = user,
                    Name = $"Revision {resource.History.Count + 1}",
                    CreationDate = resource.LastUpdatedDate,
                    ContentSize = resource.ContentSize,
                    OriginalPrompt = request.Text,
                    Notes = $"Updated Audio",
                    TextGenerationRequest = request,
                };

                resource.History.Insert(0, history);

                await _mediaRepo.AddMediaAsync(response.Result, org.Id, resource.GetCurrentStorageReferenceName(), resource.MimeType);

                resource.LastUpdatedDate = DateTime.UtcNow.ToJSONString();
                resource.LastUpdatedBy = user;

                var summary = resource.CreateSummary();
                return InvokeResult<MediaResourceSummary>.Create(summary);
            }
            else
            {
                return InvokeResult<MediaResourceSummary>.FromInvokeResult(response.ToInvokeResult());
            }
        }

        public Task<InvokeResult<List<EntityHeader>>> GetVoicesAsync(string languageCode, EntityHeader org, EntityHeader user)
        {
            return _textSpeechService.GetVoicesForLanguageAsync(languageCode);
        }

        public async Task<Stream> PreviewAudioAsync(TextToSpeechRequest request, EntityHeader org, EntityHeader user)
        {
             var result = await _textSpeechService.GenerateAudio(request);
            if (result.Successful)
                return new MemoryStream(result.Result);

            throw new Exception($"Could not get preview: {result.ErrorMessage}");;
        }

        public async Task<InvokeResult<MediaResource>> CloneMediaResourceAsync(string id, string entityTypeName, string entityFieldName, string resourceName, EntityHeader orgEntityHeader, EntityHeader userEntityHeader)
        {
            var timeStamp = DateTime.UtcNow.ToJSONString();

            var mediaResource = await _mediaRepo.GetMediaResourceRecordAsync(id);
            await AuthorizeAsync(mediaResource, AuthorizeActions.Create, userEntityHeader, orgEntityHeader);
            mediaResource.Id = Guid.NewGuid().ToId();
            mediaResource.CreatedBy = userEntityHeader;
            mediaResource.LastUpdatedBy = userEntityHeader;
            mediaResource.CreationDate = timeStamp;
            mediaResource.LastUpdatedDate = timeStamp;
            mediaResource.ClonedFromId = EntityHeader.Create(id, mediaResource.Key, mediaResource.Name);
            mediaResource.ClonedFromOrg = orgEntityHeader;

            if (!String.IsNullOrEmpty(entityTypeName) && !String.IsNullOrEmpty(entityFieldName))
            {
                var categoryKey = $"{entityTypeName.ToLower()}{entityFieldName.ToLower()}";
                var categoryName = $"{entityTypeName} {entityFieldName}";
                mediaResource.MediaLibrary = (await CreateOrGetLibraryIfNecessary(entityTypeName, timeStamp, orgEntityHeader, userEntityHeader)).ToEntityHeader();
                mediaResource.Category = await CreateCategoryIfNecssary(categoryKey, categoryName, timeStamp, orgEntityHeader, userEntityHeader);
            }

            await _mediaRepo.AddMediaResourceRecordAsync(mediaResource);

            return InvokeResult<MediaResource>.Create(mediaResource);
        }
    }
}
