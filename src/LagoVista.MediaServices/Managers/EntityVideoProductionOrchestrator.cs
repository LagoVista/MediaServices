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
        private const double CompositionCanvasWidth = 1920.0;
        private const double CompositionCanvasHeight = 1080.0;
        private const double PresenterHeightRatio = 2.0 / 3.0;

        private readonly IEntityVideoCompositionManager _entityCompositionManager;
        private readonly IVideoCompositionTemplateManager _templateManager;
        private readonly IVideoCompositionManager _compositionManager;
        private readonly IVideoAvatarManager _videoAvatarManager;
        private readonly IVideoProductionManager _videoProductionManager;
        private readonly IMediaServicesManager _mediaServicesManager;
        private readonly INotificationPublisher _notificationPublisher;

        public EntityVideoProductionOrchestrator(
            IEntityVideoCompositionManager entityCompositionManager,
            IVideoCompositionTemplateManager templateManager,
            IVideoCompositionManager compositionManager,
            IVideoAvatarManager videoAvatarManager,
            IVideoProductionManager videoProductionManager,
            IMediaServicesManager mediaServicesManager,
            ICoreAppServices coreAppServices)
        {
            _entityCompositionManager = entityCompositionManager ?? throw new ArgumentNullException(nameof(entityCompositionManager));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _compositionManager = compositionManager ?? throw new ArgumentNullException(nameof(compositionManager));
            _videoAvatarManager = videoAvatarManager ?? throw new ArgumentNullException(nameof(videoAvatarManager));
            _videoProductionManager = videoProductionManager ?? throw new ArgumentNullException(nameof(videoProductionManager));
            _mediaServicesManager = mediaServicesManager ?? throw new ArgumentNullException(nameof(mediaServicesManager));
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

            await PublishAsync(request, EntityVideoProductionStage.Validating, "Validating the source content and presenter avatar.");

            var source = await _entityCompositionManager.GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return await FailAsync(request, $"Could not find entity '{request.EntityId}'.");
            }

            var info = source.Source.VideoCompositionInfo;
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

            if (avatar.Status?.Value != VideoAvatarStatus.Ready)
            {
                var waiting = CreateWorkspace(request, EntityVideoProductionStage.WaitingForAvatar, "The presenter avatar must be ready before production can be created.", null, null);
                await PublishAsync(request, waiting.Stage, waiting.Message, null, null);
                return InvokeResult<EntityVideoProductionWorkspace>.Create(waiting);
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

                var activeWorkspace = CreateWorkspace(request, ResolveStage(production), "Presenter video production is already in progress.", null, production);
                await PublishAsync(request, activeWorkspace.Stage, activeWorkspace.Message, null, production.ToEntityHeader());
                return InvokeResult<EntityVideoProductionWorkspace>.Create(activeWorkspace);
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

                var readyWorkspace = CreateWorkspace(request, EntityVideoProductionStage.ProductionReady, "The presenter video is ready. Create the composition when you are ready for the next step.", null, production);
                await PublishAsync(request, readyWorkspace.Stage, readyWorkspace.Message, null, production.ToEntityHeader());
                return InvokeResult<EntityVideoProductionWorkspace>.Create(readyWorkspace);
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

            ApplyProductionSettings(production, source, avatar, voice, request, user);

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
                return await FailAsync(request, saveResult.Errors[0].Message, production: production.ToEntityHeader());
            }

            info = info ?? new EntityVideoCompositionInfo();
            info.VideoAvatar = avatar.ToEntityHeader();
            info.VideoProduction = production.ToEntityHeader();
            info.ActiveRunId = request.RunId;

            var patchResult = await _entityCompositionManager.PatchVideoCompositionInfoAsync(request.EntityType, request.EntityId, info, org, user, cancellationToken).ConfigureAwait(false);
            if (!patchResult.Successful)
            {
                return await FailAsync(request, patchResult.Errors[0].Message, production: production.ToEntityHeader());
            }

            var submitResult = await _videoProductionManager.SubmitVideoProductionAsync(production.Id, org, user).ConfigureAwait(false);
            if (!submitResult.Successful)
            {
                production = await _videoProductionManager.GetVideoProductionAsync(production.Id, org, user).ConfigureAwait(false);
                if (production?.Status?.Value == VideoProductionStatus.WaitingForAvatar)
                {
                    var waiting = CreateWorkspace(request, EntityVideoProductionStage.WaitingForAvatar, production.ErrorMessage, null, production);
                    await PublishAsync(request, waiting.Stage, waiting.Message, null, waiting.VideoProduction);
                    return InvokeResult<EntityVideoProductionWorkspace>.Create(waiting);
                }

                return await FailAsync(request, submitResult.Errors[0].Message, production: production?.ToEntityHeader());
            }

            production = submitResult.Result;
            var workspace = CreateWorkspace(request, EntityVideoProductionStage.Submitted, "Presenter video submitted for generation.", null, production);
            await PublishAsync(request, workspace.Stage, workspace.Message, null, workspace.VideoProduction);
            return InvokeResult<EntityVideoProductionWorkspace>.Create(workspace);
        }

        public async Task<InvokeResult<EntityVideoProductionWorkspace>> CreateCompositionAsync(
            CreateEntityVideoCompositionFromProductionRequest request,
            EntityHeader org,
            EntityHeader user,
            CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateCompositionRequest(request);
            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<EntityVideoProductionWorkspace>();
            }

            var progressRequest = new PrepareEntityVideoProductionRequest
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                VideoAvatarId = request.VideoAvatarId,
                RunId = request.RunId
            };

            await PublishAsync(progressRequest, EntityVideoProductionStage.Validating, "Validating the completed presenter video and composition template.");

            var source = await _entityCompositionManager.GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
            if (source == null)
            {
                return await FailAsync(progressRequest, $"Could not find entity '{request.EntityId}'.");
            }

            var info = source.Source.VideoCompositionInfo;
            var productionId = info?.VideoProduction?.Id;
            if (String.IsNullOrWhiteSpace(productionId))
            {
                return await FailAsync(progressRequest, "Create and complete the presenter video before creating the composition.");
            }

            var production = await _videoProductionManager.GetVideoProductionAsync(productionId, org, user).ConfigureAwait(false);
            if (production == null)
            {
                return await FailAsync(progressRequest, $"Could not find video production '{productionId}'.");
            }

            if ((production.Status?.Value != VideoProductionStatus.ProviderVideoReady &&
                 production.Status?.Value != VideoProductionStatus.Completed) ||
                production.FinalVideoMediaResource == null ||
                String.IsNullOrWhiteSpace(production.FinalVideoMediaResource.Id))
            {
                return await FailAsync(progressRequest, "The presenter video must finish rendering and importing before the composition can be created.", production: production.ToEntityHeader());
            }

            if (!production.IsCurrent)
            {
                return await FailAsync(progressRequest, "The presenter video is out of date. Regenerate it before creating or refreshing the composition.", production: production.ToEntityHeader());
            }

            var presenterMediaResource = await _mediaServicesManager.GetMediaResourceRecordAsync(
                production.FinalVideoMediaResource.Id,
                org,
                user).ConfigureAwait(false);

            if (presenterMediaResource == null)
            {
                return await FailAsync(
                    progressRequest,
                    $"Could not find presenter media resource '{production.FinalVideoMediaResource.Id}'.",
                    production: production.ToEntityHeader());
            }

            if (!presenterMediaResource.Width.HasValue ||
                presenterMediaResource.Width.Value <= 0 ||
                !presenterMediaResource.Height.HasValue ||
                presenterMediaResource.Height.Value <= 0)
            {
                return await FailAsync(
                    progressRequest,
                    "The completed presenter video does not include valid width and height metadata.",
                    production: production.ToEntityHeader());
            }

            var template = await _templateManager.GetVideoCompositionTemplateAsync(request.CompositionTemplateId, org, user).ConfigureAwait(false);
            if (template == null)
            {
                return await FailAsync(progressRequest, $"Could not find video composition template '{request.CompositionTemplateId}'.", production: production.ToEntityHeader());
            }

            var templateResult = ValidateTemplate(template);
            if (!templateResult.Successful)
            {
                return await FailAsync(progressRequest, templateResult.Errors[0].Message, production: production.ToEntityHeader());
            }

            VideoComposition composition;
            if (info?.Composition == null || String.IsNullOrWhiteSpace(info.Composition.Id))
            {
                var videoAvatarId = !String.IsNullOrWhiteSpace(request.VideoAvatarId)
                    ? request.VideoAvatarId
                    : info?.VideoAvatar?.Id;

                var createResult = await _entityCompositionManager.CreateCompositionAsync(new CreateEntityVideoCompositionRequest
                {
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    CompositionTemplateId = request.CompositionTemplateId,
                    VideoAvatarId = videoAvatarId
                }, org, user, cancellationToken).ConfigureAwait(false);

                if (!createResult.Successful)
                {
                    return await FailAsync(progressRequest, createResult.Errors[0].Message, production: production.ToEntityHeader());
                }

                composition = createResult.Result;
            }
            else
            {
                if (info.CompositionTemplate == null ||
                    !String.Equals(info.CompositionTemplate.Id, template.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return await FailAsync(progressRequest, "The entity is already bound to a different video composition template.", info.Composition, production.ToEntityHeader());
                }

                composition = await _compositionManager.GetVideoCompositionAsync(info.Composition.Id, org, user).ConfigureAwait(false);
                if (composition == null)
                {
                    return await FailAsync(progressRequest, $"Could not find bound video composition '{info.Composition.Id}'.", info.Composition, production.ToEntityHeader());
                }
            }

            var contentBlocks = composition.Blocks?.Where(block => block.Role == VideoCompositionBlockRole.Content).ToList();
            if (contentBlocks == null || contentBlocks.Count != 1)
            {
                return await FailAsync(progressRequest, "The video composition must contain exactly one content block.", composition.ToEntityHeader(), production.ToEntityHeader());
            }

            var contentBlock = contentBlocks[0];
            var effectiveBackground = contentBlock.BackgroundMediaResource ?? composition.BackgroundMediaResource;
            if (effectiveBackground == null || String.IsNullOrWhiteSpace(effectiveBackground.Id))
            {
                return await FailAsync(
                    progressRequest,
                    "The selected template must provide a background for its content block so the presenter can be scaled and positioned.",
                    composition.ToEntityHeader(),
                    production.ToEntityHeader());
            }

            contentBlock.MediaResource = production.FinalVideoMediaResource;
            ApplyDefaultPresenterLayout(contentBlock, presenterMediaResource.Width.Value, presenterMediaResource.Height.Value);
            composition.NotificationRunId = request.RunId;
            composition.LastUpdatedBy = user;
            composition.LastUpdatedDate = UtcTimestamp.Now;

            var updateResult = await _compositionManager.UpdateVideoCompositionAsync(composition, org, user).ConfigureAwait(false);
            if (!updateResult.Successful)
            {
                return await FailAsync(progressRequest, updateResult.Errors[0].Message, composition.ToEntityHeader(), production.ToEntityHeader());
            }

            source = await _entityCompositionManager.GetSourceAsync(request.EntityType, request.EntityId, org, user, cancellationToken).ConfigureAwait(false);
            info = source?.Source.VideoCompositionInfo ?? new EntityVideoCompositionInfo();
            info.VideoProduction = production.ToEntityHeader();
            info.ActiveRunId = request.RunId;

            var patchResult = await _entityCompositionManager.PatchVideoCompositionInfoAsync(request.EntityType, request.EntityId, info, org, user, cancellationToken).ConfigureAwait(false);
            if (!patchResult.Successful)
            {
                return await FailAsync(progressRequest, patchResult.Errors[0].Message, composition.ToEntityHeader(), production.ToEntityHeader());
            }

            var workspace = CreateWorkspace(progressRequest, EntityVideoProductionStage.CompositionReady, "The three-block composition is ready for review and assembly.", composition, production);
            await PublishAsync(progressRequest, workspace.Stage, workspace.Message, workspace.Composition, workspace.VideoProduction);
            return InvokeResult<EntityVideoProductionWorkspace>.Create(workspace);
        }

        private static void ApplyDefaultPresenterLayout(VideoCompositionBlock contentBlock, int sourceWidth, int sourceHeight)
        {
            var targetHeight = CompositionCanvasHeight * PresenterHeightRatio;
            var widthAtTargetHeight = targetHeight * sourceWidth / sourceHeight;
            var presenterWidth = Math.Min(CompositionCanvasWidth, widthAtTargetHeight);

            contentBlock.PresenterScale = presenterWidth / CompositionCanvasWidth;
            contentBlock.PresenterPositionX = 0.5;
            contentBlock.PresenterPositionY = 1.0;
        }

        private static void ApplyProductionSettings(
            VideoProduction production,
            EntityVideoCompositionSource source,
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
            production.DefaultLocale = String.IsNullOrWhiteSpace(production.DefaultLocale) ? VideoProduction.DefaultLocaleCode : production.DefaultLocale;
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
            if (String.IsNullOrWhiteSpace(request.RunId)) return InvokeResult.FromError("Run id is required.");
            return InvokeResult.Success;
        }

        private static InvokeResult ValidateCompositionRequest(CreateEntityVideoCompositionFromProductionRequest request)
        {
            if (request == null) return InvokeResult.FromError("Create entity video composition request is required.");
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

            var contentBlock = template.Blocks.Single(block => block.Role == VideoCompositionBlockRole.Content);
            var effectiveBackground = contentBlock.BackgroundMediaResource ?? template.BackgroundMediaResource;
            if (effectiveBackground == null || String.IsNullOrWhiteSpace(effectiveBackground.Id))
            {
                return InvokeResult.FromError(
                    $"Video composition template '{template.Name}' must provide a background for its content block so the presenter can be scaled and positioned.");
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
            if (production?.Status?.Value == VideoProductionStatus.ProviderVideoReady) return EntityVideoProductionStage.ProductionReady;
            if (production?.Status?.Value == VideoProductionStatus.Completed) return EntityVideoProductionStage.ProductionReady;
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
