using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Models.Icons;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Interfaces
{
    public interface ILagoVistaEntityInstanceIconGenerationManager
    {
        Task<InvokeResult<LagoVistaGeneratedInstanceIconResult>> GenerateInstanceIconAsync(LagoVistaGeneratedInstanceIconRequest request, EntityHeader org, EntityHeader user);
    }

    public class LagoVistaGeneratedInstanceIconRequest
    {
        public LagoVistaGeneratedInstanceIconRequest()
        {
          
        }

        public string EntityTypeName { get; set; }

        public string EntityId { get; set; }

        public string EntityKey { get; set; }

        public string EntityName { get; set; }

        public string EntityDescription { get; set; }

        public string CurrentIcon { get; set; }

        public string BaseIconPrompt { get; set; }

        public string AdditionalGuidance { get; set; }

        public bool ApplyChanges { get; set; }
    }

    public class LagoVistaGeneratedInstanceIconResult
    {
        public LagoVistaGeneratedInstanceIconResult()
        {
            Assets = new Dictionary<string, string>();
        }

        public string IconReference { get; set; }

        public string IconKey { get; set; }

        public string OrgNamespace { get; set; }

        public string FamilyKey { get; set; }

        public int Version { get; set; }

        public string PreviewUrl { get; set; }

        public string SourceUrl { get; set; }

        public string ManifestUrl { get; set; }

        public string GenerationRecordPath { get; set; }

        public string PublishedUtc { get; set; }

        public bool Applied { get; set; }

        public Dictionary<string, string> Assets { get; set; }

        public LagoVistaIconPublishResult PublishResult { get; set; }
    }
}
