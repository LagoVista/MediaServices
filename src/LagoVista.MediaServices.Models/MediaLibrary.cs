using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaLibrary_Title, MediaServicesResources.Names.MediaLibrary_Help, MediaServicesResources.Names.MediaLibrary_Description, 
        EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources),
        FactoryUrl: "/api/media/library/factory", GetListUrl: "/api/media/libraries", GetUrl: "/api/media/library/{id}", SaveUrl: "/api/media/library", DeleteUrl: "/api/media/library/{id}")]
    public class MediaLibrary : EntityBase, IDescriptionEntity, IValidateable, IFormDescriptor, ISummaryFactory
    {
        
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public String Description { get; set; }


        [FormField(LabelResource: Resources.MediaServicesResources.Names.MediaLibrary_MediaResources, FieldType: FieldTypes.Action, ResourceType: typeof(MediaServicesResources))]
        public string MediaResources { get; set; }


        public EntityHeader Customer { get; set; }

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

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(Description),
                nameof(MediaResources)
            };
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }
    }


    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaLibraries_Title, MediaServicesResources.Names.MediaLibrary_Help, MediaServicesResources.Names.MediaLibrary_Description,
        EntityDescriptionAttribute.EntityTypes.Summary, ResourceType: typeof(MediaServicesResources),
        FactoryUrl: "/api/media/library/factory", GetListUrl: "/api/media/libraries", GetUrl: "/api/media/library/{id}", SaveUrl: "/api/media/library", DeleteUrl: "/api/media/library/{id}")]
    public class MediaLibrarySummary : SummaryData
    {

    }
}
