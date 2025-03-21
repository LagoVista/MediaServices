﻿using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using LagoVista.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LagoVista.Core.Models;

namespace LagoVista.MediaServices.Rest.Controllers
{
    /// <summary>
    /// The Media Library Controller will allow users to manage a collection of media items.
    /// </summary>
    public class MediaLibraryController : LagoVistaBaseController
    {
        IMediaLibraryManager _mediaLibraryManager;

        /// <summary>
        /// The Media library controller will let users upload images and download them.
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        /// <param name="mediaLibraryManager"></param>
        public MediaLibraryController(UserManager<AppUser> userManager, IAdminLogger logger, IMediaLibraryManager mediaLibraryManager) : base(userManager, logger)
        {
            _mediaLibraryManager = mediaLibraryManager;
        }
        /// <summary>
        /// Medial Library - Add
        /// </summary>
        /// <param name="medialibrary"></param>
        [HttpPost("/api/media/library")]
        public Task<InvokeResult> AddMediaLibraryAsync([FromBody] MediaLibrary medialibrary)
        {
            return _mediaLibraryManager.AddMediaLibraryAsync(medialibrary, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Media Library - Update
        /// </summary>
        /// <param name="medialibrary"></param>
        /// <returns></returns>
        [HttpPut("/api/media/library")]
        public Task<InvokeResult> UpdateMediaLibraryAsync([FromBody] MediaLibrary medialibrary)
        {
            SetUpdatedProperties(medialibrary);
            return _mediaLibraryManager.UpdateMediaLibraryAsync(medialibrary, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Medial Library - Get for Current Org
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/libraries")]
        public Task<ListResponse<MediaLibrarySummary>> GetMediaLibrarysForOrg()
        {
            return _mediaLibraryManager.GetMediaLibrariesForOrgsAsync(OrgEntityHeader.Id, GetListRequestFromHeader(), UserEntityHeader);
        }

        /// <summary>
        /// Medial Library - Get for Current Org
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/customer/{customerid}/media/libraries")]
        public Task<ListResponse<MediaLibrarySummary>> GetMediaLibrarysForOrg(string customerid)
        {
            return _mediaLibraryManager.GetMediaLibrariesForCustomerAsync(OrgEntityHeader.Id, customerid, GetListRequestFromHeader(), UserEntityHeader);
        }

        /// <summary>
        /// Medial Library - In Use
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("/api/media/library/{id}/inuse")]
        public Task<DependentObjectCheckResult> InUseCheck(String id)
        {
            return _mediaLibraryManager.CheckMediaLibraryInUseAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        /// Medial Library - Get
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("/api/media/library/{id}")]
        public async Task<DetailResponse<MediaLibrary>> GetMediaLibrary(String id)
        {
            var MediaLibrary = await _mediaLibraryManager.GetMediaLibraryAsync(id, OrgEntityHeader, UserEntityHeader);

            return DetailResponse<MediaLibrary>.Create(MediaLibrary);
        }

        /// <summary>
        /// Medial Library - Key In Use
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/library/{key}/keyinuse")]
        public Task<bool> GetMediaLibraryKeyInUseAsync(String key)
        {
            return _mediaLibraryManager.QueryMediaLibraryKeyInUseAsync(key, CurrentOrgId);
        }

        /// <summary>
        /// Medial Library - Delete
        /// </summary>
        /// <returns></returns>
        [HttpDelete("/api/media/library/{id}")]
        public Task<InvokeResult> DeleteMediaLibraryAsync(string id)
        {
            return _mediaLibraryManager.DeleteMediaLibraryAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        /// <summary>
        ///  Medial Library - Create New
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/media/library/factory")]
        public DetailResponse<MediaLibrary> CreateMediaLibrary()
        {
            var response = DetailResponse<MediaLibrary>.Create();
            SetAuditProperties(response.Model);
            SetOwnedProperties(response.Model);
            response.FormFields.Remove(nameof(MediaLibrary.MediaResources).CamelCase());
            return response;
        }
    }
}
