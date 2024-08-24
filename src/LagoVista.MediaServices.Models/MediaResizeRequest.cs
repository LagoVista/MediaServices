using RingCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public class MediaResizeRequest
    {
        public string FileType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
