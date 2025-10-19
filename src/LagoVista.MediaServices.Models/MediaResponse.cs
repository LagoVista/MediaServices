// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 69f3a2f065b620c8be3535d9b60deaedaeae371d7bb8b6ff5b922a2e8487d234
// IndexVersion: 0
// --- END CODE INDEX META ---
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
        public string AiGenerated { get; set; }
        public string AiResponseId { get; set; }
        public List<ResultTiming> Timings { get; set; } = new List<ResultTiming>();
    }
}
