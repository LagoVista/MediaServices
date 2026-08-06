using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Managers;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class EntityVideoCompositionManager : ManagerBase, IEntityVideoCompositionManager
    {
        private readonly IEntityVideoCompositionRepo _repo;
        private readonly IEntityUtilsRepository _entityUtilsRepository;
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IVideoCompositionTemplateManager _templateManager;
        private readonly IVideoCompositionManager _compositionManager;
        private readonly IVideoAvatarManager _videoAvatarManager;

        public EntityVideoCompositionManager(IEntityVideoCompositionRepo repo, IEntityUtilsRepository entityUtilsRepository, IEntityTypeResolver entityTypeResolver, IVideoCompositionTemplateManager templateManager, IVideoCompositionManager compositionManager, IVideoAvatarManager videoAvatarManager, ICoreAppServices coreAppServices) : base(coreAppServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _entityUtilsRepository = entityUtilsRepository ?? throw new ArgumentNullException(nameof(entityUtilsRepository));
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _compositionManager = compositionManager ?? throw new ArgumentNullException(nameof(compositionManager));
            _videoAvatarManager = videoAvatarManager ?? throw new ArgumentNullException(nameof(videoAvatarManager));
        }

        public async Task<ListResponse<EntityVideoCompositionSummary>> GetSourcesAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest, CancellationToken cancellationToken = default)
        {
            var modelType = ResolveSourceType(entityType);
            await AuthorizeOrgAccessAsync(user, org.Id, modelType);

            return await _repo.GetSourcesAsync(entityType.Trim(), org.Id, listRequest, cancellationToken).ConfigureAwait(false);
        }

        public async Task<EntityVideoCompositionSource> GetSourceAsync(string entityType, string entityId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            ResolveSourceType(entityType);

            var source = await _repo.GetSourceAsync(entityType.Trim(), entityId, org.Id, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return null;
            }

            await AuthorizeAsync(source.Entity, AuthorizeResult.AuthorizeActions.Read, user, org);
            return source;
        }

        public async Task<InvokeResult<VideoComposition>> CreateCompositionAsync(CreateEntityVideoCompositionRequest request, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return InvokeResult<VideoComposition>.FromError("Create entity video composition request is required.");
            }

            if (String.IsNullOrWhiteSpace(request.EntityType))
            {
                return InvokeResult<VideoComposition>.FromError("Entity type is required.");
            }

            if (String.IsNullOrWhiteSpace(request.EntityId))
            {
                return InvokeResult<VideoComposition>.FromError("Entity id is required.");
            }

            if (String.IsNullOrWhiteSpace(request.CompositionTemplateId))
            {
                return InvokeResult<VideoComposition>.FromError("Composition template id is required.");
            }

            var source = await GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find entity '{request.EntityId}'.");
            }

            if (source.Source.VideoCompositionInfo?.Composition != null && !String.IsNullOrWhiteSpace(source.Source.VideoCompositionInfo.Composition.Id))
            {
                return InvokeResult<VideoComposition>.FromError($"Entity '{source.Entity.Name}' is already bound to a video composition.");
            }

            var template = await _templateManager.GetVideoCompositionTemplateAsync(request.CompositionTemplateId, org, user).ConfigureAwait(false);
            if (template == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find video composition template '{request.CompositionTemplateId}'.");
            }

            EntityHeader videoAvatar = source.Source.VideoCompositionInfo?.VideoAvatar;
            if (!String.IsNullOrWhiteSpace(request.VideoAvatarId))
            {
                var avatar = await _videoAvatarManager.GetVideoAvatarAsync(request.VideoAvatarId, org, user).ConfigureAwait(false);
                if (avatar == null)
                {
                    return InvokeResult<VideoComposition>.FromError($"Could not find video avatar '{request.VideoAvatarId}'.");
                }

                videoAvatar = avatar.ToEntityHeader();
            }

            var content = source.GetContent() ?? new VideoCompositionContent();
            var sourceContentSha256 = CalculateSourceContentSha256(content, template, videoAvatar);

            var composition = new VideoComposition
            {
                Name = $"{source.Entity.Name} Video",
                Key = "vc" + Guid.NewGuid().ToId().Value.ToLowerInvariant(),
                Description = $"Video composition created for {source.Entity.Name}.",
                OwnerOrganization = org,
                CreatedBy = user,
                CreationDate = UtcTimestamp.Now,
                LastUpdatedBy = user,
                LastUpdatedDate = UtcTimestamp.Now,
                DefaultLocale = String.IsNullOrWhiteSpace(template.DefaultLocale) ? VideoComposition.DefaultLocaleCode : template.DefaultLocale,
                Title = content.Title,
                Subtitle = content.Subtitle,
                CallToAction = content.CallToAction,
                SourceScript = content.Script,
                BackgroundMediaResource = template.BackgroundMediaResource,
                BackgroundAudioMediaResource = template.BackgroundAudioMediaResource,
                BackgroundAudioVolume = template.BackgroundAudioVolume,
                BackgroundAudioFadeInSeconds = template.BackgroundAudioFadeInSeconds,
                BackgroundAudioFadeOutSeconds = template.BackgroundAudioFadeOutSeconds,
                LoopBackgroundAudio = template.LoopBackgroundAudio,
                OutputMediaLibrary = template.OutputMediaLibrary,
                Blocks = CloneBlocks(template.Blocks),
                SourceEntity = source.Entity.ToEntityHeader(),
                SourceEntityType = request.EntityType.Trim(),
                SourceCompositionTemplate = template.ToEntityHeader(),
                SourceCompositionTemplateVersion = template.Version,
                SourceVideoAvatar = videoAvatar,
                SourceContentSha256 = sourceContentSha256
            };

            var addResult = await _compositionManager.AddVideoCompositionAsync(composition, org, user).ConfigureAwait(false);
            if (!addResult.Successful)
            {
                return addResult.ToInvokeResult<VideoComposition>();
            }

            var info = source.Source.VideoCompositionInfo ?? new EntityVideoCompositionInfo();
            info.Composition = composition.ToEntityHeader();
            info.CompositionTemplate = template.ToEntityHeader();
            info.VideoAvatar = videoAvatar;
            info.TemplateVersion = template.Version;
            info.SourceContentSha256 = sourceContentSha256;

            var patchResult = await PatchVideoCompositionInfoInternalAsync(source.Entity.Id, info, user, cancellationToken).ConfigureAwait(false);
            if (!patchResult.Successful)
            {
                return patchResult.ToInvokeResult<VideoComposition>();
            }

            return InvokeResult<VideoComposition>.Create(composition);
        }

        public async Task<InvokeResult<VideoComposition>> SyncCompositionAsync(string entityType, string entityId, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            var source = await GetSourceAsync(entityType, entityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find entity '{entityId}'.");
            }

            var info = source.Source.VideoCompositionInfo;
            if (info?.Composition == null || String.IsNullOrWhiteSpace(info.Composition.Id))
            {
                return InvokeResult<VideoComposition>.FromError($"Entity '{source.Entity.Name}' is not bound to a video composition.");
            }

            var composition = await _compositionManager.GetVideoCompositionAsync(info.Composition.Id, org, user).ConfigureAwait(false);
            if (composition == null)
            {
                return InvokeResult<VideoComposition>.FromError($"Could not find bound video composition '{info.Composition.Id}'.");
            }

            VideoCompositionTemplate template = null;
            if (info.CompositionTemplate != null && !String.IsNullOrWhiteSpace(info.CompositionTemplate.Id))
            {
                template = await _templateManager.GetVideoCompositionTemplateAsync(info.CompositionTemplate.Id, org, user).ConfigureAwait(false);
            }

            var content = source.GetContent() ?? new VideoCompositionContent();
            var sourceContentSha256 = CalculateSourceContentSha256(content, template, info.VideoAvatar);

            composition.Title = content.Title;
            composition.Subtitle = content.Subtitle;
            composition.CallToAction = content.CallToAction;
            composition.SourceScript = content.Script;
            composition.SourceEntity = source.Entity.ToEntityHeader();
            composition.SourceEntityType = entityType.Trim();
            composition.SourceVideoAvatar = info.VideoAvatar;
            composition.SourceContentSha256 = sourceContentSha256;

            if (template != null)
            {
                composition.SourceCompositionTemplate = template.ToEntityHeader();
                composition.SourceCompositionTemplateVersion = template.Version;
                info.CompositionTemplate = template.ToEntityHeader();
                info.TemplateVersion = template.Version;
            }

            composition.LastUpdatedBy = user;
            composition.LastUpdatedDate = UtcTimestamp.Now;

            var updateResult = await _compositionManager.UpdateVideoCompositionAsync(composition, org, user).ConfigureAwait(false);
            if (!updateResult.Successful)
            {
                return updateResult.ToInvokeResult<VideoComposition>();
            }

            info.Composition = composition.ToEntityHeader();
            info.SourceContentSha256 = sourceContentSha256;

            var patchResult = await PatchVideoCompositionInfoInternalAsync(source.Entity.Id, info, user, cancellationToken).ConfigureAwait(false);
            if (!patchResult.Successful)
            {
                return patchResult.ToInvokeResult<VideoComposition>();
            }

            return InvokeResult<VideoComposition>.Create(composition);
        }

        public async Task<InvokeResult> PatchVideoCompositionInfoAsync(string entityType, string entityId, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader org, EntityHeader user, CancellationToken cancellationToken = default)
        {
            var source = await GetSourceAsync(entityType, entityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return InvokeResult.FromError($"Could not find entity '{entityId}'.");
            }

            await AuthorizeAsync(source.Entity, AuthorizeResult.AuthorizeActions.Update, user, org);
            return await PatchVideoCompositionInfoInternalAsync(entityId, videoCompositionInfo, user, cancellationToken).ConfigureAwait(false);
        }

        private Task<InvokeResult> PatchVideoCompositionInfoInternalAsync(string entityId, EntityVideoCompositionInfo videoCompositionInfo, EntityHeader user, CancellationToken cancellationToken)
        {
            var fields = new Dictionary<string, JToken>
            {
                [nameof(IVideoCompositionSource.VideoCompositionInfo)] = videoCompositionInfo == null
                    ? JValue.CreateNull()
                    : JObject.FromObject(videoCompositionInfo)
            };

            return _entityUtilsRepository.PatchEntityFieldsAsync(entityId, fields, user, cancellationToken);
        }

        private Type ResolveSourceType(string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type is required.", nameof(entityType));
            }

            if (!_entityTypeResolver.TryGetEntityType(entityType.Trim(), out var modelType) || modelType == null)
            {
                throw new InvalidOperationException($"Could not resolve entity type '{entityType}'.");
            }

            if (!typeof(EntityBase).IsAssignableFrom(modelType))
            {
                throw new InvalidOperationException($"Entity type '{entityType}' does not inherit from {nameof(EntityBase)}.");
            }

            if (!typeof(IVideoCompositionSource).IsAssignableFrom(modelType))
            {
                throw new InvalidOperationException($"Entity type '{entityType}' does not implement {nameof(IVideoCompositionSource)}.");
            }

            return modelType;
        }

        private static List<VideoCompositionBlock> CloneBlocks(List<VideoCompositionBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return new List<VideoCompositionBlock>();
            }

            return JsonConvert.DeserializeObject<List<VideoCompositionBlock>>(JsonConvert.SerializeObject(blocks)) ?? new List<VideoCompositionBlock>();
        }

        private static string CalculateSourceContentSha256(VideoCompositionContent content, VideoCompositionTemplate template, EntityHeader videoAvatar)
        {
            var value = new StringBuilder();
            value.AppendLine($"title={NormalizeHashValue(content?.Title)}");
            value.AppendLine($"subtitle={NormalizeHashValue(content?.Subtitle)}");
            value.AppendLine($"script={NormalizeHashValue(content?.Script)}");
            value.AppendLine($"callToAction={NormalizeHashValue(content?.CallToAction)}");
            value.AppendLine($"templateId={NormalizeHashValue(template?.Id)}");
            value.AppendLine($"templateVersion={template?.Version ?? 0}");
            value.AppendLine($"videoAvatarId={NormalizeHashValue(videoAvatar?.Id)}");

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            return BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
        }

        private static string NormalizeHashValue(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Trim();
        }
    }
}
