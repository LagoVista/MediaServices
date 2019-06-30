using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using Newtonsoft.Json;
using System;

namespace LagoVista.MediaServices.Models
{
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaLibrary_Title, MediaServicesResources.Names.MediaLibrary_Help, MediaServicesResources.Names.MediaLibrary_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources))]
    public class MediaLibrary : INoSQLEntity, IKeyedEntity, INamedEntity, IAuditableEntity, IIDEntity, IOwnedEntity, IValidateable
    {
        [JsonProperty("id")]
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_UniqueId, IsUserEditable: false, ResourceType: typeof(MediaServicesResources), IsRequired: true)]
        public String Id { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Key, HelpResource: Resources.MediaServicesResources.Names.Common_Key_Help, FieldType: FieldTypes.Key, RegExValidationMessageResource: Resources.MediaServicesResources.Names.Common_Key_Validation, ResourceType: typeof(MediaServicesResources), IsRequired: true)]
        public String Key { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_IsPublic, FieldType: FieldTypes.Bool, ResourceType: typeof(MediaServicesResources))]
        public bool IsPublic { get; set; }
        public EntityHeader OwnerOrganization { get; set; }
        public EntityHeader OwnerUser { get; set; }
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_CreationDate, FieldType: FieldTypes.JsonDateTime, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: false)]
        public String CreationDate { get; set; }

        public EntityHeader CreatedBy { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_LastUpdated, FieldType: FieldTypes.JsonDateTime, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: false)]
        public String LastUpdatedDate { get; set; }

        public EntityHeader LastUpdatedBy { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Name, ResourceType: typeof(MediaServicesResources), IsRequired: true, IsUserEditable: true)]
        public String Name { get; set; }

        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public String Description { get; set; }

        public string DatabaseName { get; set; }
        public string EntityType { get; set; }

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
