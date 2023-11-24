using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaLibrary_Title, MediaServicesResources.Names.MediaLibrary_Help, MediaServicesResources.Names.MediaLibrary_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources))]
    public class MediaLibrary : EntityBase, IDescriptionEntity, IValidateable
    {
        
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public String Description { get; set; }


        public MediaLibrarySummary CreateSummary()
        {
            return new MediaLibrarySummary()
            {
                Description = Description,
                Id = Id,
                IsPublic = IsPublic,
                Key = Key,
                Name = Name
            };
        }
    }

    public class MediaLibrarySummary : SummaryData
    {

    }
}
