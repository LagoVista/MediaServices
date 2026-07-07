using LagoVista.Core.Models;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models.Icons
{
    public enum LagoVistaIconBackground
    {
        Transparent,
        Opaque,
        Auto
    }

    public class LagoVistaIconGenerationRequest
    {
        public const string SystemOrgNamespace = "system";

        public LagoVistaIconGenerationRequest()
        {
            Version = "1.0";
            RequestType = "semantic-entity-icon";
            StyleProfileKey = LagoVistaIconStyleProfile.NuvOsSemanticIconKey;
            PublishedVersion = 1;
            SourceEntity = new LagoVistaIconSourceEntity();
            Keywords = new List<string>();
            SuggestedMetaphors = new List<string>();
            AvoidMetaphors = new List<string>();
        }



        public string Version { get; set; }
        public string RequestType { get; set; }
        public string OrgNamespace { get; set; }
        public string IconKey { get; set; }
        public string StyleProfileKey { get; set; }
        public int PublishedVersion { get; set; }
        public string Meaning { get; set; }
        public string PreferredAccentColor { get; set; }
        public string AdditionalGuidance { get; set; }
        public string CdnBaseUrl { get; set; }
        public LagoVistaIconSourceEntity SourceEntity { get; set; }

        public LagoVistaIconSourceInstance SourceInstance { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> SuggestedMetaphors { get; set; }
        public List<string> AvoidMetaphors { get; set; }
        public LagoVistaIconBackground Background { get; set; } = LagoVistaIconBackground.Transparent;
    }

    public class LagoVistaIconSourceEntity
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Key { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string UserHelp { get; set; }

        public string Purpose { get; set; }

        public string PurposeSummary { get; set; }

        public string ShortCode { get; set; }

        public string DomainKey { get; set; }

        public string EntityKey { get; set; }

        public string ClusterKey { get; set; }

        public string AiIconGuidance { get; set; }
    }

    public class LagoVistaIconSourceInstance
    {
        public string Id { get; set; }

        public string Key { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string Purpose { get; set; }

        public string PurposeSummary { get; set; }

        public string ShortCode { get; set; }

        public List<string> Keywords { get; set; } = new List<string>();
    }


    public class LagoVistaIconStyleProfile
    {
        public const string NuvOsSemanticIconKey = "nuvos-semantic-icon";

        public LagoVistaIconStyleProfile()
        {
            PreferredDisplaySizes = new List<int>();
            PublishedSizes = new List<int>();
            AllowedColors = new List<string>();
            AccentColors = new List<string>();
        }

        public string Key { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public int MinimumSupportedSize { get; set; }
        public List<int> PreferredDisplaySizes { get; set; }
        public List<int> PublishedSizes { get; set; }
        public int GeneratedMasterSize { get; set; }
        public string Background { get; set; }
        public string RenderStyle { get; set; }
        public int MaxColors { get; set; }
        public string DominantColor { get; set; }
        public List<string> AllowedColors { get; set; }
        public List<string> AccentColors { get; set; }
        public bool AllowGradients { get; set; }
        public bool AllowGlow { get; set; }
        public bool AllowShadow { get; set; }
        public bool AllowScene { get; set; }
        public bool AllowText { get; set; }
        public bool AllowLogos { get; set; }
        public bool AllowPhotorealism { get; set; }
        public bool Allow3D { get; set; }
    }

    public class LagoVistaIconGenerationManifest
    {
        public LagoVistaIconGenerationManifest()
        {
            Version = "1.0";
            Catalog = new LagoVistaIconManifestCatalogInfo();
            SourceEntity = new LagoVistaIconSourceEntity();
            Generation = new LagoVistaIconGenerationInfo();
            Assets = new LagoVistaIconAssetManifest();
        }

        public string Version { get; set; }
        public string IconKey { get; set; }
        public string DisplayName { get; set; }
        public LagoVistaIconManifestCatalogInfo Catalog { get; set; }
        public LagoVistaIconSourceEntity SourceEntity { get; set; }
        public LagoVistaIconGenerationInfo Generation { get; set; }
        public LagoVistaIconAssetManifest Assets { get; set; }
    }

    public class LagoVistaIconPublicManifest
    {
        public LagoVistaIconPublicManifest()
        {
            Version = "1.0";
            Assets = new LagoVistaIconAssetManifest();
        }

        public string Version { get; set; }
        public string IconKey { get; set; }
        public string DisplayName { get; set; }
        public string SourceEntityType { get; set; }
        public string SourceEntityId { get; set; }
        public string SourceEntityKey { get; set; }
        public string FamilyKey { get; set; }
        public string FamilyVersion { get; set; }
        public int CurrentVersion { get; set; }
        public int PreferredSize { get; set; }
        public int MinimumSupportedSize { get; set; }
        public LagoVistaIconAssetManifest Assets { get; set; }
    }

    public class LagoVistaIconManifestCatalogInfo
    {
        public LagoVistaIconManifestCatalogInfo()
        {
            PreferredDisplaySizes = new List<int>();
            PublishedSizes = new List<int>();
        }

        public string FamilyKey { get; set; }
        public string FamilyVersion { get; set; }
        public int MinimumSupportedSize { get; set; }
        public List<int> PreferredDisplaySizes { get; set; }
        public List<int> PublishedSizes { get; set; }
    }

    public class LagoVistaIconGenerationInfo
    {
        public string Provider { get; set; }
        public string ProviderResponseId { get; set; }
        public string Model { get; set; }
        public string GeneratedUtc { get; set; }
        public string Prompt { get; set; }
        public string RevisedPrompt { get; set; }
        public LagoVistaIconGenerationUsage Usage { get; set; }
        public LagoVistaIconGenerationRequest Request { get; set; }
    }

    public class LagoVistaIconAssetManifest
    {
        public LagoVistaIconAssetManifest()
        {
            Webp = new Dictionary<string, string>();
        }

        public string Source { get; set; }
        public Dictionary<string, string> Webp { get; set; }
    }

    public class LagoVistaIconCatalogDocument
    {
        public LagoVistaIconCatalogDocument()
        {
            Version = "1.0";
            SourceEntityTypes = new List<LagoVistaIconSourceEntityTypeHeader>();
            Icons = new List<LagoVistaIconCatalogEntry>();
        }

        public string Version { get; set; }
        public string OrgNamespace { get; set; }
        public string FamilyKey { get; set; }
        public string FamilyVersion { get; set; }
        public string LastUpdatedUtc { get; set; }
        public List<LagoVistaIconSourceEntityTypeHeader> SourceEntityTypes { get; set; }
        public List<LagoVistaIconCatalogEntry> Icons { get; set; }
    }

    public class LagoVistaIconSourceEntityTypeHeader
    {
        public LagoVistaIconSourceEntityTypeHeader()
        {
            Tags = new List<string>();
            SuggestedMetaphors = new List<string>();
            AvoidMetaphors = new List<string>();
        }

        public string SourceEntityType { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string DefaultIconKey { get; set; }
        public string DefaultPreviewUrl { get; set; }
        public string DefaultManifestUrl { get; set; }
        public string DefaultPromptGuidance { get; set; }
        public int IconCount { get; set; }
        public List<string> Tags { get; set; }
        public List<string> SuggestedMetaphors { get; set; }
        public List<string> AvoidMetaphors { get; set; }
    }

    public class LagoVistaIconCatalogEntry
    {
        public LagoVistaIconCatalogEntry()
        {
            Tags = new List<string>();
            Assets = new Dictionary<string, string>();
        }

        public string IconKey { get; set; }
        public string DisplayName { get; set; }
        public string SourceEntityType { get; set; }
        public string SourceEntityId { get; set; }
        public string SourceEntityKey { get; set; }
        public string FamilyKey { get; set; }
        public int CurrentVersion { get; set; }
        public string Status { get; set; }
        public int PreferredSize { get; set; }
        public int MinimumSupportedSize { get; set; }
        public string PreviewUrl { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, string> Assets { get; set; }
        public string ManifestUrl { get; set; }
    }

    public class LagoVistaIconMasterCatalogDocument
    {
        public LagoVistaIconMasterCatalogDocument()
        {
            Version = "1.0";
            SourceEntityTypes = new List<LagoVistaIconSourceEntityTypeHeader>();
            Icons = new List<LagoVistaIconMasterCatalogEntry>();
        }

        public string Version { get; set; }
        public string OrgNamespace { get; set; }
        public string LastUpdatedUtc { get; set; }
        public List<LagoVistaIconSourceEntityTypeHeader> SourceEntityTypes { get; set; }
        public List<LagoVistaIconMasterCatalogEntry> Icons { get; set; }
    }

    public class LagoVistaIconMasterCatalogEntry
    {
        public LagoVistaIconMasterCatalogEntry()
        {
            Tags = new List<string>();
            SuggestedMetaphors = new List<string>();
            AvoidMetaphors = new List<string>();
            Assets = new Dictionary<string, string>();
        }

        public string IconKey { get; set; }
        public string DisplayName { get; set; }
        public string SourceEntityType { get; set; }
        public string SourceEntityId { get; set; }
        public string SourceEntityKey { get; set; }
        public string FamilyKey { get; set; }
        public string FamilyVersion { get; set; }
        public int CurrentVersion { get; set; }
        public string Status { get; set; }
        public int PreferredSize { get; set; }
        public int MinimumSupportedSize { get; set; }
        public string Meaning { get; set; }
        public string AdditionalGuidance { get; set; }
        public string PreviewUrl { get; set; }
        public string ManifestUrl { get; set; }
        public string SearchText { get; set; }
        public List<string> Tags { get; set; }
        public List<string> SuggestedMetaphors { get; set; }
        public List<string> AvoidMetaphors { get; set; }
        public Dictionary<string, string> Assets { get; set; }
    }

    public class LagoVistaIconPublishResult
    {
        public LagoVistaIconPublishResult()
        {
            Assets = new Dictionary<string, string>();
        }

        public string IconKey { get; set; }
        public string OrgNamespace { get; set; }
        public string FamilyKey { get; set; }
        public int Version { get; set; }
        public string BaseUrl { get; set; }
        public string SourceUrl { get; set; }
        public string ManifestUrl { get; set; }
        public string GenerationRecordPath { get; set; }
        public string CatalogUrl { get; set; }
        public string MasterCatalogUrl { get; set; }
        public string PublishedUtc { get; set; }
        public Dictionary<string, string> Assets { get; set; }
    }
}
