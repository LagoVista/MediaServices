using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    public class EntityVideoProductionOrchestrator : IEntityVideoProductionOrchestrator
    {
        private readonly IEntityVideoCompositionManager _entityCompositionManager;
        private readonly IVideoCompositionTemplateManager _templateManager;
        private readonly IVideoCompositionManager _compositionManager;
        private readonly IVideoAvatarManager _videoAvatarManager;
        private readonly IVideoProductionManager _videoProductionManager;
        private readonly IEntityVideoCompositionContinuation _compositionContinuation;
        private readonly INotificationPublisher _notificationPublisher;

        public EntityVideoProductionOrchestrator(
            IEntityVideoCompositionManager entityCompositionManager,
            IVideoCompositionTemplateManager templateManager,
            IVideoCompositionManager compositionManager,
            IVideoAvatarManager videoAvatarManager,
            IVideoProductionManager videoProductionManager,
            IEntityVideoCompositionContinuation compositionContinuation,
            ICoreAppServices coreAppServices)
        {
            _entityCompositionManager = entityCompositionManager ?? throw new ArgumentNullException(nameof(entityCompositionManager));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _compositionManager = compositionManager ?? throw new ArgumentNullException(nameof(compositionManager));
            _videoAvatarManager = videoAvatarManager ?? throw new ArgumentNullException(nameof(videoAvatarManager));
            _videoProductionManager = videoProductionManager ?? throw new ArgumentNullException(nameof(videoProductionManager));
            _compositionContinuation = compositionContinuation ?? throw new ArgumentNullException(nameof(compositionContinuation));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult<EntityVideoProductionWorkspace>> PrepareAsync(
            PrepareEntityVideoProductionRequest request,
            EntityHeader org,
            EntityHeader user,
            CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRequest(request);
            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<EntityVideoProductionWorkspace>();
            }

            await PublishAsync(request, EntityVideoProductionStage.Validating, "Validating the video template and source content.");

            var source = await _entityCompositionManager.GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return await FailAsync(request, $"Could not find entity '{request.EntityId}'.");
            }

            var template = await _templateManager.GetVideoCompositionTemplateAsync(request.CompositionTemplateId, org, user).ConfigureAwait(false);
            if (template == null)
            {
                return await FailAsync(request, $"Could not find video composition template '{request.CompositionTemplateId}'.");
            }

            var templateResult = ValidateTemplate(template);
            if (!templateResult.Successful)
            {
                return await FailAsync(request, templateResult.Errors[0].Message);
            }

            VideoComposition composition;
            var info = source.Source.VideoCompositionInfo;
            if (info?.Composition == null || String.IsNullOrWhiteSpace(info.Composition.Id))
            {
                var createResult = await _entityCompositionManager.CreateCompositionAsync(new CreateEntityVideoCompositionRequest
                {
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    CompositionTemplateId = request.CompositionTemplateId,
                    VideoAvatarId = request.VideoAvatarId
                }, org, user, cancellationToken).ConfigureAwait(false);

                if (!createResult.Successful)
                {
                    return await FailAsync(request, createResult.Errors[0].Message);
                }

                composition = createResult.Result;
                source = await _entityCompositionManager.GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
                info = source?.Source.VideoCompositionInfo;
            }
            else
            {
                if (info.CompositionTemplate == null ||
                    !String.Equals(info.CompositionTemplate.Id, template.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return await FailAsync(request, "The entity is already bound to a different video composition template.");
                }

                composition = await _compositionManager.GetVideoCompositionAsync(info.Composition.Id, org, user).ConfigureAwait(false);
                if (composition == null)
                {
                    return await FailAsync(request, $"Could not find bound video composition '{info.Composition.Id}'.");
                }
            }

            composition.NotificationRunId = request.RunId;
            var compositionUpdateResult = await _compositionManager.UpdateVideoCompositionAsync(composition, org, user).ConfigureAwait(false);
            if (!compositionUpdateResult.Successful)
            {
                return await FailAsync(request, compositionUpdateResult.Errors[0].Message);
            }

            await PublishAsync(request, EntityVideoProductionStage.CompositionReady, "The three-block video composition is ready.", composition: composition.ToEntityHeader());

            var videoAvatarId = !String.IsNullOrWhiteSpace(request.VideoAvatarId)
                ? request.VideoAvatarId
                : info?.VideoAvatar?.Id;

            if (String.IsNullOrWhiteSpace(videoAvatarId))
            {
                return await FailAsync(request, "A video avatar is required before production can be submitted.");
            }

            var avatar = await _videoAvatarManager.GetVideoAvatarAsync(videoAvatarId, org, user).ConfigureAwait(false);
            if (avatar == null)
            {
                return await FailAsync(request, $"Could not find video avatar '{videoAvatarId}'.");
            }

            var voice = avatar.GetDefaultVoice();
            if (voice == null || String.IsNullOrWhiteSpace(voice.VoiceId))
            {
                return await FailAsync(request, $"Video avatar '{avatar.Name}' does not have a default voice.");
            }

            VideoProduction production = null;
            if (info?.VideoProduction != null && !String.IsNullOrWhiteSpace(info.VideoProduction.Id))
            {
                production = await _videoProductionManager.GetVideoProductionAsync(info.VideoProduction.Id, org, user).ConfigureAwait(false);
            }

            if (production != null && IsActive(production.Status))
            {
                production.NotificationRunId = request.RunId;
                await _videoProductionManager.UpdateVideoProductionAsync(production, org, user).ConfigureAwait(false);

                return InvokeResult<EntityVideoProductionWorkspace>.Create(CreateWorkspace(
                    request,
                    ResolveStage(production),
                    "Video production is already in progress.",
                    composition,
                    production));
            }

            if (production != null &&
                (production.Status?.Value == VideoProductionStatus.ProviderVideoReady ||
                 production.Status?.Value == VideoProductionStatus.Completed) &&
                production.FinalVideoMediaResource != null &&
                !String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id) &&
                production.IsCurrent)
            {
                production.NotificationRunId = request.RunId;
                await _videoProductionManager.UpdateVideoProductionAsync(production, org, user).ConfigureAwait(false);

                var continuationResult = await _compositionContinuation.ContinueAfterVideoImportAsync(production, cancellationToken).ConfigureAwait(false);
                if (!continuationResult.Successful)
                {
                    return await FailAsync(request, continuationResult.Errors[0].Message, composition.ToEntityHeader(), production.ToEntityHeader());
                }

                return InvokeResult<EntityVideoProductionWorkspace>.Create(CreateWorkspace(
                    request,
                    production.Status?.Value == VideoProductionStatus.Completed ? EntityVideoProductionStage.Completed : EntityVideoProductionStage.Assembling,
                    "The presenter video is ready and the composition is advancing.",
                    composition,
                    production));
            }

            var isNewProduction = production == null;
            if (isNewProduction)
            {
                production = new VideoProduction
                {
                    Id = Guid.NewGuid().ToId(),
                    Key = "vp" + Guid.NewGuid().ToId().Value.ToLowerInvariant(),
                    Name = $"{source.Entity.Name} Presenter Video",
                    Description = $"Presenter video generated for {source.Entity.Name}.",
                    OwnerOrganization = org,
                    CreatedBy = user,
                    CreationDate = UtcTimestamp.Now
                };
            }

            ApplyProductionSettings(production, source, composition, avatar, voice, request, user);

            InvokeResult<VideoProduction> saveResult;
            if (isNewProduction)
            {
                saveResult = await _videoProductionManager.AddVideoProductionAsync(production, org, user).ConfigureAwait(false);
            }
            else
            {
                saveResult = await _videoProductionManager.UpdateVideoProductionAsync(production, org, user).ConfigureAwait(false);
            }

            if (!saveResult.Successful)
            {
                return await FailAsync(request, saveResult.Errors[0].Message, composition.ToEntityHeader());
            }

            info = info ?? new EntityVideoCompositionInfo();
            info.Composition = composition.ToEntityHeader();
            info.CompositionTemplate = template.ToEntityHeader();
            info.VideoAvatar = avatar.ToEntityHeader();
            info.VideoProduction = production.ToEntityHeader();
            info.ActiveRunId = request.RunId;

            var patchResult = await _entityCompositionManager.PatchVideoCompositionInfoAsync(request.EntityType, request.EntityId, info, org, user, cancellationToken).ConfigureAwait(false);
            if (!patchResult.Successful)
            {
                return await FailAsync(request, patchResult.Errors[0].Message, composition.ToEntityHeader(), production.ToEntityHeader());
            }

            var submitResult = await _videoProductionManager.SubmitVideoProductionAsync(production.Id, org, user).ConfigureAwait(false);
            if (!submitResult.Successful)
            {
                production = await _videoProductionManager.GetVideoProductionAsync(production.Id, org, user).ConfigureAwait(false);
                if (production?.Status?.Value == VideoProductionStatus.WaitingForAvatar)
                {
                    var waiting = CreateWorkspace(request, EntityVideoProductionStage.WaitingForAvatar, production.ErrorMessage, composition, production);
                    await PublishAsync(request, waiting.Stage, waiting.Message, waiting.Composition, waiting.VideoProduction);
                    return InvokeResult<EntityVideoProductionWorkspace>.Create(waiting);
                }

                return await FailAsync(request, submitResult.Errors[0].Message, composition.ToEntityHeader(), production?.ToEntityHeader());
            }

            production = submitResult.Result;
            var workspace = CreateWorkspace(request, EntityVideoProductionStage.Submitted, "Presenter video submitted for generation.", composition, production);
            await PublishAsync(request, workspace.Stage, workspace.Message, workspace.Composition, workspace.VideoProduction);
            return InvokeResult<EntityVideoProductionWorkspace>.Create(workspace);
        }

        private static void ApplyProductionSettings(
            VideoProduction production,
            EntityVideoCompositionSource source,
            VideoComposition composition,
            VideoAvatar avatar,
            VideoAvatarVoice voice,
            PrepareEntityVideoProductionRequest request,
            EntityHeader user)
        {
            var content = source.GetContent() ?? new VideoCompositionContent();
            var look = avatar.Looks?.FirstOrDefault(item => item.IsPrimary && item.IsActive)
                ?? avatar.Looks?.FirstOrDefault(item => item.IsActive);

            production.VideoAvatar = avatar.ToEntityHeader();
            production.VideoAvatarLookId = look?.Id;
            if (String.IsNullOrWhiteSpace(production.Script))
            {
                production.Script = content.Script;
            }
            production.VideoName = $"{source.Entity.Name} Presenter Video";
            production.VoiceBindingId = voice.Id;
            production.VoiceId = voice.VoiceId;
            production.VoiceName = String.IsNullOrWhiteSpace(voice.VoiceName) ? voice.Label : voice.VoiceName;
            production.LanguageCode = voice.LanguageCode;
            production.Locale = voice.Locale;
            production.DefaultLocale = String.IsNullOrWhiteSpace(composition.DefaultLocale) ? VideoProduction.DefaultLocaleCode : composition.DefaultLocale;
            production.OutputMediaLibrary = composition.OutputMediaLibrary;
            production.TargetEntityType = request.EntityType.Trim();
            production.TargetEntityId = request.EntityId;
            production.TargetEntityName = source.Entity.Name;
            production.TargetEntityProperty = nameof(EntityVideoCompositionInfo.VideoProduction);
            production.NotificationRunId = request.RunId;
            production.LastUpdatedBy = user;
            production.LastUpdatedDate = UtcTimestamp.Now;
            production.ErrorMessage = null;
        }

        private static InvokeResult ValidateRequest(PrepareEntityVideoProductionRequest request)
        {
            if (request == null) return InvokeResult.FromError("Prepare entity video production request is required.");
            if (String.IsNullOrWhiteSpace(request.EntityType)) return InvokeResult.FromError("Entity type is required.");
            if (String.IsNullOrWhiteSpace(request.EntityId)) return InvokeResult.FromError("Entity id is required.");
            if (String.IsNullOrWhiteSpace(request.CompositionTemplateId)) return InvokeResult.FromError("Composition template id is required.");
            if (String.IsNullOrWhiteSpace(request.RunId)) return InvokeResult.FromError("Run id is required.");
            return InvokeResult.Success;
        }

        private static InvokeResult ValidateTemplate(VideoCompositionTemplate template)
        {
            if (!template.IsActive) return InvokeResult.FromError($"Video composition template '{template.Name}' is not active.");
            if (template.Blocks == null || template.Blocks.Count != 3)
            {
                return InvokeResult.FromError($"Video composition template '{template.Name}' must contain exactly three blocks.");
            }

            var requiredRoles = new[]
            {
                VideoCompositionBlockRole.Intro,
                VideoCompositionBlockRole.Content,
                VideoCompositionBlockRole.CallToAction
            };

            foreach (var role in requiredRoles)
            {
                if (template.Blocks.Count(block => block.Role == role) != 1)
                {
                    return InvokeResult.FromError($"Video composition template '{template.Name}' must contain exactly one {role} block.");
                }
            }

            return InvokeResult.Success;
        }

        private static bool IsActive(EntityHeader<VideoProductionStatus> status)
        {
            if (status == null) return false;

            return status.Value == VideoProductionStatus.PreparingAvatar ||
                   status.Value == VideoProductionStatus.Submitting ||
                   status.Value == VideoProductionStatus.Submitted ||
                   status.Value == VideoProductionStatus.Rendering ||
                   status.Value == VideoProductionStatus.ProviderCompleted ||
                   status.Value == VideoProductionStatus.ImportingProviderVideo;
        }

        private static EntityVideoProductionStage ResolveStage(VideoProduction production)
        {
            if (production?.Status?.Value == VideoProductionStatus.WaitingForAvatar) return EntityVideoProductionStage.WaitingForAvatar;
            if (production?.Status?.Value == VideoProductionStatus.ProviderVideoReady) return EntityVideoProductionStage.Assembling;
            if (production?.Status?.Value == VideoProductionStatus.Completed) return EntityVideoProductionStage.Completed;
            if (production?.Status?.Value == VideoProductionStatus.Failed) return EntityVideoProductionStage.Failed;
            return EntityVideoProductionStage.Rendering;
        }

        private static EntityVideoProductionWorkspace CreateWorkspace(
            PrepareEntityVideoProductionRequest request,
            EntityVideoProductionStage stage,
            string message,
            VideoComposition composition,
            VideoProduction production)
        {
            return new EntityVideoProductionWorkspace
            {
                RunId = request.RunId,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Stage = stage,
                Message = message,
                Composition = composition?.ToEntityHeader(),
                VideoProduction = production?.ToEntityHeader(),
                OutputMediaResource = composition?.OutputMediaResource
            };
        }

        private async Task<InvokeResult<EntityVideoProductionWorkspace>> FailAsync(
            PrepareEntityVideoProductionRequest request,
            string message,
            EntityHeader composition = null,
            EntityHeader production = null)
        {
            await PublishAsync(request, EntityVideoProductionStage.Failed, message, composition, production);
            return InvokeResult<EntityVideoProductionWorkspace>.FromError(message);
        }

        private Task PublishAsync(
            PrepareEntityVideoProductionRequest request,
            EntityVideoProductionStage stage,
            string message,
            EntityHeader composition = null,
            EntityHeader production = null)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.RunId))
            {
                return Task.CompletedTask;
            }

            return _notificationPublisher.PublishAsync(
                Targets.WebSocket,
                Channels.Entity,
                request.RunId,
                "entity-video-production-progress",
                new EntityVideoProductionWorkspace
                {
                    RunId = request.RunId,
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    Stage = stage,
                    Message = message,
                    Composition = composition,
                    VideoProduction = production
                });
        }
    }

    public class EntityVideoCompositionContinuation : IEntityVideoCompositionContinuation
    {
        private readonly IEntityVideoCompositionManager _entityCompositionManager;
        private readonly IVideoCompositionManager _compositionManager;
        private readonly IVideoAssemblyRequestManager _assemblyRequestManager;
        private readonly INotificationPublisher _notificationPublisher;

        public EntityVideoCompositionContinuation(
            IEntityVideoCompositionManager entityCompositionManager,
            IVideoCompositionManager compositionManager,
            IVideoAssemblyRequestManager assemblyRequestManager,
            ICoreAppServices coreAppServices)
        {
            _entityCompositionManager = entityCompositionManager ?? throw new ArgumentNullException(nameof(entityCompositionManager));
            _compositionManager = compositionManager ?? throw new ArgumentNullException(nameof(compositionManager));
            _assemblyRequestManager = assemblyRequestManager ?? throw new ArgumentNullException(nameof(assemblyRequestManager));
            _notificationPublisher = coreAppServices?.NotificationPublisher ?? throw new ArgumentNullException(nameof(coreAppServices.NotificationPublisher));
        }

        public async Task<InvokeResult> ContinueAfterVideoImportAsync(VideoProduction production, CancellationToken cancellationToken = default)
        {
            if (production == null ||
                !String.Equals(production.TargetEntityProperty, nameof(EntityVideoCompositionInfo.VideoProduction), StringComparison.Ordinal) ||
                String.IsNullOrWhiteSpace(production.TargetEntityType) ||
                String.IsNullOrWhiteSpace(production.TargetEntityId))
            {
                return InvokeResult.Success;
            }

            if (production.FinalVideoMediaResource == null || String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                return InvokeResult.FromError("The presenter video does not have a completed media resource.");
            }

            var org = production.OwnerOrganization;
            var user = production.LastUpdatedBy ?? production.CreatedBy;
            var source = await _entityCompositionManager.GetSourceAsync(production.TargetEntityType, production.TargetEntityId, org, user, cancellationToken).ConfigureAwait(false);
            var compositionId = source?.Source.VideoCompositionInfo?.Composition?.Id;
            if (String.IsNullOrWhiteSpace(compositionId))
            {
                return InvokeResult.FromError("The target entity is not bound to a video composition.");
            }

            var composition = await _compositionManager.GetVideoCompositionAsync(compositionId, org, user).ConfigureAwait(false);
            if (composition == null)
            {
                return InvokeResult.FromError($"Could not find video composition '{compositionId}'.");
            }

            var contentBlocks = composition.Blocks?.Where(block => block.Role == VideoCompositionBlockRole.Content).ToList();
            if (contentBlocks == null || contentBlocks.Count != 1)
            {
                return InvokeResult.FromError("The video composition must contain exactly one content block.");
            }

            var contentBlock = contentBlocks[0];
            if (String.Equals(contentBlock.MediaResource?.Id, production.FinalVideoMediaResource.Id, StringComparison.OrdinalIgnoreCase) &&
                (composition.Status?.Value == VideoCompositionStatus.Queued ||
                 composition.Status?.Value == VideoCompositionStatus.Assembling ||
                 composition.Status?.Value == VideoCompositionStatus.Completed))
            {
                return InvokeResult.Success;
            }

            contentBlock.MediaResource = production.FinalVideoMediaResource;
            composition.NotificationRunId = production.NotificationRunId;
            composition.LastUpdatedBy = user;
            composition.LastUpdatedDate = UtcTimestamp.Now;

            var updateResult = await _compositionManager.UpdateVideoCompositionAsync(composition, org, user).ConfigureAwait(false);
            if (!updateResult.Successful)
            {
                return updateResult;
            }

            if (!String.IsNullOrWhiteSpace(production.NotificationRunId))
            {
                await _notificationPublisher.PublishAsync(
                    Targets.WebSocket,
                    Channels.Entity,
                    production.NotificationRunId,
                    "entity-video-production-progress",
                    new EntityVideoProductionWorkspace
                    {
                        RunId = production.NotificationRunId,
                        EntityType = production.TargetEntityType,
                        EntityId = production.TargetEntityId,
                        Stage = EntityVideoProductionStage.Assembling,
                        Message = "Presenter video is ready; assembling the three-block composition.",
                        Composition = composition.ToEntityHeader(),
                        VideoProduction = production.ToEntityHeader()
                    });
            }

            var assemblyResult = await _assemblyRequestManager.PrepareAssemblyRequestAsync(composition.Id, null, org, user, cancellationToken).ConfigureAwait(false);
            return assemblyResult.Successful ? InvokeResult.Success : InvokeResult.FromError(assemblyResult.Errors[0].Message);
        }
    }
}
