using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Models.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Mime;

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
        [EnumLabel(MediaResource.DeviceResourceTypes_Content, MediaServicesResources.Names.MediaResourceType_Content, typeof(Resources.MediaServicesResources))]
        Content,
        [EnumLabel(MediaResource.DeviceResourceTypes_CompressedFile, MediaServicesResources.Names.MediaResourceType_CompressedFile, typeof(Resources.MediaServicesResources))]
        CompressedFile,
        [EnumLabel(MediaResource.DeviceResourceTypes_Audio, MediaServicesResources.Names.DeviceResourceTypes_Audio, typeof(Resources.MediaServicesResources))]
        Audio,
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaResource_Title, MediaServicesResources.Names.MediaResource_Help, 
        MediaServicesResources.Names.MediaResource_Description, EntityDescriptionAttribute.EntityTypes.SimpleModel, ResourceType: typeof(MediaServicesResources),
        FactoryUrl: "/api/media/resource/factory", GetListUrl: "/api/media/library/{libraryid}/resources", GetUrl: "/api/media/resource/{id}", DeleteUrl: "/api/media/resource/{id}",
        SaveUrl: "/api/media/resource", Icon: "icon-fo-image")]
    public class MediaResource : EntityBase, IValidateable, IDescriptionEntity, IFormDescriptor, IFormConditionalFields, ISummaryFactory, IFormDescriptorCol2
    {

        public const string DeviceResourceTypes_Manual = "manual";
        public const string DeviceResourceTypes_UserGuide = "userguide";
        public const string DeviceResourceTypes_Specification = "specification";
        public const string DeviceResourceTypes_PartsList = "partslist";
        public const string DeviceResourceTypes_Picture = "picture";
        public const string DeviceResourceTypes_Video = "video";
        public const string DeviceResourceTypes_Audio = "audio";
        public const string DeviceResourceTypes_Other = "other";
        public const string DeviceResourceTypes_Content = "content";
        public const string DeviceResourceTypes_CompressedFile = "zip";

        public MediaResource()
        {
            IsFileUpload = true;
        }

        public string MediaTypeKey { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_FileName, FieldType: FieldTypes.FileUpload, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public string FileName { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_IsFileUpload, HelpResource: Resources.MediaServicesResources.Names.MediaResource_IsFileUpload_Help, FieldType: FieldTypes.CheckBox, ResourceType: typeof(MediaServicesResources))]
        public bool IsFileUpload { get; set; }
       
        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Link, HelpResource: MediaServicesResources.Names.MediaResource_Link_Help, FieldType: FieldTypes.WebLink, ResourceType: typeof(MediaServicesResources))]
        public string Link { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Icon, FieldType: FieldTypes.Icon, IsRequired: true, ResourceType: typeof(MediaServicesResources))]
        public string Icon { get; set; } = "icon-fo-image";

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_ContentLength, FieldType: FieldTypes.Integer, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public long? ContentSize { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_MimeType, IsUserEditable: false, FieldType: FieldTypes.Text, ResourceType: typeof(MediaServicesResources))]
        public string MimeType { get; set; }
        [FormField(LabelResource: MediaServicesResources.Names.Common_Description, FieldType: FieldTypes.MultiLineText, ResourceType: typeof(MediaServicesResources))]
        public string Description { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Content, FieldType: FieldTypes.HtmlEditor, ResourceType: typeof(MediaServicesResources))]
        public string Content { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_MediaLibrary, FieldType: FieldTypes.Text, IsRequired: false, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader MediaLibrary { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResources_ResourceType, WaterMark: MediaServicesResources.Names.MediaResources_ResourceType_Select, HelpResource: Resources.MediaServicesResources.Names.MediaResource_ResourceType_Help, IsRequired: true, EnumType: typeof(MediaResourceTypes), FieldType: FieldTypes.Picker, ResourceType: typeof(MediaServicesResources))]
        public EntityHeader<MediaResourceTypes> ResourceType { get; set; }


        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Width, FieldType: FieldTypes.Integer, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public int? Width { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_Height, FieldType: FieldTypes.Integer, IsUserEditable: false, ResourceType: typeof(MediaServicesResources))]
        public int? Height { get; set; }


        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_License, FieldType: FieldTypes.WebLink, IsUserEditable:false, ResourceType: typeof(MediaServicesResources))]
        public string License { get; set; }

        [FormField(LabelResource: MediaServicesResources.Names.MediaResource_OriginalSource, FieldType: FieldTypes.WebLink, IsUserEditable:false, ResourceType: typeof(MediaServicesResources))]
        public string OriginalUrl { get; set; }

        public string StorageReferenceName { get; set; }

        public void SetContentType(string contentType)
        {
            StorageReferenceName = $"{Id}.media";
            MimeType = "application/octet-stream";
            ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Other);

            if (contentType.ToLower().Contains("gif"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Picture);
                StorageReferenceName = $"{Id}.gif";
                MimeType = "image/gif";
            }
            else if (contentType.ToLower().Contains("png"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Picture);
                StorageReferenceName = $"{Id}.png";
                MimeType = "image/png";
            }
            else if (contentType.ToLower().Contains("jpg"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Picture);
                StorageReferenceName = $"{Id}.jpg";
                MimeType = "image/jpeg";
            }
            else if (contentType.ToLower().Contains("jpeg"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Picture);
                StorageReferenceName = $"{Id}.jpeg";
                MimeType = "image/jpeg";
            }
            else if (contentType.ToLower().Contains("webp"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Picture);
                StorageReferenceName = $"{Id}.webp";
                MimeType = "image/webp";
            }
            else if (contentType.ToLower().Contains("pdf"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Other);
                StorageReferenceName = $"{Id}.pdf";
                MimeType = "application/pdf";
            }
            else if (contentType.ToLower().Contains("csv"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Other);
                StorageReferenceName = $"{Id}.csv";
                MimeType = "text/plain";
            }
            else if (contentType.ToLower().Contains("zip"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.CompressedFile);
                StorageReferenceName = $"{Id}.zip";
                MimeType = "application/zip";
            }
            else if (contentType.ToLower().Contains("mp3"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Audio);
                StorageReferenceName = $"{Id}.mp3";
                MimeType = "audio/mpeg";
            }
            else if (contentType.ToLower().Contains("mp4"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Audio);
                StorageReferenceName = $"{Id}.mp4";
                MimeType = "audio/mp4";
            }
            else if (contentType.ToLower().Contains("ogg"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Audio);
                StorageReferenceName = $"{Id}.ogg";
                MimeType = "audio/ogg";
            }
            else if (contentType.ToLower().Contains("wav"))
            {
                ResourceType = EntityHeader<MediaResourceTypes>.Create(MediaResourceTypes.Audio);
                StorageReferenceName = $"{Id}.wav";
                MimeType = "audio/wav";
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
                Icon = Icon,
                Name = Name,
                ResourceType = ResourceType.Text,
                MimeType = MimeType,
                ContentSize = ContentSize,
                Link = Link,
                IsFileUpload = IsFileUpload,
                MediaTypeKey = MediaTypeKey,
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
                nameof(Icon),
                nameof(IsFileUpload),
                nameof(FileName),
                nameof(ResourceType),
                nameof(Description),
                nameof(Content),
            };
        }

        public List<string> GetFormFieldsCol2()
        {
            return new List<string>()
            {
                nameof(ContentSize),
                nameof(Width),
                nameof(Height),
                nameof(Link),                
                nameof(MimeType),
                nameof(License),
                nameof(OriginalUrl),
            };
        }

        public FormConditionals GetConditionalFields()
        {
            return new FormConditionals()
            {
                ConditionalFields = new List<string> { nameof(ContentSize), nameof(MimeType), nameof(Link), nameof(Content), nameof(Width), nameof(Height) },
                Conditionals = new List<FormConditional>()
                {
                    new FormConditional()
                    {
                        Field = nameof(ResourceType),
                        Value = DeviceResourceTypes_Content,
                        VisibleFields = new List<string>() {nameof(Content)}
                    },
                    new FormConditional()
                    {
                        Field = nameof(IsFileUpload),
                        Value = "true",
                        VisibleFields = new List<string>() {nameof(ContentSize), nameof(MimeType)}
                    },
                    new FormConditional()
                    {
                        Field = nameof(ResourceType),
                        Value = nameof(MediaResourceTypes.Picture),
                        VisibleFields = new List<string>() {nameof(Height), nameof(Width)}
                    },
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

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }

        
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.MediaResources_Title, MediaServicesResources.Names.MediaResource_Help,
    MediaServicesResources.Names.MediaResource_Description, EntityDescriptionAttribute.EntityTypes.Summary, ResourceType: typeof(MediaServicesResources),
    FactoryUrl: "/api/media/resource/factory", GetListUrl: "/api/media/library/{libraryid}/resources", GetUrl: "/api/media/resource/{id}", DeleteUrl: "/api/media/resource/{id}",
    SaveUrl: "/api/media/resource", Icon: "icon-fo-image")]
    public class MediaResourceSummary : ISummaryData
    {
        public string ResourceType { get; set; }
        public string MimeType { get; set; }
        public long? ContentSize { get; set; }
        public bool IsFileUpload { get; set; }
        public string Link { get; set; }
        public string Icon { get; set; }
        public bool IsPublic { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }

        public string MediaTypeKey { get; set; }

        // this looks ugly so we can standardized on inserting a media resource summary into other records rather
        // then just an entity header.
        private string _name;
        public string Name
        {
            get {
                if (String.IsNullOrEmpty(_name))
                    return Text;

                return _name;
            }
            set { _name = value; }
        }

        public string Text
        {
            get; set;
        }
    }
}
