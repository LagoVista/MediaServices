using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Resources;
using System;
using System.Collections.Generic;

namespace LagoVista.MediaServices.Models
{
    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoComposition_Title, MediaServicesResources.Names.VideoComposition_Help, MediaServicesResources.Names.VideoComposition_Description,
        EntityDescriptionAttribute.EntityTypes.CoreIoTModel, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default",
        ListUIUrl: "/contentmanagement/videocompositionblocktemplates", EditUIUrl: "/contentmanagement/videocompositionblocktemplate/{id}", CreateUIUrl: "/contentmanagement/videocompositionblocktemplate/add",
        GetListUrl: "/api/media/videocomposition/blocktemplates", SaveUrl: "/api/media/videocomposition/blocktemplate", GetUrl: "/api/media/videocomposition/blocktemplate/{id}", FactoryUrl: "/api/media/videocomposition/blocktemplate/factory", DeleteUrl: "/api/media/videocomposition/blocktemplate/{id}",
        ClusterKey: "video", ModelType: EntityDescriptionAttribute.ModelTypes.Document, Shape: EntityDescriptionAttribute.EntityShapes.Entity, Lifecycle: EntityDescriptionAttribute.Lifecycles.RunTime,
        Sensitivity: EntityDescriptionAttribute.Sensitivities.Internal, IndexInclude: true, IndexTier: EntityDescriptionAttribute.IndexTiers.Primary, IndexPriority: 75, IndexTagsCsv: "media,video,composition,block,template")]
    public class VideoCompositionBlockTemplate : EntityBase, IValidateable, ISummaryFactory
    {
        public VideoCompositionBlockTemplate()
        {
            Icon = "lago-icon://system/nuvos-semantic-icon/video-production-default";
            Block = new VideoCompositionBlock();
            IsActive = true;
        }

        public VideoCompositionBlock Block { get; set; }

        public bool IsActive { get; set; }

        
        public void Validate(ValidationResult result)
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                result.AddUserError("A video composition block template requires a name.");
            }

            if (String.IsNullOrWhiteSpace(Key))
            {
                result.AddUserError("A video composition block template requires a key.");
            }

            if (Block == null)
            {
                result.AddUserError("A video composition block template requires a block snapshot.");
                return;
            }

            Block.Validate(result);
        }

        public VideoCompositionBlockTemplateSummary CreateSummary()
        {
            var summary = new VideoCompositionBlockTemplateSummary
            {
                Description = Description,
                BlockType = Block?.Type,
                MediaResource = Block?.MediaResource,
                BackgroundMediaResource = Block?.BackgroundMediaResource,
                DurationSeconds = Block?.DurationSeconds,
                LabelCount = Block?.CompositionLabels?.Count ?? 0,
                IsActive = IsActive
            };

            summary.Populate(this);
            return summary;
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return CreateSummary();
        }
    }

    [EntityDescription(MediaServicesDomain.MediaServices, MediaServicesResources.Names.VideoComposition_Title, MediaServicesResources.Names.VideoComposition_Help, MediaServicesResources.Names.VideoComposition_Description,
        EntityDescriptionAttribute.EntityTypes.Dto, typeof(MediaServicesResources), Icon: "lago-icon://system/nuvos-semantic-icon/video-production-default")]
    public class VideoCompositionBlockTemplateSummary : SummaryData
    {

        public VideoCompositionBlockType? BlockType { get; set; }

        public EntityHeader MediaResource { get; set; }

        public EntityHeader BackgroundMediaResource { get; set; }

        public double? DurationSeconds { get; set; }

        public int LabelCount { get; set; }

        public bool IsActive { get; set; }
    }

    public sealed class CreateVideoCompositionBlockTemplateRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public VideoCompositionBlock Block { get; set; }
    }
}
