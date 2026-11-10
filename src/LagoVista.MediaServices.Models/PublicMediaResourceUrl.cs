namespace LagoVista.MediaServices.Models
{
    /// <summary>
    /// Stable public delivery URLs for a Media Resource's current revision.
    /// Not a time-limited signed immutable-revision lease.
    /// </summary>
    public class PublicMediaResourceUrl
    {
        public string MediaResourceId { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string CurrentRevisionId { get; set; }
        public bool IsPublic { get; set; }
    }
}
