using System;

namespace LagoVista.MediaServices.Models
{
    public class ImmutableExternalMediaReadLease
    {
        public string Url { get; set; }
        public DateTime ValidUntilUtc { get; set; }
        public string MediaResourceId { get; set; }
        public string RevisionId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long? ContentLength { get; set; }
        public string ContentSha256 { get; set; }
        public bool RevocationSupported { get; set; }
    }
}
