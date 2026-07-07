using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class LagoVistaSystemDefaultIconGenerationManager : ILagoVistaSystemDefaultIconGenerationManager
    {
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly ILagoVistaIconGenerationManager _iconGenerationManager;

        public LagoVistaSystemDefaultIconGenerationManager(IEntityTypeResolver entityTypeResolver, ILagoVistaIconGenerationManager iconGenerationManager)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _iconGenerationManager = iconGenerationManager ?? throw new ArgumentNullException(nameof(iconGenerationManager));
        }

        public Task<InvokeResult<LagoVistaIconPublishResult>> GenerateDefaultIconAsync(string entityTypeName, LagoVistaDefaultIconGenerationRequest request, EntityHeader org, EntityHeader user)
        {
            if (String.IsNullOrWhiteSpace(entityTypeName))
                return Task.FromResult(InvokeResult<LagoVistaIconPublishResult>.FromError("Entity type name is required."));

            if (!_entityTypeResolver.TryGetEntityType(entityTypeName, out var modelType))
                return Task.FromResult(InvokeResult<LagoVistaIconPublishResult>.FromError($"Could not resolve entity type '{entityTypeName}'."));

            var entitySummaryResult = CreateEntitySummary(modelType);
            if (!entitySummaryResult.Successful)
                return Task.FromResult(entitySummaryResult.ToInvokeResult<LagoVistaIconPublishResult>());

            var generationRequest = CreateGenerationRequest(modelType, entitySummaryResult.Result, request);
            return _iconGenerationManager.GenerateAsync(generationRequest, org, user);
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

        private static LagoVistaIconGenerationRequest CreateGenerationRequest(Type modelType, EntitySummary entitySummary, LagoVistaDefaultIconGenerationRequest request)
        {
            var displayName = ResolveDisplayName(modelType, entitySummary);
            var entityTypeKey = ToKebabCase(modelType.Name);

            return new LagoVistaIconGenerationRequest
            {
                OrgNamespace = LagoVistaIconGenerationRequest.SystemOrgNamespace,
                IconKey = $"{entityTypeKey}-default",
                PublishedVersion = 1,
                Meaning = $"Default semantic icon for the {displayName} entity type. This icon represents the entity type broadly, not a specific record.",
                AdditionalGuidance = request?.AdditionalGuidance,
                SourceEntity = new LagoVistaIconSourceEntity
                {
                    Id = modelType.Name,
                    Type = modelType.Name,
                    Key = ResolveEntityKey(modelType, entitySummary),
                    DisplayName = displayName,
                    Description = entitySummary.Description,
                    UserHelp = entitySummary.UserHelp,
                    DomainKey = entitySummary.DomainKey,
                    EntityKey = entitySummary.EntityKey,
                    ClusterKey = entitySummary.ClusterKey,
                    AiIconGuidance = entitySummary.AiIconGuidance
                },
                Keywords = BuildKeywords(modelType, entitySummary, displayName),
                SuggestedMetaphors = new List<string>(),
                AvoidMetaphors = new List<string>
                {
                    "record-specific status badge",
                    "money or pricing metaphor unless intrinsic to the entity type",
                    "growth chart unless intrinsic to the entity type",
                    "workflow arrows unless intrinsic to the entity type",
                    "dashboard or analytics screen",
                    "robot or artificial intelligence mascot"
                }
            };
        }

        private static List<string> BuildKeywords(Type modelType, EntitySummary entitySummary, string displayName)
        {
            var keywords = new List<string>
            {
                modelType.Name,
                displayName,
                entitySummary.Name,
                entitySummary.ShortClassName,
                entitySummary.EntityKey,
                entitySummary.ClusterKey,
                entitySummary.DomainKey
            };

            keywords.AddRange(SplitWords(modelType.Name));
            keywords.AddRange(SplitWords(displayName));
            keywords.AddRange(SplitWords(entitySummary.Name));
            keywords.AddRange(SplitWords(entitySummary.EntityKey));
            keywords.AddRange(SplitWords(entitySummary.ClusterKey));
            keywords.AddRange(SplitWords(entitySummary.DomainKey));

            return keywords.Where(keyword => !String.IsNullOrWhiteSpace(keyword)).Select(keyword => keyword.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string ResolveDisplayName(Type modelType, EntitySummary entitySummary)
        {
            if (!String.IsNullOrWhiteSpace(entitySummary.Name))
                return entitySummary.Name.Trim();

            if (!String.IsNullOrWhiteSpace(entitySummary.ShortClassName))
                return SplitPascalCase(entitySummary.ShortClassName);

            return SplitPascalCase(modelType.Name);
        }

        private static string ResolveEntityKey(Type modelType, EntitySummary entitySummary)
        {
            if (!String.IsNullOrWhiteSpace(entitySummary.EntityKey))
                return entitySummary.EntityKey.Trim();

            return ToKebabCase(modelType.Name);
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