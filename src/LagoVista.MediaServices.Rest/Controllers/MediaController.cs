// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: f67953aa992e4218d213bf3fde05b140726e0a461563c6b6bd5cbfbb7fa59746
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models.UIMetaData;
using LagoVista.MediaServices.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using LagoVista.Core;
using LagoVista.Core.Validation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Managers;
using LagoVista.MediaServices.Rest.McpWidgets;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using LagoVista.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;

namespace LagoVista.MediaServices.Rest.Controllers
{
    /// <summary>
    /// The Media Controller will let users upload images and download them.
    /// </summary>
    public class MediaController : LagoVistaBaseController
    {

        IMediaServicesManager _mediaServicesManager;
        IImageAssemblyManager _imageAssemblyManager;
        IMediaLibraryManager _libraryManager;

        /// <summary>
        /// The Media Controller will let users upload images and download them.
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        /// <param name="mediaServiceManager"></param>
        public MediaController(UserManager<AppUser> userManager, IAdminLogger logger, IMediaLibraryManager libraryManager, IMediaServicesManager mediaServiceManager, IImageAssemblyManager imageAssemblyManager) : base(userManager, logger)
        {
            _mediaServicesManager = mediaServiceManager;
            _imageAssemblyManager = imageAssemblyManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        ///  Meida Resources - Create new record.
        /// </summary>
        /// <returns></returns>
        [McpEndpoint("media-resources-factory", Description = "Create an unsaved media resource template for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/resource/factory")]
        public DetailResponse<MediaResource> CreateDeviceResource()
        {
            var response = DetailResponse<MediaResource>.Create();
            response.Model.Id = Guid.NewGuid().ToId();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            return response;
        }

        [McpEndpoint("media-resources-factory-for-library", Description = "Create an unsaved media resource template for a selected media library.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/library/{libraryid}/resource/factory")]
        public async Task<DetailResponse<MediaResource>> CreateDeviceResource(string libraryid)
        {
            var library = await _libraryManager.GetMediaLibraryAsync(libraryid, OrgEntityHeader, UserEntityHeader);
            var response = DetailResponse<MediaResource>.Create();
            response.Model.MediaLibrary = library.ToEntityHeader();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            return response;
        }


        [HttpGet("/api/media/imagegeneration/factory")]
        public DetailResponse<ImageGenerationRequest> CreateImageGenerationRequest(string entityTypeName, string entityFieldName, string entityId)
        {
            var request = new ImageGenerationRequest()
            {
                EntityFieldName = entityFieldName,
                EntityTypeName = entityTypeName,
                EntityId = entityId,
            };

            return DetailResponse<ImageGenerationRequest>.Create(request);
        }



        /// <summary>
        /// Media Resources - Add a Media Resource Record
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        [McpEndpoint("media-resources-create", Description = "Create a media resource record in the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/resource")]
        public Task<InvokeResult<MediaResourceSummary>> AddMediaResourceRecordAsync([FromBody] MediaResource resource)
        {
            return _mediaServicesManager.AddMediaResourceRecordAsync(resource, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Add a Media Resource Record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [McpEndpoint("media-resources-get", Description = "Get a media resource record by id using existing organization authorization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/resource/{id}")]
        public async Task<DetailResponse<MediaResource>> GetMediaResource(string id)
        {
            var resource = await _mediaServicesManager.GetMediaResourceRecordAsync(id, OrgEntityHeader, UserEntityHeader);
            return DetailResponse<MediaResource>.Create(resource);
        }

        /// <summary>
        /// Media Resources - Delete the Media Resource Record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [McpEndpoint("media-resources-delete", Description = "Delete a media resource record by id.", FamilyId = "media")]
        [Authorize]
        [HttpDelete("/api/media/resource/{id}")]
        public Task<InvokeResult> DeleteMediaResource(string id)
        {
            return _mediaServicesManager.DeleteMediaResourceAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Update Media Resource Record.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        [McpEndpoint("media-resources-update", Description = "Update an existing media resource record.", FamilyId = "media")]
        [Authorize]
        [HttpPut("/api/media/resource")]
        public Task<InvokeResult<MediaResourceSummary>> UpdateMediaResourceRecordAsync([FromBody] MediaResource resource)
        {
            SetUpdatedProperties(resource);
            return _mediaServicesManager.UpdateMediaResourceRecordAsync(resource, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/resource/texttospeech")]
        public Task<InvokeResult<MediaResourceSummary>> AddTextToSpeechAsync([FromBody] TextToSpeechRequest tts)
        {
            return _mediaServicesManager.GenerateAudioAsync(tts, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPut("/api/media/resource/texttospeech/{id}")]
        public Task<InvokeResult<MediaResourceSummary>> UpdateTextToSpeechAsync(string id, [FromBody] TextToSpeechRequest tts)
        {
            return _mediaServicesManager.UpdateGeneratedAudioAsync(id, tts, OrgEntityHeader, UserEntityHeader);
        }


        [HttpPost("/api/media/resource/{id}/texttospeech")]
        public Task<InvokeResult<MediaResourceSummary>> AddTextToSpeechAsync([FromBody] TextToSpeechRequest tts, string id)
        {
            return _mediaServicesManager.UpdateAudioAsync(id, tts, OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("/api/media/resource/texttospeech/voices/{languageCode}")]
        public Task<InvokeResult<List<EntityHeader>>> GetVoicesForLanguageAsync(string languagecode)
        {
            return _mediaServicesManager.GetVoicesAsync(languagecode, OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("/api/media/resource/texttospeech/preview")]
        public async Task<IActionResult> PreviewAudioAsync(string text, string language, string voice, string gender)
        {
            var tts = new TextToSpeechRequest()
            {
                Text = text,
                Language = language,
                Gender = gender, 
                Voice = voice
            };

            var result = await _mediaServicesManager.PreviewAudioAsync(tts, OrgEntityHeader, UserEntityHeader);
            return File(result, "audio/mpeg", "preview.mp3");
        }

        /// <summary>
        /// Media Resources - Get resources for library.
        /// </summary>
        /// <returns></returns>
        [McpEndpoint("media-resources-list-for-library", Description = "List media resources in a media library.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/library/{libraryid}/resources")]
        public async Task<ListResponse<MediaResourceSummary>> GetResourceForLibraryAsync(string libraryid)
        {
            var library = await _libraryManager.GetMediaLibraryAsync(libraryid, OrgEntityHeader, UserEntityHeader);
            var result = await _mediaServicesManager.GetMediaResourceSummariesAsync(libraryid, OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
            result.Title = $"{result.Title} - {library.Name}";
            return result;
        }

        /// <summary>
        /// Media Resources - Get resources for librarys.
        /// </summary>
        /// <returns></returns>
        [McpEndpoint("media-resources-list-for-media-type", Description = "List media resources by media type key.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/mediatype/key/{mediatypekey}/resources")]
        public Task<ListResponse<MediaResourceSummary>> GetForMediaTypeKey(string mediatypekey)
        {
            return _mediaServicesManager.GetResourcesForMediaTypeKeyLibrary(mediatypekey, OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }

        [McpEndpoint("media-resources-list", Description = "List media resource summaries for the current organization.", FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/resources")]
        public Task<ListResponse<MediaResourceSummary>> GetAllMediaResources()
        {
            return _mediaServicesManager.GetMediaResourceSummariesAsync(GetListRequestFromHeader(), OrgEntityHeader, UserEntityHeader);
        }


        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [McpEndpoint("media-resource-image-upload", Description = "Upload media bytes to a resource; the returned populated record can be saved separately.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/resources/{id}")]
        public async Task<InvokeResult<MediaResource>> UploadMediaAsync(string id, IFormFile file, [FromForm] string name = null, [FromForm] string mediaLibraryId = null, bool saveresource = false)
        {
            if (file == null || file.Length == 0)
            {
                return InvokeResult<MediaResource>.FromError("A media file is required.");
            }

            using (var strm = file.OpenReadStream())
            {
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader, saveresource, resourceName: name, mediaLibraryId: mediaLibraryId);
            }
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [McpEndpoint("media-resource-image-revise", Description = "Upload a revision to an existing media resource; save returned complete record to persist revision metadata.", FamilyId = "media")]
        [Authorize]
        [HttpPut("/api/media/resources/{id}")]
        public async Task<InvokeResult<MediaResource>> AddMediaRevision(string id, IFormFile file)
        {
            using (var strm = file.OpenReadStream())
            {
                return await _mediaServicesManager.AddResourceMediaRevisionAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader);
            }
        }


        [HttpGet("/api/media/resource/{id}/revision/{revisionid}/set")]
        public async Task<InvokeResult<MediaResourceSummary>> SetRevision(string id, string revisionid)
        {
            var resource = await _mediaServicesManager.GetMediaResourceRecordAsync(id, OrgEntityHeader, UserEntityHeader);
            resource.CurrentRevision = revisionid;
            resource.LastUpdatedDate = UtcTimestamp.Now;
            resource.LastUpdatedBy = UserEntityHeader;
            return await _mediaServicesManager.UpdateMediaResourceRecordAsync(resource, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resource/public/upload")]
        public async Task<InvokeResult<MediaResource>> UploadPublicMediaAsync(IFormFile file)
        {
            using (var strm = file.OpenReadStream())
            {
                var id = Guid.NewGuid().ToId();
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, file.FileName, file.ContentType, OrgEntityHeader, UserEntityHeader, true, true);
            }
        }

        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPost("/api/media/resource/upload")]
        public async Task<InvokeResult<MediaResource>> UploadMediaAsync(IFormFile file)
        {
            var fileName = file.FileName;
            var contentType = file.ContentType;

            if (Request.Form.ContainsKey("filename"))
                fileName = Request.Form["filename"];

            if (Request.Form.ContainsKey("contenttype"))
                contentType = Request.Form["contenttype"];


            using (var strm = file.OpenReadStream())
            {
                var id = Guid.NewGuid().ToId();
                return await _mediaServicesManager.AddResourceMediaAsync(id, strm, fileName, contentType, OrgEntityHeader, UserEntityHeader, true, false);
            }
        }

        [HttpGet("/api/media/resource/{id}/clone")]
        public Task<InvokeResult<MediaResource>> Clone(string id, string entityTypeName, string entityFieldName, string resourceName)
        {
            return _mediaServicesManager.CloneMediaResourceAsync(id, entityTypeName, entityFieldName, resourceName, OrgEntityHeader, UserEntityHeader);
        }
        /// <summary>
        /// Media Resources - Upload a file for a specific media resource.
        /// </summary>
        /// <param name="id">unique id of the resource.</param>
        /// <param name="file">file to be uploaded.</param>
        /// <returns></returns>
        [HttpPut("/api/media/resource/{id}/resize")]
        public  Task<InvokeResult<MediaResource>> ResizeMediaAsync([FromBody] MediaResizeRequest request, string id)
        {
            return _mediaServicesManager.ResizeImageAsync(id, request.FileName, request.Width, request.Height, request.FileType, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Resource - Download a media resource file.
        /// </summary>
        /// <param name="orgid"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("/api/media/resource/{orgid}/{id}/download")]
        public async Task<IActionResult> DownloadMedia(string orgid, string id)
        {
            var lastMod = String.Empty;
            if (!String.IsNullOrEmpty(Request.Headers["If-Modified-Since"]))
            {
                CultureInfo provider = CultureInfo.InvariantCulture;
                lastMod = DateTime.ParseExact(Request.Headers["If-Modified-Since"], "r", provider, DateTimeStyles.AssumeUniversal).ToJSONString();
            }

            var response = await _mediaServicesManager.GetPublicResourceRecordAsync(orgid, id, lastMod);
            if (!String.IsNullOrEmpty(response.AiResponseId)) 
            {
                Response.Headers["x-ai-responseid"] = response.AiResponseId;
                Response.Headers["x-ai-generated"] = "true";
            }

            if (response.NotModified)
            {
                return StatusCode(304);
            }

            var ms = new MemoryStream(response.ImageBytes);
            var idx = 1;
            foreach(var timing in response.Timings)
            {
                Response.Headers.Append($"x-{idx++}-{timing.Key}", $"{timing.Ms}ms");
            }

            Response.Headers[HeaderNames.CacheControl] = "no-cache"; // lets us make a request to the server to check the last modified date, but will cache locally.
            Response.Headers[HeaderNames.LastModified] = new[] { response.LastModified.ToDateTime().ToString("R") }; // Format RFC1123

            return File(ms, response.ContentType, response.FileName);
        }


        /// <summary>
        /// Serve the current thumbnail of an explicitly public media resource.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/api/media/resource/{orgid}/{id}/thumbnail")]
        public async Task<IActionResult> DownloadPublicThumbnail(string orgid, string id)
        {
            var thumbnail = await _mediaServicesManager.GetPublicThumbnailAsync(orgid, id);
            return File(thumbnail.ImageBytes, thumbnail.ContentType);
        }

        [HttpPost("/api/sysadmin/media/resource/{id}/contents")]
        public async Task<InvokeResult> ImportMediaResourceContentsAsync(string id)
        {
            return await _mediaServicesManager.ImportMediaResourceContentsAsync(id, Request.Body, OrgEntityHeader, UserEntityHeader);
        }


        [HttpPost("/api/media/resource/{id}/revision/{revisionid}/external-read-lease")]
        public Task<InvokeResult<ImmutableMediaReadLease>> CreateImmutableExternalReadLease(
            string id,
            string revisionid,
            [FromBody] ImmutableMediaReadLeaseRequest request)
        {
            return _mediaServicesManager.CreateImmutableMediaReadLeaseAsync(
                id,
                revisionid,
                OrgEntityHeader,
                UserEntityHeader,
                request?.LifetimeMinutes);
        }

        [McpEndpoint("media-resource-image-download",
            Description = "FALLBACK ONLY: Download an authorized Media Resource revision as a model-side image result for explicit analysis/export, NOT the normal conversational image editing path. For editing, use media-gallery-open or media-gallery-preview(id,revisionid). The widget displays the actual original; the user right-clicks Copy image and pastes it into the ChatGPT composer. Edit THAT user-pasted image as your source. The widget's setWidgetState(modelContent) communicates selected resource/revision IDs, not image pixels. Never silently select a different revision or claim a preview is itself an editor source.",
            FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/resource/{id}/download/{revisionid}")]
        public async Task<IActionResult> DownloadMediaRevision(string id, string revisionid)
        {
            var response = await _mediaServicesManager.GetMediaRevisionAsync(id, revisionid, OrgEntityHeader, UserEntityHeader);
            var ms = new MemoryStream(response.ImageBytes);
            var idx = 1;
            foreach (var timing in response.Timings)
            {
                Response.Headers.Append($"x-{idx++}-{timing.Key}", $"{timing.Ms}ms");
            }

            return File(ms, response.ContentType, response.FileName);
        }

        /// <summary>
        /// Read-only ChatGPT native file-parameter transport probe.
        /// ChatGPT supplies the hosted reference; the server downloads and hashes the image.
        /// No Media Resource or persisted file is created.
        /// </summary>
        /// <summary>
        /// Render a selected immutable Media Resource revision in a MediaServices-owned
        /// MCP App. The browser receives a short-lived signed GET URL; image bytes do
        /// not travel through the tool result.
        /// </summary>
        /// <summary>
        /// Open a MediaServices-owned MCP Apps library browser. Only library names
        /// are returned to the model; the app requests fifteen signed previews per page.
        /// </summary>
        [McpEndpoint("media-gallery-open",
            Description = "PREFERRED INTERACTIVE LIBRARY BROWSER: Opens the APTIX Media Gallery (15 media items per page). The user selects an image to see its FULL-RESOLUTION ORIGINAL and six simple steps: right-click the original and Copy image, click ChatGPT message box and Paste (Ctrl+V/Cmd+V), request changes, review/iterate, then ask to save. Never ask user to copy the thumbnail. Widget publishes the selected mediaResourceId and revisionId as modelContent via setWidgetState WITHOUT uploading pixels or sending messages. For editing, use the original image the USER pasted into the chat composer as the image editor input; NOT the widget preview and NOT text regeneration. When user explicitly approves and asks to save, use media-revise-chatgpt-image on the selected mediaResourceId, verify exact ID and persisted revision. Widget state may not be available to every host; if missing, ask user for the Resource ID shown in the widget. For a specific historical revision use media-gallery-preview(id,revisionid).",
            FamilyId = "media", WidgetResourceUri = MediaGalleryBrowserWidgetResource.ResourceUri)]
        [Authorize]
        [HttpGet("/api/media/gallery/open")]
        public async Task<InvokeResult<MediaGalleryOpen>> OpenMediaGallery(string libraryid = null)
        {
            var list = await _libraryManager.GetMediaLibrariesForOrgsAsync(
                OrgEntityHeader.Id, ListRequest.Create(1, 100), UserEntityHeader);
            if (!list.Successful)
                return InvokeResult<MediaGalleryOpen>.FromInvokeResult(list.ToInvokeResult());

            var libraries = (list.Model ?? Enumerable.Empty<MediaLibrarySummary>())
                .Select(library => new MediaGalleryLibrary
                {
                    Id = library.Id,
                    Name = library.Name
                }).ToList();

            var initial = libraries.FirstOrDefault(library =>
                String.Equals(library.Id, libraryid, StringComparison.OrdinalIgnoreCase))
                ?? libraries.FirstOrDefault();
            return InvokeResult<MediaGalleryOpen>.Create(new MediaGalleryOpen
            {
                InitialLibraryId = initial?.Id,
                Libraries = libraries
            });
        }

        /// <summary>
        /// Authorize the library, then fetch one server-paged batch of fifteen media
        /// summaries and browser-ready signed thumbnail/original-image URLs.
        /// Never returns raw media contents or signed video/audio URLs.
        /// </summary>
        [McpEndpoint("media-gallery-page",
            Description = "WIDGET INTERNAL: Fetch one fifteen-item page for the APTIX Media Gallery browser (page starts at 1). Supplies media metadata/current revision and signed preview URLs, not original image bytes for ChatGPT editing. Invoke media-gallery-open to render the gallery. For exact historical revision handoff use media-gallery-preview(id,revisionid).",
            FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/gallery/library/{libraryid}/page/{page}")]
        public async Task<InvokeResult<MediaGalleryPage>> GetMediaGalleryPage(string libraryid, int page = 1)
        {
            var library = await _libraryManager.GetMediaLibraryAsync(
                libraryid, OrgEntityHeader, UserEntityHeader);
            var result = await _mediaServicesManager.GetMediaGalleryPageAsync(
                libraryid, page, OrgEntityHeader, UserEntityHeader);
            if (result.Successful)
                result.Result.LibraryName = library.Name;
            return result;
        }

        /// <summary>
        /// Data-only original-image lease used by a user's explicit "Work with this
        /// image" action. Does not render a nested widget or fetch any media bytes.
        /// Server authorization and exact immutable revision checks are unchanged.
        /// </summary>
        [McpEndpoint("media-gallery-image-lease",
            Description = "WIDGET INTERNAL: Authorize the exact immutable image revision and return a short-lived signed URL, used by the gallery widget to DISPLAY THE FULL-RESOLUTION ORIGINAL for manual browser Copy image. This does not upload pixels into ChatGPT or attach an image to model context. Users paste the copied image into the ChatGPT message composer. For known exact revisions invoke media-gallery-preview(id,revisionid), then follow the on-screen copy/paste instructions.",
            FamilyId = "media")]
        [Authorize]
        [HttpGet("/api/media/gallery/image-lease/{id}/{revisionid}")]
        public Task<InvokeResult<ImmutableMediaReadLease>> GetMediaGalleryImageLease(string id, string revisionid)
        {
            return _mediaServicesManager.CreateImmutableMediaReadLeaseAsync(
                id, revisionid, OrgEntityHeader, UserEntityHeader, lifetimeMinutes: 10);
        }

        [McpEndpoint("media-gallery-preview",
            Description = "PREFERRED EXACT-REVISION IMAGE PREVIEW: Provide existing Media Resource id and EXACT revisionid (historical revisions supported). This widget shows the original source image for browser Copy image; it does NOT attach image bytes to model or send a message. On the preview, user right-clicks and chooses Copy image (NOT Copy image address), clicks ChatGPT composer and pastes (Ctrl+V/Cmd+V), requests changes, reviews and iterates, and asks to save after approving. The widget publishes mediaResourceId and revisionId to modelContent via setWidgetState so ChatGPT can identify the save target; IDs are also printed for manual reference if host state is not available. Edit the image pasted by the user, not the text description alone or the widget's visual preview. Never substitute current revision for requested historical one. Save approved result via media-revise-chatgpt-image on that resource ID and verify the new persisted revision. Do not save before explicit user instruction.",
            FamilyId = "media", WidgetResourceUri = MediaGalleryWidgetResource.ResourceUri)]
        [Authorize]
        [HttpGet("/api/media/gallery/preview/{id}/{revisionid}")]
        public Task<InvokeResult<ImmutableMediaReadLease>> RenderMediaGalleryPreview(string id, string revisionid)
        {
            return _mediaServicesManager.CreateImmutableMediaReadLeaseAsync(
                id, revisionid, OrgEntityHeader, UserEntityHeader, lifetimeMinutes: 10);
        }

        [McpEndpoint("media-import-chatgpt-file",
            Description = "Test ChatGPT native file transfer: supply a generated or attached image as the file argument. The server reads the temporary hosted file and returns byte count, SHA-256 and detected image type. No Media Resource is created.",
            FamilyId = "media", FileParams = new[] { "file" })]
        [Authorize]
        [HttpPost("/api/media/chatgpt/file-probe")]
        public Task<InvokeResult<ChatGptFileProbeResult>> ProbeChatGptFile([FromBody] ChatGptFileReference file)
        {
            return ChatGptFileProbe.ProbeAsync(file);
        }

        /// <summary>
        /// Import a ChatGPT-hosted PNG, JPEG or WebP directly into a new Media Resource.
        /// ChatGPT supplies the short-lived file reference through native MCP file parameters.
        /// </summary>
        [McpEndpoint("media-import-chatgpt-image",
            Description = "CREATE NEW RESOURCE ONLY: Import a ChatGPT-generated/attached image as a separate Media Resource. Do NOT use to save edits to a selected existing resource. For approved changes to an existing selected mediaResourceId use media-revise-chatgpt-image instead, preserving original revision history. Accepts native ChatGPT file plus optional name and Media Library ID.",
            FamilyId = "media", FileParams = new[] { "file" })]
        [Authorize]
        [HttpPost("/api/media/chatgpt/image-import")]
        public async Task<InvokeResult<MediaResource>> ImportChatGptImage(
            [FromBody] ChatGptFileReference file, string name = null, string mediaLibraryId = null, bool isPublic = false)
        {
            var downloaded = await ChatGptFileProbe.DownloadAsync(file);
            if (!downloaded.Successful)
                return InvokeResult<MediaResource>.FromInvokeResult(downloaded.ToInvokeResult());

            var image = downloaded.Result;
            using (var stream = new MemoryStream(image.Bytes, writable: false))
            {
                // Keep MediaServices' existing authorization, thumbnail, revision,
                // SHA-256 and persistence path as the single source of truth.
                return await _mediaServicesManager.AddResourceMediaAsync(
                    Guid.NewGuid().ToId(), stream, image.FileName, image.ContentType,
                    OrgEntityHeader, UserEntityHeader, saveResourceRecord: true,
                    isPublic: isPublic, resourceName: name, mediaLibraryId: mediaLibraryId);
            }
        }

        /// <summary>
        /// Save a ChatGPT-generated/edited image as a new revision on an existing
        /// Media Resource. Uses the proven native ChatGPT file transfer and the
        /// existing MediaServices revision/thumbnail persistence path.
        /// No new Media Resource is created; existing revision blobs remain intact.
        /// </summary>
        [McpEndpoint("media-revise-chatgpt-image",
            Description = "AFTER EXPLICIT USER APPROVAL ONLY: Save the final ChatGPT-edited PNG/JPEG/WebP as a NEW revision of an existing Media Resource. Use the original mediaResourceId from selected gallery/preview widget modelContent or from user-supplied resource ID, and the ChatGPT-generated/edited IMAGE FILE as file. Optional expectedRevisionId prevents overwriting a changed current revision; DO NOT assume the edited SOURCE revision is still current. This preserves existing revisions and updates current revision. After saving, fetch media_resources_get to verify the exact resource ID, the new revision, and prior revision preservation. Never create a different resource when the user requested an update.",
            FamilyId = "media", FileParams = new[] { "file" })]
        [Authorize]
        [HttpPost("/api/media/chatgpt/image-revise/{id}")]
        public async Task<InvokeResult<MediaResource>> ReviseChatGptImage(
            string id,
            [FromBody] ChatGptFileReference file,
            string expectedRevisionId = null,
            string editInstruction = null)
        {
            if (String.IsNullOrWhiteSpace(id))
                return InvokeResult<MediaResource>.FromError("An existing Media Resource ID is required.");

            var existing = await _mediaServicesManager.GetMediaResourceRecordAsync(
                id, OrgEntityHeader, UserEntityHeader);
            if (existing == null)
                return InvokeResult<MediaResource>.FromError("Could not find the specified Media Resource.");

            if (!String.IsNullOrWhiteSpace(expectedRevisionId) &&
                !String.Equals(existing.CurrentRevision, expectedRevisionId, StringComparison.OrdinalIgnoreCase))
                return InvokeResult<MediaResource>.FromError(
                    "The Media Resource current revision has changed since the image was selected.");

            if (String.IsNullOrWhiteSpace(existing.MimeType) ||
                !existing.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return InvokeResult<MediaResource>.FromError(
                    "This revision importer only supports existing image Media Resources.");

            var downloaded = await ChatGptFileProbe.DownloadAsync(file);
            if (!downloaded.Successful)
                return InvokeResult<MediaResource>.FromInvokeResult(downloaded.ToInvokeResult());

            var image = downloaded.Result;
            using (var stream = new MemoryStream(image.Bytes, writable: false))
            {
                // saveResourceRecord=true persists the new immutable revision;
                // AddResourceMediaRevisionAsync also generates the thumbnail,
                // dimensions, and SHA-256 while retaining earlier History entries.
                return await _mediaServicesManager.AddResourceMediaRevisionAsync(
                    id, stream, image.FileName, image.ContentType,
                    OrgEntityHeader, UserEntityHeader,
                    saveResourceRecord: true,
                    revisedPrompt: editInstruction ?? String.Empty);
            }
        }

        [McpEndpoint("media-image-assembly-create", Description = "Create a temporary blank PNG canvas in cloud storage, returning its assembly ID.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/image-assembly/create")]
        public Task<InvokeResult<ImageAssemblyStatus>> CreateImageAssembly(int width, int height)
        {
            return _imageAssemblyManager.CreateAsync(width, height, OrgEntityHeader);
        }

        [McpEndpoint("media-image-assembly-append", Description = "Paste a base64-encoded PNG image tile at x,y on the temporary cloud-stored canvas. Maximum tile is 512 pixels per side.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/image-assembly/{id}/append")]
        public Task<InvokeResult<ImageAssemblyStatus>> AppendImageAssembly(string id, int x, int y, string imageData)
        {
            return _imageAssemblyManager.AppendAsync(id, x, y, imageData, OrgEntityHeader);
        }

        [McpEndpoint("media-image-assembly-publish", Description = "Publish a fully covered temporary canvas as a saved Media Resource with an automatic thumbnail.", FamilyId = "media")]
        [Authorize]
        [HttpPost("/api/media/image-assembly/{id}/publish")]
        public Task<InvokeResult<MediaResource>> PublishImageAssembly(string id, string name, string mediaLibraryId = null, bool isPublic = false)
        {
            return _imageAssemblyManager.PublishAsync(id, name, mediaLibraryId, isPublic, OrgEntityHeader, UserEntityHeader);
        }

        [HttpPost("/api/media/resource/request")]
        public async Task<InvokeResult<MediaResource>> UploadAsync([FromBody] MediaUploadRequest uploadRequest )
        {
            return await _mediaServicesManager.AddResourceMediaAsync(new Uri(uploadRequest.Uri), uploadRequest.FileName, OrgEntityHeader, UserEntityHeader, true, uploadRequest.IsPublic, uploadRequest.License);
        }
    }
}
