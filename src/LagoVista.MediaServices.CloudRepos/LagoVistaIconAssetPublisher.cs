using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class LagoVistaIconAssetPublisher : ILagoVistaIconAssetPublisher
    {
        private const string PublicIconContainerName = "lagovistaicons";
        private const string PrivateGenerationContainerName = "lagovistaicongeneration";
        private const string VersionedAssetCacheControl = "public, max-age=31536000, immutable";
        private const string PublicManifestCacheControl = "public, max-age=300";
        private const string PrivateGenerationCacheControl = "private, max-age=0, no-cache";
        private const string CatalogCacheControl = "public, max-age=60";

        private readonly IMediaServicesConnectionSettings _settings;
        private readonly IAdminLogger _adminLogger;

        public LagoVistaIconAssetPublisher(IMediaServicesConnectionSettings settings, IAdminLogger adminLogger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
        }

        public async Task<InvokeResult<LagoVistaIconPublishResult>> PublishAsync(LagoVistaIconAssetPublishRequest publishRequest)
        {
            var validationResult = ValidatePublishRequest(publishRequest);
            if (!validationResult.Successful)
                return validationResult.ToInvokeResult<LagoVistaIconPublishResult>();

            try
            {
                var generationRequest = publishRequest.GenerationRequest;
                var profile = publishRequest.StyleProfile;
                var orgNamespace = NormalizePathSegment(generationRequest.OrgNamespace);
                var familyKey = NormalizePathSegment(profile.Key);
                var iconKey = NormalizePathSegment(generationRequest.IconKey);
                var version = generationRequest.PublishedVersion <= 0 ? 1 : generationRequest.PublishedVersion;
                var versionPath = $"{orgNamespace}/{familyKey}/{iconKey}/v{version}";
                var catalogPath = $"{orgNamespace}/catalog/{familyKey}.json";
                var masterCatalogPath = $"{orgNamespace}/catalog/master.json";
                var storage = CreateStorage();

                var result = new LagoVistaIconPublishResult
                {
                    IconKey = iconKey,
                    OrgNamespace = orgNamespace,
                    FamilyKey = familyKey,
                    Version = version,
                    BaseUrl = ResolvePublicUrl(publishRequest, versionPath),
                    PublishedUtc = DateTime.UtcNow.ToString("o")
                };

                using (var sourceImage = Image.Load(publishRequest.SourceImageData))
                {
                    var sourceBytes = EncodeWebp(sourceImage);
                    var sourcePath = $"{versionPath}/source.webp";
                    var sourceUploadResult = await storage.AddFileAsync(PublicIconContainerName, sourcePath, sourceBytes, "image/webp", VersionedAssetCacheControl);
                    if (!sourceUploadResult.Successful)
                        return sourceUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                    result.SourceUrl = ResolvePublicUrl(publishRequest, sourcePath, sourceUploadResult.Result);

                    foreach (var size in profile.PublishedSizes)
                    {
                        var assetPath = $"{versionPath}/icon-{size}.webp";
                        var assetBytes = ResizeWebp(sourceImage, size);
                        var assetUploadResult = await storage.AddFileAsync(PublicIconContainerName, assetPath, assetBytes, "image/webp", VersionedAssetCacheControl);
                        if (!assetUploadResult.Successful)
                            return assetUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                        result.Assets[size.ToString()] = ResolvePublicUrl(publishRequest, assetPath, assetUploadResult.Result);
                    }
                }

                var publicManifest = CreatePublicManifest(publishRequest, result);
                var publicManifestJson = JsonConvert.SerializeObject(publicManifest, Formatting.Indented);
                var publicManifestPath = $"{versionPath}/manifest.json";
                var publicManifestUploadResult = await storage.AddFileAsync(PublicIconContainerName, publicManifestPath, publicManifestJson, "application/json", PublicManifestCacheControl);
                if (!publicManifestUploadResult.Successful)
                    return publicManifestUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                result.ManifestUrl = ResolvePublicUrl(publishRequest, publicManifestPath, publicManifestUploadResult.Result);

                var generationManifest = CreateGenerationManifest(publishRequest, result);
                var generationManifestJson = JsonConvert.SerializeObject(generationManifest, Formatting.Indented);
                var generationManifestPath = $"{versionPath}/generation.json";
                var generationManifestUploadResult = await storage.AddFileAsync(PrivateGenerationContainerName, generationManifestPath, generationManifestJson, "application/json", PrivateGenerationCacheControl);
                if (!generationManifestUploadResult.Successful)
                    return generationManifestUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                result.GenerationRecordPath = generationManifestPath;

                var catalogResult = await UpdateCatalogAsync(storage, catalogPath, publishRequest, result);
                if (!catalogResult.Successful)
                    return catalogResult.ToInvokeResult<LagoVistaIconPublishResult>();

                result.CatalogUrl = ResolvePublicUrl(publishRequest, catalogPath, catalogResult.Result);

                var masterCatalogResult = await UpdateMasterCatalogAsync(storage, masterCatalogPath, publishRequest, result);
                if (!masterCatalogResult.Successful)
                    return masterCatalogResult.ToInvokeResult<LagoVistaIconPublishResult>();

                result.MasterCatalogUrl = ResolvePublicUrl(publishRequest, masterCatalogPath, masterCatalogResult.Result);

                return InvokeResult<LagoVistaIconPublishResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<LagoVistaIconPublishResult>.FromException("Could not publish LagoVista icon assets.", ex);
            }
        }

        private static InvokeResult ValidatePublishRequest(LagoVistaIconAssetPublishRequest publishRequest)
        {
            if (publishRequest == null)
                return InvokeResult.FromError("LagoVista icon publish request is required.");

            if (publishRequest.GenerationRequest == null)
                return InvokeResult.FromError("LagoVista icon generation request is required.");

            if (publishRequest.StyleProfile == null)
                return InvokeResult.FromError("LagoVista icon style profile is required.");

            if (String.IsNullOrWhiteSpace(publishRequest.GenerationRequest.OrgNamespace))
                return InvokeResult.FromError("Organization namespace is required.");

            if (String.IsNullOrWhiteSpace(publishRequest.GenerationRequest.IconKey))
                return InvokeResult.FromError("Icon key is required.");

            if (String.IsNullOrWhiteSpace(publishRequest.StyleProfile.Key))
                return InvokeResult.FromError("Icon style profile key is required.");

            if (publishRequest.StyleProfile.PublishedSizes == null || publishRequest.StyleProfile.PublishedSizes.Count == 0)
                return InvokeResult.FromError("Icon style profile must include published sizes.");

            if (publishRequest.SourceImageData == null || publishRequest.SourceImageData.Length == 0)
                return InvokeResult.FromError("Source image data is required.");

            return InvokeResult.Success;
        }

        private CloudFileStorage CreateStorage()
        {
            if (_settings.MediaStorageConnection == null)
                throw new InvalidOperationException("Media storage connection is required.");

            if (String.IsNullOrWhiteSpace(_settings.MediaStorageConnection.AccountId))
                throw new InvalidOperationException("Media storage account id is required.");

            if (String.IsNullOrWhiteSpace(_settings.MediaStorageConnection.AccessKey))
                throw new InvalidOperationException("Media storage access key is required.");

            return new CloudFileStorage(_settings.MediaStorageConnection.AccountId, _settings.MediaStorageConnection.AccessKey, _adminLogger);
        }

        private static byte[] ResizeWebp(Image sourceImage, int size)
        {
            using (var resizedImage = sourceImage.Clone(ctx => ctx.Resize(size, size)))
            {
                return EncodeWebp(resizedImage);
            }
        }

        private static byte[] EncodeWebp(Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, new WebpEncoder());
                return stream.ToArray();
            }
        }

        private static LagoVistaIconPublicManifest CreatePublicManifest(LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var generationRequest = publishRequest.GenerationRequest;
            var profile = publishRequest.StyleProfile;
            var manifest = new LagoVistaIconPublicManifest
            {
                IconKey = publishResult.IconKey,
                DisplayName = generationRequest.SourceEntity?.DisplayName,
                SourceEntityType = generationRequest.SourceEntity?.Type,
                SourceEntityId = generationRequest.SourceEntity?.Id,
                SourceEntityKey = generationRequest.IconKey,
                FamilyKey = profile.Key,
                FamilyVersion = profile.Version,
                CurrentVersion = publishResult.Version,
                PreferredSize = profile.PreferredDisplaySizes?.FirstOrDefault() ?? profile.MinimumSupportedSize,
                MinimumSupportedSize = profile.MinimumSupportedSize
            };

            manifest.Assets.Source = "source.webp";
            foreach (var size in profile.PublishedSizes)
                manifest.Assets.Webp[size.ToString()] = $"icon-{size}.webp";

            return manifest;
        }

        private static LagoVistaIconGenerationManifest CreateGenerationManifest(LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var generationRequest = publishRequest.GenerationRequest;
            var profile = publishRequest.StyleProfile;

            var manifest = new LagoVistaIconGenerationManifest
            {
                IconKey = publishResult.IconKey,
                DisplayName = generationRequest.SourceEntity?.DisplayName,
                SourceEntity = generationRequest.SourceEntity,
                Catalog = new LagoVistaIconManifestCatalogInfo
                {
                    FamilyKey = profile.Key,
                    FamilyVersion = profile.Version,
                    MinimumSupportedSize = profile.MinimumSupportedSize,
                    PreferredDisplaySizes = profile.PreferredDisplaySizes,
                    PublishedSizes = profile.PublishedSizes
                },
                Generation = new LagoVistaIconGenerationInfo
                {
                    Provider = publishRequest.Provider,
                    ProviderResponseId = publishRequest.ProviderResponseId,
                    Model = publishRequest.Model,
                    GeneratedUtc = String.IsNullOrWhiteSpace(publishRequest.GeneratedUtc) ? DateTime.UtcNow.ToString("o") : publishRequest.GeneratedUtc,
                    Prompt = publishRequest.Prompt,
                    RevisedPrompt = publishRequest.RevisedPrompt,
                    Usage = publishRequest.Usage,
                    Request = generationRequest
                }
            };

            manifest.Assets.Source = "source.webp";
            foreach (var size in profile.PublishedSizes)
                manifest.Assets.Webp[size.ToString()] = $"icon-{size}.webp";

            return manifest;
        }

        private async Task<InvokeResult<Uri>> UpdateCatalogAsync(CloudFileStorage storage, string catalogPath, LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var catalog = await ReadCatalogAsync(storage, catalogPath, publishRequest, publishResult);
            var generationRequest = publishRequest.GenerationRequest;
            var profile = publishRequest.StyleProfile;
            var previewUrl = ResolvePreviewUrl(publishResult);

            var existing = catalog.Icons.FirstOrDefault(icon => String.Equals(icon.IconKey, publishResult.IconKey, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                catalog.Icons.Remove(existing);

            catalog.LastUpdatedUtc = DateTime.UtcNow.ToString("o");
            catalog.Icons.Add(new LagoVistaIconCatalogEntry
            {
                IconKey = publishResult.IconKey,
                DisplayName = generationRequest.SourceEntity?.DisplayName,
                SourceEntityType = generationRequest.SourceEntity?.Type,
                SourceEntityId = generationRequest.SourceEntity?.Id,
                SourceEntityKey = generationRequest.IconKey,
                FamilyKey = profile.Key,
                CurrentVersion = publishResult.Version,
                Status = "published",
                PreferredSize = profile.PreferredDisplaySizes?.FirstOrDefault() ?? profile.MinimumSupportedSize,
                MinimumSupportedSize = profile.MinimumSupportedSize,
                PreviewUrl = previewUrl,
                Tags = generationRequest.Keywords ?? new List<string>(),
                Assets = new Dictionary<string, string>(publishResult.Assets),
                ManifestUrl = publishResult.ManifestUrl
            });

            catalog.Icons = catalog.Icons.OrderBy(icon => icon.IconKey).ToList();
            catalog.SourceEntityTypes = BuildSourceEntityTypeHeaders(catalog.Icons);

            var catalogJson = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            return await storage.AddFileAsync(PublicIconContainerName, catalogPath, catalogJson, "application/json", CatalogCacheControl);
        }

        private async Task<LagoVistaIconCatalogDocument> ReadCatalogAsync(CloudFileStorage storage, string catalogPath, LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var existingCatalogResult = await storage.GetFileAsync(PublicIconContainerName, catalogPath);
            if (existingCatalogResult.Successful && existingCatalogResult.Result != null && existingCatalogResult.Result.Length > 0)
            {
                var json = Encoding.UTF8.GetString(existingCatalogResult.Result);
                var existingCatalog = JsonConvert.DeserializeObject<LagoVistaIconCatalogDocument>(json);
                if (existingCatalog != null)
                    return existingCatalog;
            }

            return new LagoVistaIconCatalogDocument
            {
                OrgNamespace = publishResult.OrgNamespace,
                FamilyKey = publishRequest.StyleProfile.Key,
                FamilyVersion = publishRequest.StyleProfile.Version,
                LastUpdatedUtc = DateTime.UtcNow.ToString("o")
            };
        }

        private async Task<InvokeResult<Uri>> UpdateMasterCatalogAsync(CloudFileStorage storage, string masterCatalogPath, LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var masterCatalog = await ReadMasterCatalogAsync(storage, masterCatalogPath, publishResult);
            var existing = masterCatalog.Icons.FirstOrDefault(icon => String.Equals(icon.IconKey, publishResult.IconKey, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                masterCatalog.Icons.Remove(existing);

            masterCatalog.LastUpdatedUtc = DateTime.UtcNow.ToString("o");
            masterCatalog.Icons.Add(CreateMasterCatalogEntry(publishRequest, publishResult));
            masterCatalog.Icons = masterCatalog.Icons.OrderBy(icon => icon.SourceEntityType).ThenBy(icon => icon.DisplayName).ThenBy(icon => icon.IconKey).ToList();
            masterCatalog.SourceEntityTypes = BuildSourceEntityTypeHeaders(masterCatalog.Icons);

            var masterCatalogJson = JsonConvert.SerializeObject(masterCatalog, Formatting.Indented);
            return await storage.AddFileAsync(PublicIconContainerName, masterCatalogPath, masterCatalogJson, "application/json", CatalogCacheControl);
        }

        private async Task<LagoVistaIconMasterCatalogDocument> ReadMasterCatalogAsync(CloudFileStorage storage, string masterCatalogPath, LagoVistaIconPublishResult publishResult)
        {
            var existingCatalogResult = await storage.GetFileAsync(PublicIconContainerName, masterCatalogPath);
            if (existingCatalogResult.Successful && existingCatalogResult.Result != null && existingCatalogResult.Result.Length > 0)
            {
                var json = Encoding.UTF8.GetString(existingCatalogResult.Result);
                var existingCatalog = JsonConvert.DeserializeObject<LagoVistaIconMasterCatalogDocument>(json);
                if (existingCatalog != null)
                    return existingCatalog;
            }

            return new LagoVistaIconMasterCatalogDocument
            {
                OrgNamespace = publishResult.OrgNamespace,
                LastUpdatedUtc = DateTime.UtcNow.ToString("o")
            };
        }

        private static LagoVistaIconMasterCatalogEntry CreateMasterCatalogEntry(LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var generationRequest = publishRequest.GenerationRequest;
            var profile = publishRequest.StyleProfile;
            var previewUrl = ResolvePreviewUrl(publishResult);
            var tags = generationRequest.Keywords ?? new List<string>();

            return new LagoVistaIconMasterCatalogEntry
            {
                IconKey = publishResult.IconKey,
                DisplayName = generationRequest.SourceEntity?.DisplayName,
                SourceEntityType = generationRequest.SourceEntity?.Type,
                SourceEntityId = generationRequest.SourceEntity?.Id,
                SourceEntityKey = generationRequest.IconKey,
                FamilyKey = profile.Key,
                FamilyVersion = profile.Version,
                CurrentVersion = publishResult.Version,
                Status = "published",
                PreferredSize = profile.PreferredDisplaySizes?.FirstOrDefault() ?? profile.MinimumSupportedSize,
                MinimumSupportedSize = profile.MinimumSupportedSize,
                Meaning = generationRequest.Meaning,
                AdditionalGuidance = generationRequest.AdditionalGuidance,
                PreviewUrl = previewUrl,
                ManifestUrl = publishResult.ManifestUrl,
                SearchText = BuildSearchText(generationRequest, profile, tags),
                Tags = tags,
                SuggestedMetaphors = generationRequest.SuggestedMetaphors ?? new List<string>(),
                AvoidMetaphors = generationRequest.AvoidMetaphors ?? new List<string>(),
                Assets = new Dictionary<string, string>(publishResult.Assets)
            };
        }

        private static List<LagoVistaIconSourceEntityTypeHeader> BuildSourceEntityTypeHeaders(IEnumerable<LagoVistaIconCatalogEntry> icons)
        {
            return icons.Where(icon => !String.IsNullOrWhiteSpace(icon.SourceEntityType)).GroupBy(icon => icon.SourceEntityType).OrderBy(group => group.Key).Select(group =>
            {
                var defaultIcon = group.FirstOrDefault(icon => IsDefaultIconForSourceEntityType(icon.IconKey, group.Key));

                return new LagoVistaIconSourceEntityTypeHeader
                {
                    SourceEntityType = group.Key,
                    DisplayName = ToDisplayName(group.Key),
                    DefaultIconKey = defaultIcon?.IconKey,
                    DefaultPreviewUrl = defaultIcon?.PreviewUrl,
                    DefaultManifestUrl = defaultIcon?.ManifestUrl,
                    DefaultPromptGuidance = BuildDefaultPromptGuidance(group.Key, defaultIcon?.DisplayName, null),
                    IconCount = group.Count(),
                    Tags = group.SelectMany(icon => icon.Tags ?? new List<string>()).Where(tag => !String.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag).ToList()
                };
            }).ToList();
        }

        private static List<LagoVistaIconSourceEntityTypeHeader> BuildSourceEntityTypeHeaders(IEnumerable<LagoVistaIconMasterCatalogEntry> icons)
        {
            return icons.Where(icon => !String.IsNullOrWhiteSpace(icon.SourceEntityType)).GroupBy(icon => icon.SourceEntityType).OrderBy(group => group.Key).Select(group =>
            {
                var defaultIcon = group.FirstOrDefault(icon => IsDefaultIconForSourceEntityType(icon.IconKey, group.Key));

                return new LagoVistaIconSourceEntityTypeHeader
                {
                    SourceEntityType = group.Key,
                    DisplayName = ToDisplayName(group.Key),
                    DefaultIconKey = defaultIcon?.IconKey,
                    DefaultPreviewUrl = defaultIcon?.PreviewUrl,
                    DefaultManifestUrl = defaultIcon?.ManifestUrl,
                    DefaultPromptGuidance = BuildDefaultPromptGuidance(group.Key, defaultIcon?.DisplayName, defaultIcon?.AdditionalGuidance),
                    IconCount = group.Count(),
                    Tags = group.SelectMany(icon => icon.Tags ?? new List<string>()).Where(tag => !String.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag).ToList(),
                    SuggestedMetaphors = group.SelectMany(icon => icon.SuggestedMetaphors ?? new List<string>()).Where(metaphor => !String.IsNullOrWhiteSpace(metaphor)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(metaphor => metaphor).ToList(),
                    AvoidMetaphors = group.SelectMany(icon => icon.AvoidMetaphors ?? new List<string>()).Where(metaphor => !String.IsNullOrWhiteSpace(metaphor)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(metaphor => metaphor).ToList()
                };
            }).ToList();
        }

        private static bool IsDefaultIconForSourceEntityType(string iconKey, string sourceEntityType)
        {
            if (String.IsNullOrWhiteSpace(iconKey) || String.IsNullOrWhiteSpace(sourceEntityType))
                return false;

            return String.Equals(iconKey, $"{ToKebabCase(sourceEntityType)}-default", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildDefaultPromptGuidance(string sourceEntityType, string displayName, string additionalGuidance)
        {
            var entityTypeDisplayName = !String.IsNullOrWhiteSpace(displayName) ? displayName : ToDisplayName(sourceEntityType);
            var builder = new StringBuilder();

            builder.Append($"Use the system default {entityTypeDisplayName} icon as the visual baseline for source entity type '{sourceEntityType}'. Keep the same NuvOS semantic icon style, silhouette weight, rounded geometry, color discipline, centered composition, and 32px readability. Adapt the metaphor to the specific record without copying or overlaying the default icon.");

            if (!String.IsNullOrWhiteSpace(additionalGuidance))
                builder.Append($" Default generation guidance: {additionalGuidance.Trim()}");

            return builder.ToString();
        }

        private static string ToKebabCase(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;

            var builder = new StringBuilder();
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];

                if (Char.IsUpper(current))
                {
                    if (index > 0 && builder.Length > 0 && builder[builder.Length - 1] != '-')
                        builder.Append('-');

                    builder.Append(Char.ToLowerInvariant(current));
                }
                else if (Char.IsLetterOrDigit(current))
                {
                    builder.Append(Char.ToLowerInvariant(current));
                }
                else if (current == '-' || current == '_' || Char.IsWhiteSpace(current))
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                        builder.Append('-');
                }
            }

            return builder.ToString().Trim('-');
        }

        private static string ToDisplayName(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;

            var builder = new StringBuilder();
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (index > 0 && Char.IsUpper(current) && !Char.IsWhiteSpace(value[index - 1]))
                    builder.Append(' ');

                builder.Append(current);
            }

            return builder.ToString().Trim();
        }

        private static string ResolvePreviewUrl(LagoVistaIconPublishResult publishResult)
        {
            if (publishResult.Assets == null || publishResult.Assets.Count == 0)
                return null;

            if (publishResult.Assets.ContainsKey("64"))
                return publishResult.Assets["64"];

            if (publishResult.Assets.ContainsKey("48"))
                return publishResult.Assets["48"];

            return publishResult.Assets.OrderBy(asset => asset.Key).First().Value;
        }

        private static string BuildSearchText(LagoVistaIconGenerationRequest generationRequest, LagoVistaIconStyleProfile profile, List<string> tags)
        {
            var values = new List<string>
            {
                generationRequest.IconKey,
                generationRequest.SourceEntity?.DisplayName,
                generationRequest.SourceEntity?.Type,
                generationRequest.SourceEntity?.ShortCode,
                generationRequest.Meaning,
                generationRequest.SourceEntity?.PurposeSummary,
                generationRequest.SourceEntity?.Purpose,
                generationRequest.SourceEntity?.Description,
                profile.Key,
                profile.Name
            };

            if (tags != null)
                values.AddRange(tags);

            if (generationRequest.SuggestedMetaphors != null)
                values.AddRange(generationRequest.SuggestedMetaphors);

            return String.Join(" ", values.Where(value => !String.IsNullOrWhiteSpace(value)).Select(value => value.Trim())).Trim();
        }

        private static string NormalizePathSegment(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;

            var builder = new StringBuilder();
            foreach (var character in value.Trim().ToLowerInvariant())
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                {
                    builder.Append(character);
                }
                else if (character == '-' || character == '_')
                {
                    builder.Append(character);
                }
                else if (character == ' ' || character == '.' || character == ':')
                {
                    builder.Append('-');
                }
            }

            var normalized = builder.ToString().Trim('-');
            while (normalized.Contains("--"))
                normalized = normalized.Replace("--", "-");

            return normalized;
        }

        private static string ResolvePublicUrl(LagoVistaIconAssetPublishRequest publishRequest, string path, Uri storageUri = null)
        {
            if (!String.IsNullOrWhiteSpace(publishRequest.CdnBaseUrl))
                return $"{publishRequest.CdnBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

            return storageUri == null ? path : storageUri.ToString();
        }
    }
}
