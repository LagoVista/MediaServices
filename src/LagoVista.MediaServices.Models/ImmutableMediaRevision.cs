using System;

namespace LagoVista.MediaServices.Models
{
    public class ImmutableMediaRevision
    {
        public string MediaResourceId { get; set; }
        public string RevisionId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long? ContentSize { get; set; }
        public string ContentSha256 { get; set; }
        public byte[] ContentBytes { get; set; }
        public bool ContentSizeVerified { get; set; }
        public bool ContentSha256Verified { get; set; }
    }
}
