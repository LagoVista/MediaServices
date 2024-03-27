using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class MediaUploadRequest
    {
        public string Uri { get; set; }
        public string FileName { get; set; }
        public bool IsPublic { get; set; }
    }
}
