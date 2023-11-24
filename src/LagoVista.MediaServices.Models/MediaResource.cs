    using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
    public class MediaResource : EntityBase, IValidateable, IDescriptionEntity, IFormDescriptor, IFormConditionalFields
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

        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_FileName, FieldType: FieldTypes.MediaResourceUpload, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public string FileName { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_IsFileUpload, HelpResource: Resources.MediaServicesResources.Names.MediaResource_IsFileUpload_Help, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources))]
        public bool IsFileUpload { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Link, HelpResource: MediaServicesResources.Names.MediaResource_Link_Help, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string Link { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_ContentLength, FieldType: FieldTypes.Integer, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public long? ContentSize { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_MimeType, IsUserEditable: false, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string MimeType { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public string Description { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_MediaLibrary, FieldType: FieldTypes.Text, IsRequired: false, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader MediaLibrary { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_ResourceType, WaterMark: MediaServicesResources.Names.MediaResources_ResourceType_Select, HelpResource: Resources.MediaServicesResources.Names.MediaResource_ResourceType_Help, IsRequired: true, EnumType: typeof(MediaResourceTypes), FieldType: FieldTypes.Picker, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<MediaResourceTypes> ResourceType { get; set; }
      
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
                Link = Link,
                IsFileUpload = IsFileUpload,
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
                    result.AddUserError("Must provide file name.");
                }
                else
                {
                    if (String.IsNullOrEmpty(MimeType))
                    {
                        result.AddUserError("Mime Type is a Required Field.");
                    }

                    if (String.IsNullOrEmpty(StorageReferenceName))
                    {
                        result.AddUserError("Storage Reference Name is a Required Field.");
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

        public List<string> GetFormFields()
        {
            return new List<string>()
            {
                nameof(Name),
                nameof(Key),
                nameof(IsFileUpload),
                nameof(FileName),
                nameof(ResourceType),
                nameof(Link),
                nameof(ContentSize),
                nameof(MimeType),
                nameof(Description),
            };
        }

        public FormConditionals GetConditionalFields()
        {
            return new FormConditionals()
            {
                ConditionalFields = new List<string> { nameof(ContentSize), nameof(MimeType), nameof(Link) },
                Conditionals = new List<FormConditional>()
                {
                    new FormConditional()
                    {
                        Field = nameof(IsFileUpload),
                        Value = "true",
                        VisibleFields = new List<string>() {nameof(ContentSize), nameof(MimeType)}
                    },
                    new FormConditional()
                    {
                        Field = nameof(IsFileUpload),
                        Value = "false",
                        VisibleFields = new List<string>() {nameof(Link)}
                    }
                }
            };
        }
    }

    public class MediaResourceSummary : SummaryData
    {
        public string ResourceType { get; set; }
        public string MimeType { get; set; }
        public long? ContentSize { get; set; }
        public bool IsFileUpload { get; set; }
        public string Link { get; set; }
    }
}
