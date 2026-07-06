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
        private const string IconCatalogContainerName = "lagovistaicons";
        private const string VersionedAssetCacheControl = "public, max-age=31536000, immutable";
        private const string ManifestCacheControl = "public, max-age=300";
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
                    var sourceUploadResult = await storage.AddFileAsync(IconCatalogContainerName, sourcePath, sourceBytes, "image/webp", VersionedAssetCacheControl);
                    if (!sourceUploadResult.Successful)
                        return sourceUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                    result.SourceUrl = ResolvePublicUrl(publishRequest, sourcePath, sourceUploadResult.Result);

                    foreach (var size in profile.PublishedSizes)
                    {
                        var assetPath = $"{versionPath}/icon-{size}.webp";
                        var assetBytes = ResizeWebp(sourceImage, size);
                        var assetUploadResult = await storage.AddFileAsync(IconCatalogContainerName, assetPath, assetBytes, "image/webp", VersionedAssetCacheControl);
                        if (!assetUploadResult.Successful)
                            return assetUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                        result.Assets[size.ToString()] = ResolvePublicUrl(publishRequest, assetPath, assetUploadResult.Result);
                    }
                }

                var manifest = CreateManifest(publishRequest, result);
                var manifestJson = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                var manifestPath = $"{versionPath}/generation.json";
                var manifestUploadResult = await storage.AddFileAsync(IconCatalogContainerName, manifestPath, manifestJson, "application/json", ManifestCacheControl);
                if (!manifestUploadResult.Successful)
                    return manifestUploadResult.ToInvokeResult<LagoVistaIconPublishResult>();

                result.ManifestUrl = ResolvePublicUrl(publishRequest, manifestPath, manifestUploadResult.Result);

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

        private static LagoVistaIconGenerationManifest CreateManifest(LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
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
                Tags = generationRequest.Keywords ?? new List<string>(),
                Assets = new Dictionary<string, string>(publishResult.Assets),
                ManifestUrl = publishResult.ManifestUrl
            });

            catalog.Icons = catalog.Icons.OrderBy(icon => icon.IconKey).ToList();

            var catalogJson = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            return await storage.AddFileAsync(IconCatalogContainerName, catalogPath, catalogJson, "application/json", CatalogCacheControl);
        }

        private async Task<LagoVistaIconCatalogDocument> ReadCatalogAsync(CloudFileStorage storage, string catalogPath, LagoVistaIconAssetPublishRequest publishRequest, LagoVistaIconPublishResult publishResult)
        {
            var existingCatalogResult = await storage.GetFileAsync(IconCatalogContainerName, catalogPath);
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

            var masterCatalogJson = JsonConvert.SerializeObject(masterCatalog, Formatting.Indented);
            return await storage.AddFileAsync(IconCatalogContainerName, masterCatalogPath, masterCatalogJson, "application/json", CatalogCacheControl);
        }

        private async Task<LagoVistaIconMasterCatalogDocument> ReadMasterCatalogAsync(CloudFileStorage storage, string masterCatalogPath, LagoVistaIconPublishResult publishResult)
        {
            var existingCatalogResult = await storage.GetFileAsync(IconCatalogContainerName, masterCatalogPath);
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
                PreviewUrl = previewUrl,
                ManifestUrl = publishResult.ManifestUrl,
                SearchText = BuildSearchText(generationRequest, profile, tags),
                Tags = tags,
                Assets = new Dictionary<string, string>(publishResult.Assets)
            };
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
