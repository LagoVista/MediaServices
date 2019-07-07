    using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;

namespace LagoVista.MediaServices.Models
{
    public enum MediaResourceTypes
    {
        [EnumLabel(MediaResource.DeviceResourceTypes_Manual, MediaServicesResources.Names.MediaResourceType_Manual, typeof(Resources.MediaServicesResources))]
        Manual,
        [EnumLabel(MediaResource.DeviceResourceTypes_UserGuide, MediaServicesResources.Names.MediaResourceType_UserGuide, typeof(Resources.MediaServicesResources))]
        UserGuide,
        [EnumLabel(MediaResource.DeviceResourceTypes_Specification, MediaServicesResources.Names.MediaResourceType_Specification, typeof(Resources.MediaServicesResources))]
        Specification,
        [EnumLabel(MediaResource.DeviceResourceTypes_PartsList, MediaServicesResources.Names.MediaResourceType_PartsList, typeof(Resources.MediaServicesResources))]
        PartList,
        [EnumLabel(MediaResource.DeviceResourceTypes_Picture, MediaServicesResources.Names.MediaResourceType_Picture, typeof(Resources.MediaServicesResources))]
        Picture,
        [EnumLabel(MediaResource.DeviceResourceTypes_Video, MediaServicesResources.Names.MediaResourceType_Video, typeof(Resources.MediaServicesResources))]
        Video,
        [EnumLabel(MediaResource.DeviceResourceTypes_Other, MediaServicesResources.Names.MediaResourceType_Other, typeof(Resources.MediaServicesResources))]
        Other,
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaResource_Title, MediaServicesResources.Names.MediaResource_Help, MediaServicesResources.Names.MediaResource_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources))]
    public class MediaResource : IIDEntity, INoSQLEntity, IValidateable, IOwnedEntity, IAuditableEntity
    {

        public const string DeviceResourceTypes_Manual = "manual";
        public const string DeviceResourceTypes_UserGuide = "userguide";
        public const string DeviceResourceTypes_Specification = "specification";
        public const string DeviceResourceTypes_PartsList = "partslist";
        public const string DeviceResourceTypes_Picture = "picture";
        public const string DeviceResourceTypes_Video = "video";
        public const string DeviceResourceTypes_Other = "other";

        public MediaResource()
        {
            IsFileUpload = true;
        }

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

        public string Id { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_FileName, FieldType: FieldTypes.Text, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public string FileName { get; set; }
        [FormField(LabelResource: Resources.MediaServicesResources.Names.Common_Key, HelpResource: Resources.MediaServicesResources.Names.Common_Key_Help, FieldType: FieldTypes.Key, RegExValidationMessageResource: Resources.MediaServicesResources.Names.Common_Key_Validation, ResourceType: typeof(MediaServicesResources), IsRequired: true)]
        public string Key { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_IsFileUpload, HelpResource: Resources.MediaServicesResources.Names.MediaResource_IsFileUpload_Help, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources))]
        public bool IsFileUpload { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Link, HelpResource: MediaServicesResources.Names.MediaResource_Link_Help, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string Link { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_ContentLength, FieldType: FieldTypes.Integer, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public long? ContentSize { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_MimeType, IsUserEditable: false, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string MimeType { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.Common_Name, IsRequired: true, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string Name { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public string Description { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_MediaLibrary, FieldType: FieldTypes.Text, IsRequired: true, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader MediaLibrary { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_ResourceType, WaterMark: MediaServicesResources.Names.MediaResources_ResourceType_Select, HelpResource: Resources.MediaServicesResources.Names.MediaResource_ResourceType_Help, IsRequired: true, EnumType: typeof(MediaResourceTypes), FieldType: FieldTypes.Picker, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<MediaResourceTypes> ResourceType { get; set; }
        public string DatabaseName { get; set; }
        public string EntityType { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_StorageRefName, WaterMark: MediaServicesResources.Names.MediaResources_ResourceType_Select, EnumType: typeof(MediaResourceTypes), FieldType: FieldTypes.Picker, ResourceType: typeof(MediaServicesResources))]
        public string StorageReferenceName { get; set; }

        public void SetContentType(string contentType)
        {
            StorageReferenceName = $"{Id}.media";
            MimeType = "application/octet-stream";

            if (contentType.ToLower().Contains("gif"))
            {
                StorageReferenceName = $"{Id}.gif";
                MimeType = "image/gif";
            }
            else if (contentType.ToLower().Contains("png"))
            {
                StorageReferenceName = $"{Id}.png";
                MimeType = "image/png";
            }
            else if (contentType.ToLower().Contains("jpg"))
            {
                StorageReferenceName = $"{Id}.jpg";
                MimeType = "image/jpeg";
            }
            else if (contentType.ToLower().Contains("jpeg"))
            {
                StorageReferenceName = $"{Id}.jpeg";
                MimeType = "image/jpeg";
            }
            else if (contentType.ToLower().Contains("pdf"))
            {
                StorageReferenceName = $"{Id}.pdf";
                MimeType = "application/pdf";
            }
            else if (contentType.ToLower().Contains("csv"))
            {
                StorageReferenceName = $"{Id}.csv";
                MimeType = "text/plain";
            }
        }

        public MediaResourceSummary CreateSummary()
        {
            return new MediaResourceSummary()
            {
                Id = Id,
                Description = Description,
                IsPublic = IsPublic,
                Key = Key,
                Name = Name,
                ResourceType = ResourceType.Text,
                MimeType = MimeType,
                ContentSize = ContentSize,
            };
        }

        [CustomValidator]
        public void Validate(ValidationResult result)
        {
            if (IsFileUpload)
            {
                Link = String.Empty;

                if (String.IsNullOrEmpty(FileName))
                {
                    result.AddUserError("Must provide file.");
                }
                else
                {
                    if (String.IsNullOrEmpty(MimeType))
                    {
                        result.AddUserError("Mime Type is a Required FIeld..");
                    }

                    if (String.IsNullOrEmpty(StorageReferenceName))
                    {
                        result.AddUserError("Mime Type is a Required FIeld..");
                    }
                }
            }
            else
            {
                ContentSize = null;
                MimeType = null;
                FileName = null;
                StorageReferenceName = null;

                if (String.IsNullOrEmpty(Link))
                {
                    result.AddUserError("Must provide link.");
                }
            }
        }
    }

    public class MediaResourceSummary : SummaryData
    {
        public string ResourceType { get; set; }
        public string MimeType { get; set; }
        public long? ContentSize { get; set; }
    }
}
