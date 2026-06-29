using LagoVista.Core.Models;

namespace LagoVista.MediaServices.Models
{
    public sealed class GenerateEntityVideoRequest
    {
        public EntityHeader Organization { get; set; }
        public EntityHeader User { get; set; }

        public string AvatarMediaResourceId { get; set; }
        public string BackgroundMediaResourceId { get; set; }

        public string Script { get; set; }
        public string VideoName { get; set; }

        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string LanguageCode { get; set; }
        public string EntityProperty { get; set; }
    }
}
