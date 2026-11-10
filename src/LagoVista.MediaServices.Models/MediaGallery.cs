using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    /// <summary>
    /// Lightweight browser data for an MCP App. A gallery page never contains media bytes.
    /// </summary>
    public class MediaGalleryOpen
    {
        public string InitialLibraryId { get; set; }
        public List<MediaGalleryLibrary> Libraries { get; set; } = new List<MediaGalleryLibrary>();
    }

    public class MediaGalleryLibrary
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class MediaGalleryPage
    {
        public string LibraryId { get; set; }
        public string LibraryName { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = 15;
        public bool HasMore { get; set; }
        public List<MediaGalleryItem> Items { get; set; } = new List<MediaGalleryItem>();
    }

    public class MediaGalleryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ResourceType { get; set; }
        public string MimeType { get; set; }
        public string Kind { get; set; }
        public string RevisionId { get; set; }
        public string FileName { get; set; }
        public long? ContentSize { get; set; }
        public string PreviewUrl { get; set; }
        public bool UsesOriginalAsPreview { get; set; }

        public static string DetectKind(string mimeType, string resourceType, bool isFileUpload, string externalUrl)
        {
            // MIME describes the actual content. Generated video and audio may
            // have IsFileUpload=false plus a URL; that does NOT make them links.
            var mime = (mimeType ?? String.Empty).Trim().ToLowerInvariant();
            var category = (resourceType ?? String.Empty).Trim().ToLowerInvariant();
            if (mime.StartsWith("image/")) return "image";
            if (mime.StartsWith("video/")) return "video";
            if (mime.StartsWith("audio/")) return "audio";
            if (mime.Contains("zip") || mime.Contains("compressed")) return "archive";
            if (mime == "application/pdf" || mime.StartsWith("text/") ||
                mime.Contains("document") || mime.Contains("word")) return "document";
            if (!isFileUpload && !String.IsNullOrWhiteSpace(externalUrl))
                return "link";
            if (category == "picture") return "image";
            if (category == "video" || category == "rawvideo") return "video";
            if (category == "audio") return "audio";
            if (category == "zip") return "archive";
            if (category == "manual" || category == "userguide" ||
                category == "specification") return "document";
            return "file";
        }

        /// <summary>
        /// Old resources without a generated thumbnail can use a small original
        /// directly from storage; larger originals never enter the gallery grid.
        /// </summary>
        public static bool AllowsOriginalFallback(string mimeType, long? contentSize, bool isFileUpload)
        {
            return isFileUpload && IsBrowserImageMimeType(mimeType) &&
                   contentSize.HasValue && contentSize.Value > 0 &&
                   contentSize.Value <= 4L * 1024 * 1024;
        }

        public static bool IsBrowserImageMimeType(string mimeType)
        {
            var mime = (mimeType ?? String.Empty).Trim().ToLowerInvariant();
            return mime == "image/png" || mime == "image/jpeg" ||
                   mime == "image/webp" || mime == "image/gif";
        }
    }
}
