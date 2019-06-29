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
    }
}
