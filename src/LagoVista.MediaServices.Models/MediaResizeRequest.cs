// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 0a7f16b971320bf2c6a07ce3b68d9c259289ca573159aa5c681e4c7f6a44f23b
// IndexVersion: 2
// --- END CODE INDEX META ---
using RingCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class MediaResizeRequest
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
