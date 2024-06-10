/*6/7/2024 11:58:58 AM*/
using System.Globalization;
using System.Reflection;

//Resources:MediaServicesResources:Common_CreatedBy
namespace LagoVista.MediaServices.Models.Resources
{
	public class MediaServicesResources
	{
        private static global::System.Resources.ResourceManager _resourceManager;
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static global::System.Resources.ResourceManager ResourceManager 
		{
            get 
			{
                if (object.ReferenceEquals(_resourceManager, null)) 
				{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LagoVista.MediaServices.Models.Resources.MediaServicesResources", typeof(MediaServicesResources).GetTypeInfo().Assembly);
                    _resourceManager = temp;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static string GetResourceString(string key, params string[] tokens)
		{
			var culture = CultureInfo.CurrentCulture;;
            var str = ResourceManager.GetString(key, culture);

			for(int i = 0; i < tokens.Length; i += 2)
				str = str.Replace(tokens[i], tokens[i+1]);
										
            return str;
        }
        
        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
		/*
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static HtmlString GetResourceHtmlString(string key, params string[] tokens)
		{
			var str = GetResourceString(key, tokens);
							
			if(str.StartsWith("HTML:"))
				str = str.Substring(5);

			return new HtmlString(str);
        }*/
		
		public static string Common_CreatedBy { get { return GetResourceString("Common_CreatedBy"); } }
//Resources:MediaServicesResources:Common_CreationDate

		public static string Common_CreationDate { get { return GetResourceString("Common_CreationDate"); } }
//Resources:MediaServicesResources:Common_Description

		public static string Common_Description { get { return GetResourceString("Common_Description"); } }
//Resources:MediaServicesResources:Common_IsPublic

		public static string Common_IsPublic { get { return GetResourceString("Common_IsPublic"); } }
//Resources:MediaServicesResources:Common_IsRequired

		public static string Common_IsRequired { get { return GetResourceString("Common_IsRequired"); } }
//Resources:MediaServicesResources:Common_IsValid

		public static string Common_IsValid { get { return GetResourceString("Common_IsValid"); } }
//Resources:MediaServicesResources:Common_Key

		public static string Common_Key { get { return GetResourceString("Common_Key"); } }
//Resources:MediaServicesResources:Common_Key_Help

		public static string Common_Key_Help { get { return GetResourceString("Common_Key_Help"); } }
//Resources:MediaServicesResources:Common_Key_Validation

		public static string Common_Key_Validation { get { return GetResourceString("Common_Key_Validation"); } }
//Resources:MediaServicesResources:Common_LastUpdated

		public static string Common_LastUpdated { get { return GetResourceString("Common_LastUpdated"); } }
//Resources:MediaServicesResources:Common_LastUpdatedBy

		public static string Common_LastUpdatedBy { get { return GetResourceString("Common_LastUpdatedBy"); } }
//Resources:MediaServicesResources:Common_Name

		public static string Common_Name { get { return GetResourceString("Common_Name"); } }
//Resources:MediaServicesResources:Common_Note

		public static string Common_Note { get { return GetResourceString("Common_Note"); } }
//Resources:MediaServicesResources:Common_Notes

		public static string Common_Notes { get { return GetResourceString("Common_Notes"); } }
//Resources:MediaServicesResources:Common_PageNumberOne

		public static string Common_PageNumberOne { get { return GetResourceString("Common_PageNumberOne"); } }
//Resources:MediaServicesResources:Common_Resources

		public static string Common_Resources { get { return GetResourceString("Common_Resources"); } }
//Resources:MediaServicesResources:Common_UniqueId

		public static string Common_UniqueId { get { return GetResourceString("Common_UniqueId"); } }
//Resources:MediaServicesResources:Common_ValidationErrors

		public static string Common_ValidationErrors { get { return GetResourceString("Common_ValidationErrors"); } }
//Resources:MediaServicesResources:MediaLibraries_Title

		public static string MediaLibraries_Title { get { return GetResourceString("MediaLibraries_Title"); } }
//Resources:MediaServicesResources:MediaLibrary_Description

		public static string MediaLibrary_Description { get { return GetResourceString("MediaLibrary_Description"); } }
//Resources:MediaServicesResources:MediaLibrary_Help

		public static string MediaLibrary_Help { get { return GetResourceString("MediaLibrary_Help"); } }
//Resources:MediaServicesResources:MediaLibrary_MediaResources

		public static string MediaLibrary_MediaResources { get { return GetResourceString("MediaLibrary_MediaResources"); } }
//Resources:MediaServicesResources:MediaLibrary_Title

		public static string MediaLibrary_Title { get { return GetResourceString("MediaLibrary_Title"); } }
//Resources:MediaServicesResources:MediaResource_Content

		public static string MediaResource_Content { get { return GetResourceString("MediaResource_Content"); } }
//Resources:MediaServicesResources:MediaResource_ContentLength

		public static string MediaResource_ContentLength { get { return GetResourceString("MediaResource_ContentLength"); } }
//Resources:MediaServicesResources:MediaResource_Description

		public static string MediaResource_Description { get { return GetResourceString("MediaResource_Description"); } }
//Resources:MediaServicesResources:MediaResource_Help

		public static string MediaResource_Help { get { return GetResourceString("MediaResource_Help"); } }
//Resources:MediaServicesResources:MediaResource_Icon

		public static string MediaResource_Icon { get { return GetResourceString("MediaResource_Icon"); } }
//Resources:MediaServicesResources:MediaResource_IsFileUpload

		public static string MediaResource_IsFileUpload { get { return GetResourceString("MediaResource_IsFileUpload"); } }
//Resources:MediaServicesResources:MediaResource_IsFileUpload_Help

		public static string MediaResource_IsFileUpload_Help { get { return GetResourceString("MediaResource_IsFileUpload_Help"); } }
//Resources:MediaServicesResources:MediaResource_Link

		public static string MediaResource_Link { get { return GetResourceString("MediaResource_Link"); } }
//Resources:MediaServicesResources:MediaResource_Link_Help

		public static string MediaResource_Link_Help { get { return GetResourceString("MediaResource_Link_Help"); } }
//Resources:MediaServicesResources:MediaResource_MediaLibrary

		public static string MediaResource_MediaLibrary { get { return GetResourceString("MediaResource_MediaLibrary"); } }
//Resources:MediaServicesResources:MediaResource_ResourceType_Help

		public static string MediaResource_ResourceType_Help { get { return GetResourceString("MediaResource_ResourceType_Help"); } }
//Resources:MediaServicesResources:MediaResource_StorageRefName

		public static string MediaResource_StorageRefName { get { return GetResourceString("MediaResource_StorageRefName"); } }
//Resources:MediaServicesResources:MediaResource_Title

		public static string MediaResource_Title { get { return GetResourceString("MediaResource_Title"); } }
//Resources:MediaServicesResources:MediaResource_WebLink

		public static string MediaResource_WebLink { get { return GetResourceString("MediaResource_WebLink"); } }
//Resources:MediaServicesResources:MediaResources_FileName

		public static string MediaResources_FileName { get { return GetResourceString("MediaResources_FileName"); } }
//Resources:MediaServicesResources:MediaResources_MimeType

		public static string MediaResources_MimeType { get { return GetResourceString("MediaResources_MimeType"); } }
//Resources:MediaServicesResources:MediaResources_ResourceType

		public static string MediaResources_ResourceType { get { return GetResourceString("MediaResources_ResourceType"); } }
//Resources:MediaServicesResources:MediaResources_ResourceType_Select

		public static string MediaResources_ResourceType_Select { get { return GetResourceString("MediaResources_ResourceType_Select"); } }
//Resources:MediaServicesResources:MediaResources_Title

		public static string MediaResources_Title { get { return GetResourceString("MediaResources_Title"); } }
//Resources:MediaServicesResources:MediaResourceType_Content

		public static string MediaResourceType_Content { get { return GetResourceString("MediaResourceType_Content"); } }
//Resources:MediaServicesResources:MediaResourceType_Manual

		public static string MediaResourceType_Manual { get { return GetResourceString("MediaResourceType_Manual"); } }
//Resources:MediaServicesResources:MediaResourceType_Other

		public static string MediaResourceType_Other { get { return GetResourceString("MediaResourceType_Other"); } }
//Resources:MediaServicesResources:MediaResourceType_PartsList

		public static string MediaResourceType_PartsList { get { return GetResourceString("MediaResourceType_PartsList"); } }
//Resources:MediaServicesResources:MediaResourceType_Picture

		public static string MediaResourceType_Picture { get { return GetResourceString("MediaResourceType_Picture"); } }
//Resources:MediaServicesResources:MediaResourceType_Specification

		public static string MediaResourceType_Specification { get { return GetResourceString("MediaResourceType_Specification"); } }
//Resources:MediaServicesResources:MediaResourceType_UserGuide

		public static string MediaResourceType_UserGuide { get { return GetResourceString("MediaResourceType_UserGuide"); } }
//Resources:MediaServicesResources:MediaResourceType_Video

		public static string MediaResourceType_Video { get { return GetResourceString("MediaResourceType_Video"); } }
//Resources:MediaServicesResources:MediaResourceType_WebLink

		public static string MediaResourceType_WebLink { get { return GetResourceString("MediaResourceType_WebLink"); } }

		public static class Names
		{
			public const string Common_CreatedBy = "Common_CreatedBy";
			public const string Common_CreationDate = "Common_CreationDate";
			public const string Common_Description = "Common_Description";
			public const string Common_IsPublic = "Common_IsPublic";
			public const string Common_IsRequired = "Common_IsRequired";
			public const string Common_IsValid = "Common_IsValid";
			public const string Common_Key = "Common_Key";
			public const string Common_Key_Help = "Common_Key_Help";
			public const string Common_Key_Validation = "Common_Key_Validation";
			public const string Common_LastUpdated = "Common_LastUpdated";
			public const string Common_LastUpdatedBy = "Common_LastUpdatedBy";
			public const string Common_Name = "Common_Name";
			public const string Common_Note = "Common_Note";
			public const string Common_Notes = "Common_Notes";
			public const string Common_PageNumberOne = "Common_PageNumberOne";
			public const string Common_Resources = "Common_Resources";
			public const string Common_UniqueId = "Common_UniqueId";
			public const string Common_ValidationErrors = "Common_ValidationErrors";
			public const string MediaLibraries_Title = "MediaLibraries_Title";
			public const string MediaLibrary_Description = "MediaLibrary_Description";
			public const string MediaLibrary_Help = "MediaLibrary_Help";
			public const string MediaLibrary_MediaResources = "MediaLibrary_MediaResources";
			public const string MediaLibrary_Title = "MediaLibrary_Title";
			public const string MediaResource_Content = "MediaResource_Content";
			public const string MediaResource_ContentLength = "MediaResource_ContentLength";
			public const string MediaResource_Description = "MediaResource_Description";
			public const string MediaResource_Help = "MediaResource_Help";
			public const string MediaResource_Icon = "MediaResource_Icon";
			public const string MediaResource_IsFileUpload = "MediaResource_IsFileUpload";
			public const string MediaResource_IsFileUpload_Help = "MediaResource_IsFileUpload_Help";
			public const string MediaResource_Link = "MediaResource_Link";
			public const string MediaResource_Link_Help = "MediaResource_Link_Help";
			public const string MediaResource_MediaLibrary = "MediaResource_MediaLibrary";
			public const string MediaResource_ResourceType_Help = "MediaResource_ResourceType_Help";
			public const string MediaResource_StorageRefName = "MediaResource_StorageRefName";
			public const string MediaResource_Title = "MediaResource_Title";
			public const string MediaResource_WebLink = "MediaResource_WebLink";
			public const string MediaResources_FileName = "MediaResources_FileName";
			public const string MediaResources_MimeType = "MediaResources_MimeType";
			public const string MediaResources_ResourceType = "MediaResources_ResourceType";
			public const string MediaResources_ResourceType_Select = "MediaResources_ResourceType_Select";
			public const string MediaResources_Title = "MediaResources_Title";
			public const string MediaResourceType_Content = "MediaResourceType_Content";
			public const string MediaResourceType_Manual = "MediaResourceType_Manual";
			public const string MediaResourceType_Other = "MediaResourceType_Other";
			public const string MediaResourceType_PartsList = "MediaResourceType_PartsList";
			public const string MediaResourceType_Picture = "MediaResourceType_Picture";
			public const string MediaResourceType_Specification = "MediaResourceType_Specification";
			public const string MediaResourceType_UserGuide = "MediaResourceType_UserGuide";
			public const string MediaResourceType_Video = "MediaResourceType_Video";
			public const string MediaResourceType_WebLink = "MediaResourceType_WebLink";
		}
	}
}

