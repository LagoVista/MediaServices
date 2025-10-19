// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 5b5a54062b6e7414f85b834cc9ee517a347fab9344ad35a00f67c8a5124ea9c5
// IndexVersion: 0
// --- END CODE INDEX META ---
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class MediaUploadRequest
    {
        public string License { get; set; }
        public string Uri { get; set; }
        public string FileName { get; set; }
        public bool IsPublic { get; set; }
    }
}
