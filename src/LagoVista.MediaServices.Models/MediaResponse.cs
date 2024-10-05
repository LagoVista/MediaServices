using LagoVista.Core.Validation;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class MediaItemResponse
    {
        public byte[] ImageBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string ImageTimestamp { get; set; }
        public bool NotModified { get; set; } = false;
        public string LastModified { get; set; }
        public List<ResultTiming> Timings { get; set; } = new List<ResultTiming>();
    }
}
