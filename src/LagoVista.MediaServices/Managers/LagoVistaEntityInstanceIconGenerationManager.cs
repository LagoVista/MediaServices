using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class LagoVistaEntityInstanceIconGenerationManager : ILagoVistaEntityInstanceIconGenerationManager
    {
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly ILagoVistaIconGenerationManager _iconGenerationManager;
        private readonly IOrganizationLoaderRepo _orgLoader;

        private const string LagoVistaIconPrefix = "lago-icon://";
        private const string LagoVistaIconBaseUrl = "https://lagoicons.blob.core.windows.net/lagovistaicons";
        private const string DefaultIconVersion = "v1";
        private const int ReferenceIconSize = 256;

        private static string ResolveGeneratedIconReferenceUrl(string iconReference)
        {
            if (String.IsNullOrWhiteSpace(iconReference))
                return null;

            var trimmed = iconReference.Trim();

            if (!trimmed.StartsWith(LagoVistaIconPrefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var rawPath = trimmed.Substring(LagoVistaIconPrefix.Length);
            var parts = rawPath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !String.IsNullOrWhiteSpace(part))
                .ToArray();

            if (parts.Length < 3)
                return null;

            var orgNamespace = parts[0];
            var familyKey = parts[1];
            var iconKey = parts[2];
            var version = parts.Length > 3 ? parts[3] : DefaultIconVersion;

            return $"{LagoVistaIconBaseUrl}/{orgNamespace}/{familyKey}/{iconKey}/{version}/icon-{ReferenceIconSize}.webp";
        }

        public LagoVistaEntityInstanceIconGenerationManager(IEntityTypeResolver entityTypeResolver, IOrganizationLoaderRepo orgLoader, ILagoVistaIconGenerationManager iconGenerationManager)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _orgLoader = orgLoader ?? throw new ArgumentNullException(nameof(orgLoader));
            _iconGenerationManager = iconGenerationManager ?? throw new ArgumentNullException(nameof(iconGenerationManager));
        }

        public async Task<InvokeResult<LagoVistaGeneratedInstanceIconResult>> GenerateInstanceIconAsync(LagoVistaGeneratedInstanceIconRequest request, EntityHeader org, EntityHeader user)
        {
            if (request == null)
                return InvokeResult<LagoVistaGeneratedInstanceIconResult>.FromError("Generated instance icon request is required.");

            if (String.IsNullOrWhiteSpace(request.EntityTypeName))
                return InvokeResult<LagoVistaGeneratedInstanceIconResult>.FromError("Entity type name is required.");

            if (String.IsNullOrWhiteSpace(request.EntityId))
                return InvokeResult<LagoVistaGeneratedInstanceIconResult>.FromError("Entity id is required.");

            if (!_entityTypeResolver.TryGetEntityType(request.EntityTypeName, out var modelType))
                return InvokeResult<LagoVistaGeneratedInstanceIconResult>.FromError($"Could not resolve entity type '{request.EntityTypeName}'.");

            var entitySummaryResult = CreateEntitySummary(modelType);
            if (!entitySummaryResult.Successful)
                return entitySummaryResult.ToInvokeResult<LagoVistaGeneratedInstanceIconResult>();

            var generationRequest = await CreateGenerationRequestAsync(modelType, entitySummaryResult.Result, request, org);
            var publishResult = await _iconGenerationManager.GenerateAsync(generationRequest, org, user);

            if (!publishResult.Successful)
                return publishResult.ToInvokeResult<LagoVistaGeneratedInstanceIconResult>();

            return InvokeResult<LagoVistaGeneratedInstanceIconResult>.Create(CreateResult(publishResult.Result));
        }

        private static InvokeResult<EntitySummary> CreateEntitySummary(Type modelType)
        {
            if (modelType == null)
                return InvokeResult<EntitySummary>.FromError("Entity CLR type is required.");

            var attr = modelType.GetTypeInfo().GetCustomAttributes<EntityDescriptionAttribute>().FirstOrDefault();
            if (attr == null)
                return InvokeResult<EntitySummary>.FromError($"Entity type '{modelType.FullName}' does not have an EntityDescriptionAttribute.");

            try
            {
                return InvokeResult<EntitySummary>.Create(EntitySummary.CreateFromAttribute(modelType, attr));
            }
            catch (Exception ex)
            {
                return InvokeResult<EntitySummary>.FromError($"Could not create entity summary for entity type '{modelType.FullName}': {ex.Message}");
            }
        }

        private async Task<LagoVistaIconGenerationRequest> CreateGenerationRequestAsync(Type modelType, EntitySummary entitySummary, LagoVistaGeneratedInstanceIconRequest request, EntityHeader org)
        {
            var entityDisplayName = ResolveEntityDisplayName(modelType, entitySummary);
            var entityTypeKey = ToKebabCase(modelType.Name);
            var iconKey = CreateIconKey(entityTypeKey);
            var instanceDisplayName = ResolveInstanceDisplayName(request);

            var orgNs = await ResolveOrgNamespaceAsync(org);

            return new LagoVistaIconGenerationRequest
            {
                RequestType = "semantic-entity-instance-icon",
                OrgNamespace = orgNs.Result,
                ReferenceImageUrl = ResolveGeneratedIconReferenceUrl(entitySummary.Icon),
                IconKey = iconKey,
                PublishedVersion = 1,
                Meaning = CreateMeaning(entityDisplayName, instanceDisplayName),
                AdditionalGuidance = CreateAdditionalGuidance(request),
                SourceEntity = new LagoVistaIconSourceEntity
                {
                    Id = modelType.Name,
                    Type = modelType.Name,
                    Key = ResolveEntityKey(modelType, entitySummary),
                    DisplayName = entityDisplayName,
                    Description = entitySummary.Description,
                    UserHelp = entitySummary.UserHelp,

                    DomainKey = entitySummary.DomainKey,
                    EntityKey = entitySummary.EntityKey,
                    ClusterKey = entitySummary.ClusterKey,
                    AiIconGuidance = ResolveAiIconGuidance(entitySummary, request)
                },
                SourceInstance = new LagoVistaIconSourceInstance
                {
                    Id = request.EntityId,
                    Key = request.EntityKey,
                    DisplayName = instanceDisplayName,
                    Description = request.EntityDescription,
                    Keywords = BuildInstanceKeywords(request)
                },
                Keywords = BuildKeywords(modelType, entitySummary, request, entityDisplayName, instanceDisplayName),
                SuggestedMetaphors = new List<string>(),
                AvoidMetaphors = BuildAvoidMetaphors()
            };
        }

        private static LagoVistaGeneratedInstanceIconResult CreateResult(LagoVistaIconPublishResult publishResult)
        {
            var previewUrl = ResolvePreviewUrl(publishResult);
            var iconReference = $"lago-icon://{publishResult.OrgNamespace}/{publishResult.FamilyKey}/{publishResult.IconKey}";

            return new LagoVistaGeneratedInstanceIconResult
            {
                IconReference = iconReference,
                IconKey = publishResult.IconKey,
                OrgNamespace = publishResult.OrgNamespace,
                FamilyKey = publishResult.FamilyKey,
                Version = publishResult.Version,
                PreviewUrl = previewUrl,
                SourceUrl = publishResult.SourceUrl,
                ManifestUrl = publishResult.ManifestUrl,
                GenerationRecordPath = publishResult.GenerationRecordPath,
                PublishedUtc = publishResult.PublishedUtc,
                Applied = false,
                Assets = publishResult.Assets ?? new Dictionary<string, string>(),
                PublishResult = publishResult
            };
        }

        private static string ResolvePreviewUrl(LagoVistaIconPublishResult publishResult)
        {
            if (publishResult?.Assets != null && publishResult.Assets.TryGetValue("128", out var preview128))
                return preview128;

            if (publishResult?.Assets != null && publishResult.Assets.TryGetValue("64", out var preview64))
                return preview64;

            return publishResult?.SourceUrl;
        }

        private static string CreateMeaning(string entityDisplayName, string instanceDisplayName)
        {
            return $"Semantic icon for the {instanceDisplayName} {entityDisplayName} instance. This icon should represent the specific instance while remaining recognizable as an icon for the {entityDisplayName} entity type.";
        }

        private static string CreateAdditionalGuidance(LagoVistaGeneratedInstanceIconRequest request)
        {
            var parts = new List<string>();

            if (!String.IsNullOrWhiteSpace(request.CurrentIcon))
                parts.Add($"Current/default icon reference: {request.CurrentIcon.Trim()}");

            if (!String.IsNullOrWhiteSpace(request.AdditionalGuidance))
                parts.Add($"Additional user guidance for this generated icon:\n{request.AdditionalGuidance.Trim()}");

            return parts.Any() ? String.Join("\n\n", parts) : null;
        }

        private static string ResolveAiIconGuidance(EntitySummary entitySummary, LagoVistaGeneratedInstanceIconRequest request)
        {
            if (!String.IsNullOrWhiteSpace(request.BaseIconPrompt))
                return request.BaseIconPrompt.Trim();

            return entitySummary.AiIconGuidance;
        }

        private static string ResolveEntityDisplayName(Type modelType, EntitySummary entitySummary)
        {
            if (!String.IsNullOrWhiteSpace(entitySummary.Name))
                return entitySummary.Name.Trim();

            if (!String.IsNullOrWhiteSpace(entitySummary.ShortClassName))
                return SplitPascalCase(entitySummary.ShortClassName);

            return SplitPascalCase(modelType.Name);
        }

        private static string ResolveInstanceDisplayName(LagoVistaGeneratedInstanceIconRequest request)
        {
            if (!String.IsNullOrWhiteSpace(request.EntityName))
                return request.EntityName.Trim();

            if (!String.IsNullOrWhiteSpace(request.EntityKey))
                return SplitPascalCase(request.EntityKey);

            return "this entity";
        }

        private static string ResolveEntityKey(Type modelType, EntitySummary entitySummary)
        {
            if (!String.IsNullOrWhiteSpace(entitySummary.EntityKey))
                return entitySummary.EntityKey.Trim();

            return ToKebabCase(modelType.Name);
        }

        private async Task<InvokeResult<string>> ResolveOrgNamespaceAsync(EntityHeader orgEh)
        {
            var org = await _orgLoader.GetOrganizationAsync(orgEh.Id);

            if(!String.IsNullOrEmpty(org.IconNamespace))
                return InvokeResult<string>.Create(org.IconNamespace);

            return InvokeResult<string>.Create(org.Namespace);
        }

        private static string CreateIconKey(string entityTypeKey)
        {
            return $"{entityTypeKey}-{CreateUniqueIconSuffix()}";
        }

        private static string CreateUniqueIconSuffix()
        {
            var ticksHex = DateTimeOffset.UtcNow.Ticks.ToString("x16");
            var randomHex = new Random().Next(0, 0x10000).ToString("x4");

            return $"{ticksHex}{randomHex}";
        }

        private static List<string> BuildInstanceKeywords(LagoVistaGeneratedInstanceIconRequest request)
        {
            var keywords = new List<string>();

            keywords.Add(request.EntityName);
            keywords.Add(request.EntityKey);

            keywords.AddRange(SplitWords(request.EntityName));
            keywords.AddRange(SplitWords(request.EntityKey));

            return CleanKeywords(keywords);
        }

        private static List<string> BuildKeywords(Type modelType, EntitySummary entitySummary, LagoVistaGeneratedInstanceIconRequest request, string entityDisplayName, string instanceDisplayName)
        {
            var keywords = new List<string>
            {
                modelType.Name,
                entityDisplayName,
                instanceDisplayName,
                entitySummary.Name,
                entitySummary.ShortClassName,
                entitySummary.EntityKey,
                entitySummary.ClusterKey,
                entitySummary.DomainKey,
                request.EntityName,
                request.EntityKey
            };

            keywords.AddRange(SplitWords(modelType.Name));
            keywords.AddRange(SplitWords(entityDisplayName));
            keywords.AddRange(SplitWords(instanceDisplayName));
            keywords.AddRange(SplitWords(entitySummary.EntityKey));
            keywords.AddRange(SplitWords(entitySummary.ClusterKey));
            keywords.AddRange(SplitWords(entitySummary.DomainKey));

            return CleanKeywords(keywords);
        }

        private static List<string> CleanKeywords(IEnumerable<string> keywords)
        {
            return keywords.Where(keyword => !String.IsNullOrWhiteSpace(keyword)).Select(keyword => keyword.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> BuildAvoidMetaphors()
        {
            return new List<string>
            {
                "record-specific status badge unless explicitly requested",
                "money or pricing metaphor unless intrinsic to the entity type",
                "growth chart unless intrinsic to the entity type",
                "workflow arrows unless intrinsic to the entity type",
                "dashboard or analytics screen",
                "robot or artificial intelligence mascot",
                "generic profile avatar",
                "smiley face",
                "cartoon mascot"
            };
        }

        private static IEnumerable<string> SplitWords(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return SplitPascalCase(value).Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string SplitPascalCase(string value)
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
    }
}