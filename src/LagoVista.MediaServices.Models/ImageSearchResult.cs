using Newtonsoft.Json;
using RingCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.MediaServices.Models
{
    public  class ImageSearchResult
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string ImageUrl { get; set; }

        [JsonProperty("filetype")]
        public string FileType { get; set; }

        [JsonProperty("filesize")]
        public string FileSize { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }


        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("Creator")]
        public string Creator { get; set; }

        [JsonProperty("CreatorUrl")]
        public string CreatorUrl { get; set; }

        [JsonProperty("license_version")]
        public string LicenseVersion { get; set; }

        [JsonProperty("license_url")]
        public string LicenseUrl { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }
    }

    public class ImageSearchResults
    {
        [JsonProperty("result_count")]
        public int ResultCount { get; set; }

        [JsonProperty("page_count")]
        public int PageCount { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }


        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        [JsonProperty("results")]
        public List<ImageSearchResult> Results { get; set; }

    }
}
