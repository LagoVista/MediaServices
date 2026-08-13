using LagoVista.Core;
using LagoVista.Core.Models;
using System;

namespace LagoVista.MediaServices.Models
{
    public interface IVideoCompositionSource
    {
        EntityVideoCompositionInfo VideoCompositionInfo { get; set; }

        VideoCompositionContent GetVideoCompositionContent();
    }

    public sealed class EntityVideoCompositionInfo
    {
        public EntityHeader Composition { get; set; }

        public EntityHeader CompositionTemplate { get; set; }

        public EntityHeader VideoAvatar { get; set; }

        public EntityHeader VideoProduction { get; set; }

        public string ActiveRunId { get; set; }

        public int? TemplateVersion { get; set; }

        public UtcTimestamp? LastGeneratedUtc { get; set; }

        public UtcTimestamp? LastPublishedUtc { get; set; }

        public string SourceContentSha256 { get; set; }
    }

    public sealed class VideoCompositionContent
    {
        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Script { get; set; }

        public string CallToAction { get; set; }
    }

    public sealed class EntityVideoCompositionSummary
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string EntityType { get; set; }

        public EntityVideoCompositionInfo VideoCompositionInfo { get; set; }

        public bool HasComposition =>
            VideoCompositionInfo?.Composition != null &&
            !String.IsNullOrWhiteSpace(VideoCompositionInfo.Composition.Id);
    }

    public sealed class EntityVideoCompositionSource
    {
        public EntityVideoCompositionSource(EntityBase entity, IVideoCompositionSource source)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public EntityBase Entity { get; }

        public IVideoCompositionSource Source { get; }

        public VideoCompositionContent GetContent()
        {
            return Source.GetVideoCompositionContent();
        }
    }

    public sealed class CreateEntityVideoCompositionRequest
    {
        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public string CompositionTemplateId { get; set; }

        public string VideoAvatarId { get; set; }

        public EntityHeader BackgroundAudioMediaResource { get; set; }

        public EntityHeader ContentBackgroundMediaResource { get; set; }
    }

    public enum EntityVideoProductionStage
    {
        Validating,
        CompositionReady,
        WaitingForAvatar,
        ProductionReady,
        Submitted,
        Rendering,
        Assembling,
        Completed,
        Failed
    }

    public sealed class PrepareEntityVideoProductionRequest
    {
        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public string CompositionTemplateId { get; set; }

        public string VideoAvatarId { get; set; }

        public string RunId { get; set; }
    }

    public sealed class CreateEntityVideoCompositionFromProductionRequest
    {
        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public string CompositionTemplateId { get; set; }

        public string VideoAvatarId { get; set; }

        public string RunId { get; set; }
    }

    public sealed class EntityVideoProductionWorkspace
    {
        public string RunId { get; set; }

        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public EntityVideoProductionStage Stage { get; set; }

        public string Message { get; set; }

        public EntityHeader Composition { get; set; }

        public EntityHeader VideoProduction { get; set; }

        public EntityHeader OutputMediaResource { get; set; }
    }

    public sealed class PatchEntityVideoCompositionInfoRequest
    {
        public EntityVideoCompositionInfo VideoCompositionInfo { get; set; }
    }
}
