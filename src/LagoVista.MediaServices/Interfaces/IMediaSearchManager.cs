using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Models;
using LagoVista.MediaServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices
{
    public interface IMediaSearchManager
    {
        Task<ListResponse<ImageSearchResult>> SearchImagesAsync(string term, string source, ListRequest listRequest, EntityHeader org, EntityHeader user);
    }
}
