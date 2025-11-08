// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 84c515c81cf142834e65321390037a94ef38e528b466ee26db8e351df69f744e
// IndexVersion: 2
// --- END CODE INDEX META ---
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
