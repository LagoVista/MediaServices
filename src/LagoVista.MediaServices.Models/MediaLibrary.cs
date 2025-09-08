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
        EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources), Icon: "icon-pz-podcast",
        EditUIUrl: "/contentmanagement/medialibrary/{id}", ListUIUrl: "/contentmanagement/medialibraries", CreateUIUrl: "/contentmanagement/medialibrary/add",
        FactoryUrl: "/api/media/library/factory", GetListUrl: "/api/media/libraries", GetUrl: "/api/media/library/{id}", SaveUrl: "/api/media/library", DeleteUrl: "/api/media/library/{id}")]
    public class MediaLibrary : EntityBase, IDescriptionEntity, IValidateable, IFormDescriptor, ISummaryFactory, IIconEntity
    {
        
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public String Description { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.MediaLibrary_MediaResources, FieldType: FieldTypes.Action, ResourceType: typeof(MediaServicesResources))]
        public string MediaResources { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.Common_Icon, FieldType: FieldTypes.Icon, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public string Icon { get; set; } = "icon-pz-podcast";

        public EntityHeader Customer { get; set; }

        public MediaLibrarySummary CreateSummary()
        {
            var summary = new MediaLibrarySummary();
            summary.Populate(this);
            return summary;
        }

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(Icon),
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
        EntityDescriptionAttribute.EntityTypes.Summary, ResourceType: typeof(MediaServicesResources), Icon: "icon-pz-podcast",
        FactoryUrl: "/api/media/library/factory", GetListUrl: "/api/media/libraries", GetUrl: "/api/media/library/{id}", SaveUrl: "/api/media/library", DeleteUrl: "/api/media/library/{id}")]
    public class MediaLibrarySummary : SummaryData
    {

    }
}
