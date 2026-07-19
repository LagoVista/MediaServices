/*7/19/2026 3:52:51 PM*/
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
//Resources:MediaServicesResources:Common_Icon

		public static string Common_Icon { get { return GetResourceString("Common_Icon"); } }
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
//Resources:MediaServicesResources:DeviceResourceTypes_Audio

		public static string DeviceResourceTypes_Audio { get { return GetResourceString("DeviceResourceTypes_Audio"); } }
//Resources:MediaServicesResources:GeneratedImageQualities_Premium

		public static string GeneratedImageQualities_Premium { get { return GetResourceString("GeneratedImageQualities_Premium"); } }
//Resources:MediaServicesResources:GeneratedImageQualities_Standard

		public static string GeneratedImageQualities_Standard { get { return GetResourceString("GeneratedImageQualities_Standard"); } }
//Resources:MediaServicesResources:GeneratedImageSizes_Landscape

		public static string GeneratedImageSizes_Landscape { get { return GetResourceString("GeneratedImageSizes_Landscape"); } }
//Resources:MediaServicesResources:GeneratedImageSizes_Portrait

		public static string GeneratedImageSizes_Portrait { get { return GetResourceString("GeneratedImageSizes_Portrait"); } }
//Resources:MediaServicesResources:GeneratedImageSizes_Square

		public static string GeneratedImageSizes_Square { get { return GetResourceString("GeneratedImageSizes_Square"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_Abstract

		public static string GeneratedImageStyles_Abstract { get { return GetResourceString("GeneratedImageStyles_Abstract"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_CorporateMemphis

		public static string GeneratedImageStyles_CorporateMemphis { get { return GetResourceString("GeneratedImageStyles_CorporateMemphis"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_EditorialIllustration

		public static string GeneratedImageStyles_EditorialIllustration { get { return GetResourceString("GeneratedImageStyles_EditorialIllustration"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_EditorialPhotography

		public static string GeneratedImageStyles_EditorialPhotography { get { return GetResourceString("GeneratedImageStyles_EditorialPhotography"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_FlatIllustration

		public static string GeneratedImageStyles_FlatIllustration { get { return GetResourceString("GeneratedImageStyles_FlatIllustration"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_StudioPortrait

		public static string GeneratedImageStyles_StudioPortrait { get { return GetResourceString("GeneratedImageStyles_StudioPortrait"); } }
//Resources:MediaServicesResources:GeneratedImageStyles_ThreeDimensionalIllustration

		public static string GeneratedImageStyles_ThreeDimensionalIllustration { get { return GetResourceString("GeneratedImageStyles_ThreeDimensionalIllustration"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_Description

		public static string ImageGenerationRequest_Description { get { return GetResourceString("ImageGenerationRequest_Description"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_Help

		public static string ImageGenerationRequest_Help { get { return GetResourceString("ImageGenerationRequest_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageGenerationStyleGuidance

		public static string ImageGenerationRequest_ImageGenerationStyleGuidance { get { return GetResourceString("ImageGenerationRequest_ImageGenerationStyleGuidance"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageGenerationStyleGuidance_Help

		public static string ImageGenerationRequest_ImageGenerationStyleGuidance_Help { get { return GetResourceString("ImageGenerationRequest_ImageGenerationStyleGuidance_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImagePurpose

		public static string ImageGenerationRequest_ImagePurpose { get { return GetResourceString("ImageGenerationRequest_ImagePurpose"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImagePurpose_Help

		public static string ImageGenerationRequest_ImagePurpose_Help { get { return GetResourceString("ImageGenerationRequest_ImagePurpose_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageQuality

		public static string ImageGenerationRequest_ImageQuality { get { return GetResourceString("ImageGenerationRequest_ImageQuality"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageQuality_Help

		public static string ImageGenerationRequest_ImageQuality_Help { get { return GetResourceString("ImageGenerationRequest_ImageQuality_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageSize

		public static string ImageGenerationRequest_ImageSize { get { return GetResourceString("ImageGenerationRequest_ImageSize"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageSize_Help

		public static string ImageGenerationRequest_ImageSize_Help { get { return GetResourceString("ImageGenerationRequest_ImageSize_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageStyle

		public static string ImageGenerationRequest_ImageStyle { get { return GetResourceString("ImageGenerationRequest_ImageStyle"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_ImageStyle_Help

		public static string ImageGenerationRequest_ImageStyle_Help { get { return GetResourceString("ImageGenerationRequest_ImageStyle_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_IsPublic

		public static string ImageGenerationRequest_IsPublic { get { return GetResourceString("ImageGenerationRequest_IsPublic"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_IsPublic_Help

		public static string ImageGenerationRequest_IsPublic_Help { get { return GetResourceString("ImageGenerationRequest_IsPublic_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_NumberGenerated

		public static string ImageGenerationRequest_NumberGenerated { get { return GetResourceString("ImageGenerationRequest_NumberGenerated"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_NumberGenerated_Help

		public static string ImageGenerationRequest_NumberGenerated_Help { get { return GetResourceString("ImageGenerationRequest_NumberGenerated_Help"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_Title

		public static string ImageGenerationRequest_Title { get { return GetResourceString("ImageGenerationRequest_Title"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_UserPrompt

		public static string ImageGenerationRequest_UserPrompt { get { return GetResourceString("ImageGenerationRequest_UserPrompt"); } }
//Resources:MediaServicesResources:ImageGenerationRequest_UserPrompt_Help

		public static string ImageGenerationRequest_UserPrompt_Help { get { return GetResourceString("ImageGenerationRequest_UserPrompt_Help"); } }
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
//Resources:MediaServicesResources:MediaResource_Height

		public static string MediaResource_Height { get { return GetResourceString("MediaResource_Height"); } }
//Resources:MediaServicesResources:MediaResource_Help

		public static string MediaResource_Help { get { return GetResourceString("MediaResource_Help"); } }
//Resources:MediaServicesResources:MediaResource_Icon

		public static string MediaResource_Icon { get { return GetResourceString("MediaResource_Icon"); } }
//Resources:MediaServicesResources:MediaResource_IsFileUpload

		public static string MediaResource_IsFileUpload { get { return GetResourceString("MediaResource_IsFileUpload"); } }
//Resources:MediaServicesResources:MediaResource_IsFileUpload_Help

		public static string MediaResource_IsFileUpload_Help { get { return GetResourceString("MediaResource_IsFileUpload_Help"); } }
//Resources:MediaServicesResources:MediaResource_License

		public static string MediaResource_License { get { return GetResourceString("MediaResource_License"); } }
//Resources:MediaServicesResources:MediaResource_Link

		public static string MediaResource_Link { get { return GetResourceString("MediaResource_Link"); } }
//Resources:MediaServicesResources:MediaResource_Link_Help

		public static string MediaResource_Link_Help { get { return GetResourceString("MediaResource_Link_Help"); } }
//Resources:MediaServicesResources:MediaResource_MediaLibrary

		public static string MediaResource_MediaLibrary { get { return GetResourceString("MediaResource_MediaLibrary"); } }
//Resources:MediaServicesResources:MediaResource_OriginalSource

		public static string MediaResource_OriginalSource { get { return GetResourceString("MediaResource_OriginalSource"); } }
//Resources:MediaServicesResources:MediaResource_ResourceType_Help

		public static string MediaResource_ResourceType_Help { get { return GetResourceString("MediaResource_ResourceType_Help"); } }
//Resources:MediaServicesResources:MediaResource_StorageRefName

		public static string MediaResource_StorageRefName { get { return GetResourceString("MediaResource_StorageRefName"); } }
//Resources:MediaServicesResources:MediaResource_ThumbnailUrl

		public static string MediaResource_ThumbnailUrl { get { return GetResourceString("MediaResource_ThumbnailUrl"); } }
//Resources:MediaServicesResources:MediaResource_ThumbnailUrl_Help

		public static string MediaResource_ThumbnailUrl_Help { get { return GetResourceString("MediaResource_ThumbnailUrl_Help"); } }
//Resources:MediaServicesResources:MediaResource_Title

		public static string MediaResource_Title { get { return GetResourceString("MediaResource_Title"); } }
//Resources:MediaServicesResources:MediaResource_WebLink

		public static string MediaResource_WebLink { get { return GetResourceString("MediaResource_WebLink"); } }
//Resources:MediaServicesResources:MediaResource_Width

		public static string MediaResource_Width { get { return GetResourceString("MediaResource_Width"); } }
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
//Resources:MediaServicesResources:MediaResourceStatus_Deprecated

		public static string MediaResourceStatus_Deprecated { get { return GetResourceString("MediaResourceStatus_Deprecated"); } }
//Resources:MediaServicesResources:MediaResourceStatus_Failed

		public static string MediaResourceStatus_Failed { get { return GetResourceString("MediaResourceStatus_Failed"); } }
//Resources:MediaServicesResources:MediaResourceStatus_Obsolete

		public static string MediaResourceStatus_Obsolete { get { return GetResourceString("MediaResourceStatus_Obsolete"); } }
//Resources:MediaServicesResources:MediaResourceStatus_Pending

		public static string MediaResourceStatus_Pending { get { return GetResourceString("MediaResourceStatus_Pending"); } }
//Resources:MediaServicesResources:MediaResourceStatus_Ready

		public static string MediaResourceStatus_Ready { get { return GetResourceString("MediaResourceStatus_Ready"); } }
//Resources:MediaServicesResources:MediaResourceType_CompressedFile

		public static string MediaResourceType_CompressedFile { get { return GetResourceString("MediaResourceType_CompressedFile"); } }
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
//Resources:MediaServicesResources:MediaResourceType_RawVideo

		public static string MediaResourceType_RawVideo { get { return GetResourceString("MediaResourceType_RawVideo"); } }
//Resources:MediaServicesResources:MediaResourceType_Specification

		public static string MediaResourceType_Specification { get { return GetResourceString("MediaResourceType_Specification"); } }
//Resources:MediaServicesResources:MediaResourceType_UserGuide

		public static string MediaResourceType_UserGuide { get { return GetResourceString("MediaResourceType_UserGuide"); } }
//Resources:MediaServicesResources:MediaResourceType_Video

		public static string MediaResourceType_Video { get { return GetResourceString("MediaResourceType_Video"); } }
//Resources:MediaServicesResources:MediaResourceType_WebLink

		public static string MediaResourceType_WebLink { get { return GetResourceString("MediaResourceType_WebLink"); } }
//Resources:MediaServicesResources:VideoAvatar_Description

		public static string VideoAvatar_Description { get { return GetResourceString("VideoAvatar_Description"); } }
//Resources:MediaServicesResources:VideoAvatar_EditorialImage

		public static string VideoAvatar_EditorialImage { get { return GetResourceString("VideoAvatar_EditorialImage"); } }
//Resources:MediaServicesResources:VideoAvatar_ErrorMessage

		public static string VideoAvatar_ErrorMessage { get { return GetResourceString("VideoAvatar_ErrorMessage"); } }
//Resources:MediaServicesResources:VideoAvatar_Help

		public static string VideoAvatar_Help { get { return GetResourceString("VideoAvatar_Help"); } }
//Resources:MediaServicesResources:VideoAvatar_IsDefault

		public static string VideoAvatar_IsDefault { get { return GetResourceString("VideoAvatar_IsDefault"); } }
//Resources:MediaServicesResources:VideoAvatar_LanguageCode

		public static string VideoAvatar_LanguageCode { get { return GetResourceString("VideoAvatar_LanguageCode"); } }
//Resources:MediaServicesResources:VideoAvatar_LastStatusCheckUtc

		public static string VideoAvatar_LastStatusCheckUtc { get { return GetResourceString("VideoAvatar_LastStatusCheckUtc"); } }
//Resources:MediaServicesResources:VideoAvatar_LastUsedUtc

		public static string VideoAvatar_LastUsedUtc { get { return GetResourceString("VideoAvatar_LastUsedUtc"); } }
//Resources:MediaServicesResources:VideoAvatar_Locale

		public static string VideoAvatar_Locale { get { return GetResourceString("VideoAvatar_Locale"); } }
//Resources:MediaServicesResources:VideoAvatar_Provider

		public static string VideoAvatar_Provider { get { return GetResourceString("VideoAvatar_Provider"); } }
//Resources:MediaServicesResources:VideoAvatar_ProviderAssetId

		public static string VideoAvatar_ProviderAssetId { get { return GetResourceString("VideoAvatar_ProviderAssetId"); } }
//Resources:MediaServicesResources:VideoAvatar_ProviderAvatarId

		public static string VideoAvatar_ProviderAvatarId { get { return GetResourceString("VideoAvatar_ProviderAvatarId"); } }
//Resources:MediaServicesResources:VideoAvatar_ProviderAvatarStatus

		public static string VideoAvatar_ProviderAvatarStatus { get { return GetResourceString("VideoAvatar_ProviderAvatarStatus"); } }
//Resources:MediaServicesResources:VideoAvatar_Role

		public static string VideoAvatar_Role { get { return GetResourceString("VideoAvatar_Role"); } }
//Resources:MediaServicesResources:VideoAvatar_SourceImage

		public static string VideoAvatar_SourceImage { get { return GetResourceString("VideoAvatar_SourceImage"); } }
//Resources:MediaServicesResources:VideoAvatar_Status

		public static string VideoAvatar_Status { get { return GetResourceString("VideoAvatar_Status"); } }
//Resources:MediaServicesResources:VideoAvatar_SubjectEntity

		public static string VideoAvatar_SubjectEntity { get { return GetResourceString("VideoAvatar_SubjectEntity"); } }
//Resources:MediaServicesResources:VideoAvatar_Title

		public static string VideoAvatar_Title { get { return GetResourceString("VideoAvatar_Title"); } }
//Resources:MediaServicesResources:VideoAvatar_VoiceId

		public static string VideoAvatar_VoiceId { get { return GetResourceString("VideoAvatar_VoiceId"); } }
//Resources:MediaServicesResources:VideoAvatar_VoiceName

		public static string VideoAvatar_VoiceName { get { return GetResourceString("VideoAvatar_VoiceName"); } }
//Resources:MediaServicesResources:VideoAvatarProvider_HeyGen

		public static string VideoAvatarProvider_HeyGen { get { return GetResourceString("VideoAvatarProvider_HeyGen"); } }
//Resources:MediaServicesResources:VideoAvatarRole_Campaign

		public static string VideoAvatarRole_Campaign { get { return GetResourceString("VideoAvatarRole_Campaign"); } }
//Resources:MediaServicesResources:VideoAvatarRole_Editorial

		public static string VideoAvatarRole_Editorial { get { return GetResourceString("VideoAvatarRole_Editorial"); } }
//Resources:MediaServicesResources:VideoAvatarRole_Experimental

		public static string VideoAvatarRole_Experimental { get { return GetResourceString("VideoAvatarRole_Experimental"); } }
//Resources:MediaServicesResources:VideoAvatarRole_Primary

		public static string VideoAvatarRole_Primary { get { return GetResourceString("VideoAvatarRole_Primary"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_Archived

		public static string VideoAvatarStatus_Archived { get { return GetResourceString("VideoAvatarStatus_Archived"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_Draft

		public static string VideoAvatarStatus_Draft { get { return GetResourceString("VideoAvatarStatus_Draft"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_Failed

		public static string VideoAvatarStatus_Failed { get { return GetResourceString("VideoAvatarStatus_Failed"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_Preparing

		public static string VideoAvatarStatus_Preparing { get { return GetResourceString("VideoAvatarStatus_Preparing"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_Ready

		public static string VideoAvatarStatus_Ready { get { return GetResourceString("VideoAvatarStatus_Ready"); } }
//Resources:MediaServicesResources:VideoAvatarStatus_WaitingForProvider

		public static string VideoAvatarStatus_WaitingForProvider { get { return GetResourceString("VideoAvatarStatus_WaitingForProvider"); } }
//Resources:MediaServicesResources:VideoComposition_BackgroundMediaResource

		public static string VideoComposition_BackgroundMediaResource { get { return GetResourceString("VideoComposition_BackgroundMediaResource"); } }
//Resources:MediaServicesResources:VideoComposition_Blocks

		public static string VideoComposition_Blocks { get { return GetResourceString("VideoComposition_Blocks"); } }
//Resources:MediaServicesResources:VideoComposition_Description

		public static string VideoComposition_Description { get { return GetResourceString("VideoComposition_Description"); } }
//Resources:MediaServicesResources:VideoComposition_ErrorMessage

		public static string VideoComposition_ErrorMessage { get { return GetResourceString("VideoComposition_ErrorMessage"); } }
//Resources:MediaServicesResources:VideoComposition_Help

		public static string VideoComposition_Help { get { return GetResourceString("VideoComposition_Help"); } }
//Resources:MediaServicesResources:VideoComposition_OutputMediaResource

		public static string VideoComposition_OutputMediaResource { get { return GetResourceString("VideoComposition_OutputMediaResource"); } }
//Resources:MediaServicesResources:VideoComposition_Status

		public static string VideoComposition_Status { get { return GetResourceString("VideoComposition_Status"); } }
//Resources:MediaServicesResources:VideoComposition_Title

		public static string VideoComposition_Title { get { return GetResourceString("VideoComposition_Title"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_Completed

		public static string VideoCompositionAssemblyStage_Completed { get { return GetResourceString("VideoCompositionAssemblyStage_Completed"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_DownloadingMedia

		public static string VideoCompositionAssemblyStage_DownloadingMedia { get { return GetResourceString("VideoCompositionAssemblyStage_DownloadingMedia"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_Encoding

		public static string VideoCompositionAssemblyStage_Encoding { get { return GetResourceString("VideoCompositionAssemblyStage_Encoding"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_Failed

		public static string VideoCompositionAssemblyStage_Failed { get { return GetResourceString("VideoCompositionAssemblyStage_Failed"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_GeneratingThumbnail

		public static string VideoCompositionAssemblyStage_GeneratingThumbnail { get { return GetResourceString("VideoCompositionAssemblyStage_GeneratingThumbnail"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_InspectingMedia

		public static string VideoCompositionAssemblyStage_InspectingMedia { get { return GetResourceString("VideoCompositionAssemblyStage_InspectingMedia"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_None

		public static string VideoCompositionAssemblyStage_None { get { return GetResourceString("VideoCompositionAssemblyStage_None"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_NormalizingMedia

		public static string VideoCompositionAssemblyStage_NormalizingMedia { get { return GetResourceString("VideoCompositionAssemblyStage_NormalizingMedia"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_Queued

		public static string VideoCompositionAssemblyStage_Queued { get { return GetResourceString("VideoCompositionAssemblyStage_Queued"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_RenderingLabels

		public static string VideoCompositionAssemblyStage_RenderingLabels { get { return GetResourceString("VideoCompositionAssemblyStage_RenderingLabels"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_UploadingThumbnail

		public static string VideoCompositionAssemblyStage_UploadingThumbnail { get { return GetResourceString("VideoCompositionAssemblyStage_UploadingThumbnail"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_UploadingToAzure

		public static string VideoCompositionAssemblyStage_UploadingToAzure { get { return GetResourceString("VideoCompositionAssemblyStage_UploadingToAzure"); } }
//Resources:MediaServicesResources:VideoCompositionAssemblyStage_UploadingToVimeo

		public static string VideoCompositionAssemblyStage_UploadingToVimeo { get { return GetResourceString("VideoCompositionAssemblyStage_UploadingToVimeo"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_BackgroundMediaResource

		public static string VideoCompositionBlock_BackgroundMediaResource { get { return GetResourceString("VideoCompositionBlock_BackgroundMediaResource"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Description

		public static string VideoCompositionBlock_Description { get { return GetResourceString("VideoCompositionBlock_Description"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_DurationSeconds

		public static string VideoCompositionBlock_DurationSeconds { get { return GetResourceString("VideoCompositionBlock_DurationSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_FadeInSeconds

		public static string VideoCompositionBlock_FadeInSeconds { get { return GetResourceString("VideoCompositionBlock_FadeInSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_FadeOutSeconds

		public static string VideoCompositionBlock_FadeOutSeconds { get { return GetResourceString("VideoCompositionBlock_FadeOutSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Help

		public static string VideoCompositionBlock_Help { get { return GetResourceString("VideoCompositionBlock_Help"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Key

		public static string VideoCompositionBlock_Key { get { return GetResourceString("VideoCompositionBlock_Key"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Labels

		public static string VideoCompositionBlock_Labels { get { return GetResourceString("VideoCompositionBlock_Labels"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_MediaResource

		public static string VideoCompositionBlock_MediaResource { get { return GetResourceString("VideoCompositionBlock_MediaResource"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_MediaResourceFileName

		public static string VideoCompositionBlock_MediaResourceFileName { get { return GetResourceString("VideoCompositionBlock_MediaResourceFileName"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_MediaResourceMimeType

		public static string VideoCompositionBlock_MediaResourceMimeType { get { return GetResourceString("VideoCompositionBlock_MediaResourceMimeType"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_PresenterPositionX

		public static string VideoCompositionBlock_PresenterPositionX { get { return GetResourceString("VideoCompositionBlock_PresenterPositionX"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_PresenterPositionY

		public static string VideoCompositionBlock_PresenterPositionY { get { return GetResourceString("VideoCompositionBlock_PresenterPositionY"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_PresenterScale

		public static string VideoCompositionBlock_PresenterScale { get { return GetResourceString("VideoCompositionBlock_PresenterScale"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_SortOrder

		public static string VideoCompositionBlock_SortOrder { get { return GetResourceString("VideoCompositionBlock_SortOrder"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Title

		public static string VideoCompositionBlock_Title { get { return GetResourceString("VideoCompositionBlock_Title"); } }
//Resources:MediaServicesResources:VideoCompositionBlock_Type

		public static string VideoCompositionBlock_Type { get { return GetResourceString("VideoCompositionBlock_Type"); } }
//Resources:MediaServicesResources:VideoCompositionBlockType_Image

		public static string VideoCompositionBlockType_Image { get { return GetResourceString("VideoCompositionBlockType_Image"); } }
//Resources:MediaServicesResources:VideoCompositionBlockType_Video

		public static string VideoCompositionBlockType_Video { get { return GetResourceString("VideoCompositionBlockType_Video"); } }
//Resources:MediaServicesResources:VideoCompositions_Title

		public static string VideoCompositions_Title { get { return GetResourceString("VideoCompositions_Title"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Assembling

		public static string VideoCompositionStatus_Assembling { get { return GetResourceString("VideoCompositionStatus_Assembling"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Cancelled

		public static string VideoCompositionStatus_Cancelled { get { return GetResourceString("VideoCompositionStatus_Cancelled"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Completed

		public static string VideoCompositionStatus_Completed { get { return GetResourceString("VideoCompositionStatus_Completed"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Draft

		public static string VideoCompositionStatus_Draft { get { return GetResourceString("VideoCompositionStatus_Draft"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Failed

		public static string VideoCompositionStatus_Failed { get { return GetResourceString("VideoCompositionStatus_Failed"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Preparing

		public static string VideoCompositionStatus_Preparing { get { return GetResourceString("VideoCompositionStatus_Preparing"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_ProcessingAtVimeo

		public static string VideoCompositionStatus_ProcessingAtVimeo { get { return GetResourceString("VideoCompositionStatus_ProcessingAtVimeo"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Queued

		public static string VideoCompositionStatus_Queued { get { return GetResourceString("VideoCompositionStatus_Queued"); } }
//Resources:MediaServicesResources:VideoCompositionStatus_Uploading

		public static string VideoCompositionStatus_Uploading { get { return GetResourceString("VideoCompositionStatus_Uploading"); } }
//Resources:MediaServicesResources:VideoCompositionTextAlignment_Center

		public static string VideoCompositionTextAlignment_Center { get { return GetResourceString("VideoCompositionTextAlignment_Center"); } }
//Resources:MediaServicesResources:VideoCompositionTextAlignment_Left

		public static string VideoCompositionTextAlignment_Left { get { return GetResourceString("VideoCompositionTextAlignment_Left"); } }
//Resources:MediaServicesResources:VideoCompositionTextAlignment_Right

		public static string VideoCompositionTextAlignment_Right { get { return GetResourceString("VideoCompositionTextAlignment_Right"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Alignment

		public static string VideoCompositionTextLabel_Alignment { get { return GetResourceString("VideoCompositionTextLabel_Alignment"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Bold

		public static string VideoCompositionTextLabel_Bold { get { return GetResourceString("VideoCompositionTextLabel_Bold"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Color

		public static string VideoCompositionTextLabel_Color { get { return GetResourceString("VideoCompositionTextLabel_Color"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_DelaySeconds

		public static string VideoCompositionTextLabel_DelaySeconds { get { return GetResourceString("VideoCompositionTextLabel_DelaySeconds"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Description

		public static string VideoCompositionTextLabel_Description { get { return GetResourceString("VideoCompositionTextLabel_Description"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_FadeInSeconds

		public static string VideoCompositionTextLabel_FadeInSeconds { get { return GetResourceString("VideoCompositionTextLabel_FadeInSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_FadeOutSeconds

		public static string VideoCompositionTextLabel_FadeOutSeconds { get { return GetResourceString("VideoCompositionTextLabel_FadeOutSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_FontSize

		public static string VideoCompositionTextLabel_FontSize { get { return GetResourceString("VideoCompositionTextLabel_FontSize"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Help

		public static string VideoCompositionTextLabel_Help { get { return GetResourceString("VideoCompositionTextLabel_Help"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_MaxWidth

		public static string VideoCompositionTextLabel_MaxWidth { get { return GetResourceString("VideoCompositionTextLabel_MaxWidth"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Text

		public static string VideoCompositionTextLabel_Text { get { return GetResourceString("VideoCompositionTextLabel_Text"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Title

		public static string VideoCompositionTextLabel_Title { get { return GetResourceString("VideoCompositionTextLabel_Title"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_VisibleDurationSeconds

		public static string VideoCompositionTextLabel_VisibleDurationSeconds { get { return GetResourceString("VideoCompositionTextLabel_VisibleDurationSeconds"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_X

		public static string VideoCompositionTextLabel_X { get { return GetResourceString("VideoCompositionTextLabel_X"); } }
//Resources:MediaServicesResources:VideoCompositionTextLabel_Y

		public static string VideoCompositionTextLabel_Y { get { return GetResourceString("VideoCompositionTextLabel_Y"); } }
//Resources:MediaServicesResources:VideoProduction_BackgroundMediaResource

		public static string VideoProduction_BackgroundMediaResource { get { return GetResourceString("VideoProduction_BackgroundMediaResource"); } }
//Resources:MediaServicesResources:VideoProduction_Description

		public static string VideoProduction_Description { get { return GetResourceString("VideoProduction_Description"); } }
//Resources:MediaServicesResources:VideoProduction_ErrorMessage

		public static string VideoProduction_ErrorMessage { get { return GetResourceString("VideoProduction_ErrorMessage"); } }
//Resources:MediaServicesResources:VideoProduction_FinalVideoMediaResource

		public static string VideoProduction_FinalVideoMediaResource { get { return GetResourceString("VideoProduction_FinalVideoMediaResource"); } }
//Resources:MediaServicesResources:VideoProduction_Help

		public static string VideoProduction_Help { get { return GetResourceString("VideoProduction_Help"); } }
//Resources:MediaServicesResources:VideoProduction_LanguageCode

		public static string VideoProduction_LanguageCode { get { return GetResourceString("VideoProduction_LanguageCode"); } }
//Resources:MediaServicesResources:VideoProduction_Locale

		public static string VideoProduction_Locale { get { return GetResourceString("VideoProduction_Locale"); } }
//Resources:MediaServicesResources:VideoProduction_PreviewAudioMediaResource

		public static string VideoProduction_PreviewAudioMediaResource { get { return GetResourceString("VideoProduction_PreviewAudioMediaResource"); } }
//Resources:MediaServicesResources:VideoProduction_Provider

		public static string VideoProduction_Provider { get { return GetResourceString("VideoProduction_Provider"); } }
//Resources:MediaServicesResources:VideoProduction_Script

		public static string VideoProduction_Script { get { return GetResourceString("VideoProduction_Script"); } }
//Resources:MediaServicesResources:VideoProduction_Status

		public static string VideoProduction_Status { get { return GetResourceString("VideoProduction_Status"); } }
//Resources:MediaServicesResources:VideoProduction_TargetEntityId

		public static string VideoProduction_TargetEntityId { get { return GetResourceString("VideoProduction_TargetEntityId"); } }
//Resources:MediaServicesResources:VideoProduction_TargetEntityName

		public static string VideoProduction_TargetEntityName { get { return GetResourceString("VideoProduction_TargetEntityName"); } }
//Resources:MediaServicesResources:VideoProduction_TargetEntityProperty

		public static string VideoProduction_TargetEntityProperty { get { return GetResourceString("VideoProduction_TargetEntityProperty"); } }
//Resources:MediaServicesResources:VideoProduction_TargetEntityType

		public static string VideoProduction_TargetEntityType { get { return GetResourceString("VideoProduction_TargetEntityType"); } }
//Resources:MediaServicesResources:VideoProduction_Title

		public static string VideoProduction_Title { get { return GetResourceString("VideoProduction_Title"); } }
//Resources:MediaServicesResources:VideoProduction_VideoAvatar

		public static string VideoProduction_VideoAvatar { get { return GetResourceString("VideoProduction_VideoAvatar"); } }
//Resources:MediaServicesResources:VideoProduction_VideoName

		public static string VideoProduction_VideoName { get { return GetResourceString("VideoProduction_VideoName"); } }
//Resources:MediaServicesResources:VideoProduction_VoiceId

		public static string VideoProduction_VoiceId { get { return GetResourceString("VideoProduction_VoiceId"); } }
//Resources:MediaServicesResources:VideoProduction_VoiceName

		public static string VideoProduction_VoiceName { get { return GetResourceString("VideoProduction_VoiceName"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Auto

		public static string VideoProductionAspectRatio_Auto { get { return GetResourceString("VideoProductionAspectRatio_Auto"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Landscape16x9

		public static string VideoProductionAspectRatio_Landscape16x9 { get { return GetResourceString("VideoProductionAspectRatio_Landscape16x9"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Landscape5x4

		public static string VideoProductionAspectRatio_Landscape5x4 { get { return GetResourceString("VideoProductionAspectRatio_Landscape5x4"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Portrait4x5

		public static string VideoProductionAspectRatio_Portrait4x5 { get { return GetResourceString("VideoProductionAspectRatio_Portrait4x5"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Portrait9x16

		public static string VideoProductionAspectRatio_Portrait9x16 { get { return GetResourceString("VideoProductionAspectRatio_Portrait9x16"); } }
//Resources:MediaServicesResources:VideoProductionAspectRatio_Square1x1

		public static string VideoProductionAspectRatio_Square1x1 { get { return GetResourceString("VideoProductionAspectRatio_Square1x1"); } }
//Resources:MediaServicesResources:VideoProductionEngine_AvatarIII

		public static string VideoProductionEngine_AvatarIII { get { return GetResourceString("VideoProductionEngine_AvatarIII"); } }
//Resources:MediaServicesResources:VideoProductionEngine_AvatarIV

		public static string VideoProductionEngine_AvatarIV { get { return GetResourceString("VideoProductionEngine_AvatarIV"); } }
//Resources:MediaServicesResources:VideoProductionEngine_AvatarV

		public static string VideoProductionEngine_AvatarV { get { return GetResourceString("VideoProductionEngine_AvatarV"); } }
//Resources:MediaServicesResources:VideoProductionExpressiveness_High

		public static string VideoProductionExpressiveness_High { get { return GetResourceString("VideoProductionExpressiveness_High"); } }
//Resources:MediaServicesResources:VideoProductionExpressiveness_Low

		public static string VideoProductionExpressiveness_Low { get { return GetResourceString("VideoProductionExpressiveness_Low"); } }
//Resources:MediaServicesResources:VideoProductionExpressiveness_Medium

		public static string VideoProductionExpressiveness_Medium { get { return GetResourceString("VideoProductionExpressiveness_Medium"); } }
//Resources:MediaServicesResources:VideoProductionFit_Automatic

		public static string VideoProductionFit_Automatic { get { return GetResourceString("VideoProductionFit_Automatic"); } }
//Resources:MediaServicesResources:VideoProductionFit_Contain

		public static string VideoProductionFit_Contain { get { return GetResourceString("VideoProductionFit_Contain"); } }
//Resources:MediaServicesResources:VideoProductionFit_Cover

		public static string VideoProductionFit_Cover { get { return GetResourceString("VideoProductionFit_Cover"); } }
//Resources:MediaServicesResources:VideoProductionProvider_HeyGen

		public static string VideoProductionProvider_HeyGen { get { return GetResourceString("VideoProductionProvider_HeyGen"); } }
//Resources:MediaServicesResources:VideoProductionProvider_QualityPremium

		public static string VideoProductionProvider_QualityPremium { get { return GetResourceString("VideoProductionProvider_QualityPremium"); } }
//Resources:MediaServicesResources:VideoProductionProvider_QualityStandard

		public static string VideoProductionProvider_QualityStandard { get { return GetResourceString("VideoProductionProvider_QualityStandard"); } }
//Resources:MediaServicesResources:VideoProductionResolution_FullHD1080

		public static string VideoProductionResolution_FullHD1080 { get { return GetResourceString("VideoProductionResolution_FullHD1080"); } }
//Resources:MediaServicesResources:VideoProductionResolution_HD720

		public static string VideoProductionResolution_HD720 { get { return GetResourceString("VideoProductionResolution_HD720"); } }
//Resources:MediaServicesResources:VideoProductionResolution_UHD4K

		public static string VideoProductionResolution_UHD4K { get { return GetResourceString("VideoProductionResolution_UHD4K"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Cancelled

		public static string VideoProductionStatus_Cancelled { get { return GetResourceString("VideoProductionStatus_Cancelled"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Completed

		public static string VideoProductionStatus_Completed { get { return GetResourceString("VideoProductionStatus_Completed"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Draft

		public static string VideoProductionStatus_Draft { get { return GetResourceString("VideoProductionStatus_Draft"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Failed

		public static string VideoProductionStatus_Failed { get { return GetResourceString("VideoProductionStatus_Failed"); } }
//Resources:MediaServicesResources:VideoProductionStatus_GeneratingPreviewAudio

		public static string VideoProductionStatus_GeneratingPreviewAudio { get { return GetResourceString("VideoProductionStatus_GeneratingPreviewAudio"); } }
//Resources:MediaServicesResources:VideoProductionStatus_ImportingProviderVideo

		public static string VideoProductionStatus_ImportingProviderVideo { get { return GetResourceString("VideoProductionStatus_ImportingProviderVideo"); } }
//Resources:MediaServicesResources:VideoProductionStatus_ImportingToVimeo

		public static string VideoProductionStatus_ImportingToVimeo { get { return GetResourceString("VideoProductionStatus_ImportingToVimeo"); } }
//Resources:MediaServicesResources:VideoProductionStatus_PreparingAvatar

		public static string VideoProductionStatus_PreparingAvatar { get { return GetResourceString("VideoProductionStatus_PreparingAvatar"); } }
//Resources:MediaServicesResources:VideoProductionStatus_PreviewAudioReady

		public static string VideoProductionStatus_PreviewAudioReady { get { return GetResourceString("VideoProductionStatus_PreviewAudioReady"); } }
//Resources:MediaServicesResources:VideoProductionStatus_ProcessingAtVimeo

		public static string VideoProductionStatus_ProcessingAtVimeo { get { return GetResourceString("VideoProductionStatus_ProcessingAtVimeo"); } }
//Resources:MediaServicesResources:VideoProductionStatus_ProviderCompleted

		public static string VideoProductionStatus_ProviderCompleted { get { return GetResourceString("VideoProductionStatus_ProviderCompleted"); } }
//Resources:MediaServicesResources:VideoProductionStatus_ProviderVideoReady

		public static string VideoProductionStatus_ProviderVideoReady { get { return GetResourceString("VideoProductionStatus_ProviderVideoReady"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Rendering

		public static string VideoProductionStatus_Rendering { get { return GetResourceString("VideoProductionStatus_Rendering"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Submitted

		public static string VideoProductionStatus_Submitted { get { return GetResourceString("VideoProductionStatus_Submitted"); } }
//Resources:MediaServicesResources:VideoProductionStatus_Submitting

		public static string VideoProductionStatus_Submitting { get { return GetResourceString("VideoProductionStatus_Submitting"); } }
//Resources:MediaServicesResources:VideoProductionStatus_UpdatingEntity

		public static string VideoProductionStatus_UpdatingEntity { get { return GetResourceString("VideoProductionStatus_UpdatingEntity"); } }
//Resources:MediaServicesResources:VideoProductionStatus_UploadingBackground

		public static string VideoProductionStatus_UploadingBackground { get { return GetResourceString("VideoProductionStatus_UploadingBackground"); } }
//Resources:MediaServicesResources:VideoProductionStatus_WaitingForAvatar

		public static string VideoProductionStatus_WaitingForAvatar { get { return GetResourceString("VideoProductionStatus_WaitingForAvatar"); } }

		public static class Names
		{
			public const string Common_CreatedBy = "Common_CreatedBy";
			public const string Common_CreationDate = "Common_CreationDate";
			public const string Common_Description = "Common_Description";
			public const string Common_Icon = "Common_Icon";
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
			public const string DeviceResourceTypes_Audio = "DeviceResourceTypes_Audio";
			public const string GeneratedImageQualities_Premium = "GeneratedImageQualities_Premium";
			public const string GeneratedImageQualities_Standard = "GeneratedImageQualities_Standard";
			public const string GeneratedImageSizes_Landscape = "GeneratedImageSizes_Landscape";
			public const string GeneratedImageSizes_Portrait = "GeneratedImageSizes_Portrait";
			public const string GeneratedImageSizes_Square = "GeneratedImageSizes_Square";
			public const string GeneratedImageStyles_Abstract = "GeneratedImageStyles_Abstract";
			public const string GeneratedImageStyles_CorporateMemphis = "GeneratedImageStyles_CorporateMemphis";
			public const string GeneratedImageStyles_EditorialIllustration = "GeneratedImageStyles_EditorialIllustration";
			public const string GeneratedImageStyles_EditorialPhotography = "GeneratedImageStyles_EditorialPhotography";
			public const string GeneratedImageStyles_FlatIllustration = "GeneratedImageStyles_FlatIllustration";
			public const string GeneratedImageStyles_StudioPortrait = "GeneratedImageStyles_StudioPortrait";
			public const string GeneratedImageStyles_ThreeDimensionalIllustration = "GeneratedImageStyles_ThreeDimensionalIllustration";
			public const string ImageGenerationRequest_Description = "ImageGenerationRequest_Description";
			public const string ImageGenerationRequest_Help = "ImageGenerationRequest_Help";
			public const string ImageGenerationRequest_ImageGenerationStyleGuidance = "ImageGenerationRequest_ImageGenerationStyleGuidance";
			public const string ImageGenerationRequest_ImageGenerationStyleGuidance_Help = "ImageGenerationRequest_ImageGenerationStyleGuidance_Help";
			public const string ImageGenerationRequest_ImagePurpose = "ImageGenerationRequest_ImagePurpose";
			public const string ImageGenerationRequest_ImagePurpose_Help = "ImageGenerationRequest_ImagePurpose_Help";
			public const string ImageGenerationRequest_ImageQuality = "ImageGenerationRequest_ImageQuality";
			public const string ImageGenerationRequest_ImageQuality_Help = "ImageGenerationRequest_ImageQuality_Help";
			public const string ImageGenerationRequest_ImageSize = "ImageGenerationRequest_ImageSize";
			public const string ImageGenerationRequest_ImageSize_Help = "ImageGenerationRequest_ImageSize_Help";
			public const string ImageGenerationRequest_ImageStyle = "ImageGenerationRequest_ImageStyle";
			public const string ImageGenerationRequest_ImageStyle_Help = "ImageGenerationRequest_ImageStyle_Help";
			public const string ImageGenerationRequest_IsPublic = "ImageGenerationRequest_IsPublic";
			public const string ImageGenerationRequest_IsPublic_Help = "ImageGenerationRequest_IsPublic_Help";
			public const string ImageGenerationRequest_NumberGenerated = "ImageGenerationRequest_NumberGenerated";
			public const string ImageGenerationRequest_NumberGenerated_Help = "ImageGenerationRequest_NumberGenerated_Help";
			public const string ImageGenerationRequest_Title = "ImageGenerationRequest_Title";
			public const string ImageGenerationRequest_UserPrompt = "ImageGenerationRequest_UserPrompt";
			public const string ImageGenerationRequest_UserPrompt_Help = "ImageGenerationRequest_UserPrompt_Help";
			public const string MediaLibraries_Title = "MediaLibraries_Title";
			public const string MediaLibrary_Description = "MediaLibrary_Description";
			public const string MediaLibrary_Help = "MediaLibrary_Help";
			public const string MediaLibrary_MediaResources = "MediaLibrary_MediaResources";
			public const string MediaLibrary_Title = "MediaLibrary_Title";
			public const string MediaResource_Content = "MediaResource_Content";
			public const string MediaResource_ContentLength = "MediaResource_ContentLength";
			public const string MediaResource_Description = "MediaResource_Description";
			public const string MediaResource_Height = "MediaResource_Height";
			public const string MediaResource_Help = "MediaResource_Help";
			public const string MediaResource_Icon = "MediaResource_Icon";
			public const string MediaResource_IsFileUpload = "MediaResource_IsFileUpload";
			public const string MediaResource_IsFileUpload_Help = "MediaResource_IsFileUpload_Help";
			public const string MediaResource_License = "MediaResource_License";
			public const string MediaResource_Link = "MediaResource_Link";
			public const string MediaResource_Link_Help = "MediaResource_Link_Help";
			public const string MediaResource_MediaLibrary = "MediaResource_MediaLibrary";
			public const string MediaResource_OriginalSource = "MediaResource_OriginalSource";
			public const string MediaResource_ResourceType_Help = "MediaResource_ResourceType_Help";
			public const string MediaResource_StorageRefName = "MediaResource_StorageRefName";
			public const string MediaResource_ThumbnailUrl = "MediaResource_ThumbnailUrl";
			public const string MediaResource_ThumbnailUrl_Help = "MediaResource_ThumbnailUrl_Help";
			public const string MediaResource_Title = "MediaResource_Title";
			public const string MediaResource_WebLink = "MediaResource_WebLink";
			public const string MediaResource_Width = "MediaResource_Width";
			public const string MediaResources_FileName = "MediaResources_FileName";
			public const string MediaResources_MimeType = "MediaResources_MimeType";
			public const string MediaResources_ResourceType = "MediaResources_ResourceType";
			public const string MediaResources_ResourceType_Select = "MediaResources_ResourceType_Select";
			public const string MediaResources_Title = "MediaResources_Title";
			public const string MediaResourceStatus_Deprecated = "MediaResourceStatus_Deprecated";
			public const string MediaResourceStatus_Failed = "MediaResourceStatus_Failed";
			public const string MediaResourceStatus_Obsolete = "MediaResourceStatus_Obsolete";
			public const string MediaResourceStatus_Pending = "MediaResourceStatus_Pending";
			public const string MediaResourceStatus_Ready = "MediaResourceStatus_Ready";
			public const string MediaResourceType_CompressedFile = "MediaResourceType_CompressedFile";
			public const string MediaResourceType_Content = "MediaResourceType_Content";
			public const string MediaResourceType_Manual = "MediaResourceType_Manual";
			public const string MediaResourceType_Other = "MediaResourceType_Other";
			public const string MediaResourceType_PartsList = "MediaResourceType_PartsList";
			public const string MediaResourceType_Picture = "MediaResourceType_Picture";
			public const string MediaResourceType_RawVideo = "MediaResourceType_RawVideo";
			public const string MediaResourceType_Specification = "MediaResourceType_Specification";
			public const string MediaResourceType_UserGuide = "MediaResourceType_UserGuide";
			public const string MediaResourceType_Video = "MediaResourceType_Video";
			public const string MediaResourceType_WebLink = "MediaResourceType_WebLink";
			public const string VideoAvatar_Description = "VideoAvatar_Description";
			public const string VideoAvatar_EditorialImage = "VideoAvatar_EditorialImage";
			public const string VideoAvatar_ErrorMessage = "VideoAvatar_ErrorMessage";
			public const string VideoAvatar_Help = "VideoAvatar_Help";
			public const string VideoAvatar_IsDefault = "VideoAvatar_IsDefault";
			public const string VideoAvatar_LanguageCode = "VideoAvatar_LanguageCode";
			public const string VideoAvatar_LastStatusCheckUtc = "VideoAvatar_LastStatusCheckUtc";
			public const string VideoAvatar_LastUsedUtc = "VideoAvatar_LastUsedUtc";
			public const string VideoAvatar_Locale = "VideoAvatar_Locale";
			public const string VideoAvatar_Provider = "VideoAvatar_Provider";
			public const string VideoAvatar_ProviderAssetId = "VideoAvatar_ProviderAssetId";
			public const string VideoAvatar_ProviderAvatarId = "VideoAvatar_ProviderAvatarId";
			public const string VideoAvatar_ProviderAvatarStatus = "VideoAvatar_ProviderAvatarStatus";
			public const string VideoAvatar_Role = "VideoAvatar_Role";
			public const string VideoAvatar_SourceImage = "VideoAvatar_SourceImage";
			public const string VideoAvatar_Status = "VideoAvatar_Status";
			public const string VideoAvatar_SubjectEntity = "VideoAvatar_SubjectEntity";
			public const string VideoAvatar_Title = "VideoAvatar_Title";
			public const string VideoAvatar_VoiceId = "VideoAvatar_VoiceId";
			public const string VideoAvatar_VoiceName = "VideoAvatar_VoiceName";
			public const string VideoAvatarProvider_HeyGen = "VideoAvatarProvider_HeyGen";
			public const string VideoAvatarRole_Campaign = "VideoAvatarRole_Campaign";
			public const string VideoAvatarRole_Editorial = "VideoAvatarRole_Editorial";
			public const string VideoAvatarRole_Experimental = "VideoAvatarRole_Experimental";
			public const string VideoAvatarRole_Primary = "VideoAvatarRole_Primary";
			public const string VideoAvatarStatus_Archived = "VideoAvatarStatus_Archived";
			public const string VideoAvatarStatus_Draft = "VideoAvatarStatus_Draft";
			public const string VideoAvatarStatus_Failed = "VideoAvatarStatus_Failed";
			public const string VideoAvatarStatus_Preparing = "VideoAvatarStatus_Preparing";
			public const string VideoAvatarStatus_Ready = "VideoAvatarStatus_Ready";
			public const string VideoAvatarStatus_WaitingForProvider = "VideoAvatarStatus_WaitingForProvider";
			public const string VideoComposition_BackgroundMediaResource = "VideoComposition_BackgroundMediaResource";
			public const string VideoComposition_Blocks = "VideoComposition_Blocks";
			public const string VideoComposition_Description = "VideoComposition_Description";
			public const string VideoComposition_ErrorMessage = "VideoComposition_ErrorMessage";
			public const string VideoComposition_Help = "VideoComposition_Help";
			public const string VideoComposition_OutputMediaResource = "VideoComposition_OutputMediaResource";
			public const string VideoComposition_Status = "VideoComposition_Status";
			public const string VideoComposition_Title = "VideoComposition_Title";
			public const string VideoCompositionAssemblyStage_Completed = "VideoCompositionAssemblyStage_Completed";
			public const string VideoCompositionAssemblyStage_DownloadingMedia = "VideoCompositionAssemblyStage_DownloadingMedia";
			public const string VideoCompositionAssemblyStage_Encoding = "VideoCompositionAssemblyStage_Encoding";
			public const string VideoCompositionAssemblyStage_Failed = "VideoCompositionAssemblyStage_Failed";
			public const string VideoCompositionAssemblyStage_GeneratingThumbnail = "VideoCompositionAssemblyStage_GeneratingThumbnail";
			public const string VideoCompositionAssemblyStage_InspectingMedia = "VideoCompositionAssemblyStage_InspectingMedia";
			public const string VideoCompositionAssemblyStage_None = "VideoCompositionAssemblyStage_None";
			public const string VideoCompositionAssemblyStage_NormalizingMedia = "VideoCompositionAssemblyStage_NormalizingMedia";
			public const string VideoCompositionAssemblyStage_Queued = "VideoCompositionAssemblyStage_Queued";
			public const string VideoCompositionAssemblyStage_RenderingLabels = "VideoCompositionAssemblyStage_RenderingLabels";
			public const string VideoCompositionAssemblyStage_UploadingThumbnail = "VideoCompositionAssemblyStage_UploadingThumbnail";
			public const string VideoCompositionAssemblyStage_UploadingToAzure = "VideoCompositionAssemblyStage_UploadingToAzure";
			public const string VideoCompositionAssemblyStage_UploadingToVimeo = "VideoCompositionAssemblyStage_UploadingToVimeo";
			public const string VideoCompositionBlock_BackgroundMediaResource = "VideoCompositionBlock_BackgroundMediaResource";
			public const string VideoCompositionBlock_Description = "VideoCompositionBlock_Description";
			public const string VideoCompositionBlock_DurationSeconds = "VideoCompositionBlock_DurationSeconds";
			public const string VideoCompositionBlock_FadeInSeconds = "VideoCompositionBlock_FadeInSeconds";
			public const string VideoCompositionBlock_FadeOutSeconds = "VideoCompositionBlock_FadeOutSeconds";
			public const string VideoCompositionBlock_Help = "VideoCompositionBlock_Help";
			public const string VideoCompositionBlock_Key = "VideoCompositionBlock_Key";
			public const string VideoCompositionBlock_Labels = "VideoCompositionBlock_Labels";
			public const string VideoCompositionBlock_MediaResource = "VideoCompositionBlock_MediaResource";
			public const string VideoCompositionBlock_MediaResourceFileName = "VideoCompositionBlock_MediaResourceFileName";
			public const string VideoCompositionBlock_MediaResourceMimeType = "VideoCompositionBlock_MediaResourceMimeType";
			public const string VideoCompositionBlock_PresenterPositionX = "VideoCompositionBlock_PresenterPositionX";
			public const string VideoCompositionBlock_PresenterPositionY = "VideoCompositionBlock_PresenterPositionY";
			public const string VideoCompositionBlock_PresenterScale = "VideoCompositionBlock_PresenterScale";
			public const string VideoCompositionBlock_SortOrder = "VideoCompositionBlock_SortOrder";
			public const string VideoCompositionBlock_Title = "VideoCompositionBlock_Title";
			public const string VideoCompositionBlock_Type = "VideoCompositionBlock_Type";
			public const string VideoCompositionBlockType_Image = "VideoCompositionBlockType_Image";
			public const string VideoCompositionBlockType_Video = "VideoCompositionBlockType_Video";
			public const string VideoCompositions_Title = "VideoCompositions_Title";
			public const string VideoCompositionStatus_Assembling = "VideoCompositionStatus_Assembling";
			public const string VideoCompositionStatus_Cancelled = "VideoCompositionStatus_Cancelled";
			public const string VideoCompositionStatus_Completed = "VideoCompositionStatus_Completed";
			public const string VideoCompositionStatus_Draft = "VideoCompositionStatus_Draft";
			public const string VideoCompositionStatus_Failed = "VideoCompositionStatus_Failed";
			public const string VideoCompositionStatus_Preparing = "VideoCompositionStatus_Preparing";
			public const string VideoCompositionStatus_ProcessingAtVimeo = "VideoCompositionStatus_ProcessingAtVimeo";
			public const string VideoCompositionStatus_Queued = "VideoCompositionStatus_Queued";
			public const string VideoCompositionStatus_Uploading = "VideoCompositionStatus_Uploading";
			public const string VideoCompositionTextAlignment_Center = "VideoCompositionTextAlignment_Center";
			public const string VideoCompositionTextAlignment_Left = "VideoCompositionTextAlignment_Left";
			public const string VideoCompositionTextAlignment_Right = "VideoCompositionTextAlignment_Right";
			public const string VideoCompositionTextLabel_Alignment = "VideoCompositionTextLabel_Alignment";
			public const string VideoCompositionTextLabel_Bold = "VideoCompositionTextLabel_Bold";
			public const string VideoCompositionTextLabel_Color = "VideoCompositionTextLabel_Color";
			public const string VideoCompositionTextLabel_DelaySeconds = "VideoCompositionTextLabel_DelaySeconds";
			public const string VideoCompositionTextLabel_Description = "VideoCompositionTextLabel_Description";
			public const string VideoCompositionTextLabel_FadeInSeconds = "VideoCompositionTextLabel_FadeInSeconds";
			public const string VideoCompositionTextLabel_FadeOutSeconds = "VideoCompositionTextLabel_FadeOutSeconds";
			public const string VideoCompositionTextLabel_FontSize = "VideoCompositionTextLabel_FontSize";
			public const string VideoCompositionTextLabel_Help = "VideoCompositionTextLabel_Help";
			public const string VideoCompositionTextLabel_MaxWidth = "VideoCompositionTextLabel_MaxWidth";
			public const string VideoCompositionTextLabel_Text = "VideoCompositionTextLabel_Text";
			public const string VideoCompositionTextLabel_Title = "VideoCompositionTextLabel_Title";
			public const string VideoCompositionTextLabel_VisibleDurationSeconds = "VideoCompositionTextLabel_VisibleDurationSeconds";
			public const string VideoCompositionTextLabel_X = "VideoCompositionTextLabel_X";
			public const string VideoCompositionTextLabel_Y = "VideoCompositionTextLabel_Y";
			public const string VideoProduction_BackgroundMediaResource = "VideoProduction_BackgroundMediaResource";
			public const string VideoProduction_Description = "VideoProduction_Description";
			public const string VideoProduction_ErrorMessage = "VideoProduction_ErrorMessage";
			public const string VideoProduction_FinalVideoMediaResource = "VideoProduction_FinalVideoMediaResource";
			public const string VideoProduction_Help = "VideoProduction_Help";
			public const string VideoProduction_LanguageCode = "VideoProduction_LanguageCode";
			public const string VideoProduction_Locale = "VideoProduction_Locale";
			public const string VideoProduction_PreviewAudioMediaResource = "VideoProduction_PreviewAudioMediaResource";
			public const string VideoProduction_Provider = "VideoProduction_Provider";
			public const string VideoProduction_Script = "VideoProduction_Script";
			public const string VideoProduction_Status = "VideoProduction_Status";
			public const string VideoProduction_TargetEntityId = "VideoProduction_TargetEntityId";
			public const string VideoProduction_TargetEntityName = "VideoProduction_TargetEntityName";
			public const string VideoProduction_TargetEntityProperty = "VideoProduction_TargetEntityProperty";
			public const string VideoProduction_TargetEntityType = "VideoProduction_TargetEntityType";
			public const string VideoProduction_Title = "VideoProduction_Title";
			public const string VideoProduction_VideoAvatar = "VideoProduction_VideoAvatar";
			public const string VideoProduction_VideoName = "VideoProduction_VideoName";
			public const string VideoProduction_VoiceId = "VideoProduction_VoiceId";
			public const string VideoProduction_VoiceName = "VideoProduction_VoiceName";
			public const string VideoProductionAspectRatio_Auto = "VideoProductionAspectRatio_Auto";
			public const string VideoProductionAspectRatio_Landscape16x9 = "VideoProductionAspectRatio_Landscape16x9";
			public const string VideoProductionAspectRatio_Landscape5x4 = "VideoProductionAspectRatio_Landscape5x4";
			public const string VideoProductionAspectRatio_Portrait4x5 = "VideoProductionAspectRatio_Portrait4x5";
			public const string VideoProductionAspectRatio_Portrait9x16 = "VideoProductionAspectRatio_Portrait9x16";
			public const string VideoProductionAspectRatio_Square1x1 = "VideoProductionAspectRatio_Square1x1";
			public const string VideoProductionEngine_AvatarIII = "VideoProductionEngine_AvatarIII";
			public const string VideoProductionEngine_AvatarIV = "VideoProductionEngine_AvatarIV";
			public const string VideoProductionEngine_AvatarV = "VideoProductionEngine_AvatarV";
			public const string VideoProductionExpressiveness_High = "VideoProductionExpressiveness_High";
			public const string VideoProductionExpressiveness_Low = "VideoProductionExpressiveness_Low";
			public const string VideoProductionExpressiveness_Medium = "VideoProductionExpressiveness_Medium";
			public const string VideoProductionFit_Automatic = "VideoProductionFit_Automatic";
			public const string VideoProductionFit_Contain = "VideoProductionFit_Contain";
			public const string VideoProductionFit_Cover = "VideoProductionFit_Cover";
			public const string VideoProductionProvider_HeyGen = "VideoProductionProvider_HeyGen";
			public const string VideoProductionProvider_QualityPremium = "VideoProductionProvider_QualityPremium";
			public const string VideoProductionProvider_QualityStandard = "VideoProductionProvider_QualityStandard";
			public const string VideoProductionResolution_FullHD1080 = "VideoProductionResolution_FullHD1080";
			public const string VideoProductionResolution_HD720 = "VideoProductionResolution_HD720";
			public const string VideoProductionResolution_UHD4K = "VideoProductionResolution_UHD4K";
			public const string VideoProductionStatus_Cancelled = "VideoProductionStatus_Cancelled";
			public const string VideoProductionStatus_Completed = "VideoProductionStatus_Completed";
			public const string VideoProductionStatus_Draft = "VideoProductionStatus_Draft";
			public const string VideoProductionStatus_Failed = "VideoProductionStatus_Failed";
			public const string VideoProductionStatus_GeneratingPreviewAudio = "VideoProductionStatus_GeneratingPreviewAudio";
			public const string VideoProductionStatus_ImportingProviderVideo = "VideoProductionStatus_ImportingProviderVideo";
			public const string VideoProductionStatus_ImportingToVimeo = "VideoProductionStatus_ImportingToVimeo";
			public const string VideoProductionStatus_PreparingAvatar = "VideoProductionStatus_PreparingAvatar";
			public const string VideoProductionStatus_PreviewAudioReady = "VideoProductionStatus_PreviewAudioReady";
			public const string VideoProductionStatus_ProcessingAtVimeo = "VideoProductionStatus_ProcessingAtVimeo";
			public const string VideoProductionStatus_ProviderCompleted = "VideoProductionStatus_ProviderCompleted";
			public const string VideoProductionStatus_ProviderVideoReady = "VideoProductionStatus_ProviderVideoReady";
			public const string VideoProductionStatus_Rendering = "VideoProductionStatus_Rendering";
			public const string VideoProductionStatus_Submitted = "VideoProductionStatus_Submitted";
			public const string VideoProductionStatus_Submitting = "VideoProductionStatus_Submitting";
			public const string VideoProductionStatus_UpdatingEntity = "VideoProductionStatus_UpdatingEntity";
			public const string VideoProductionStatus_UploadingBackground = "VideoProductionStatus_UploadingBackground";
			public const string VideoProductionStatus_WaitingForAvatar = "VideoProductionStatus_WaitingForAvatar";
		}
	}
}

