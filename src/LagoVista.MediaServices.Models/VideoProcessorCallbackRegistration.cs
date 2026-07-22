using LagoVista.Core.Models;

namespace LagoVista.MediaServices.Models
{
    public sealed class VideoProcessorCallbackRegistration : TableStorageEntity
    {
        public string OrganizationId { get; set; }
        public string JobType { get; set; }
        public string RequestId { get; set; }
        public string AttemptId { get; set; }
        public string ProductionId { get; set; }
        public string MediaResourceId { get; set; }
        public string ProviderVideoId { get; set; }
        public string ImportLeaseKey { get; set; }
        public string ImportLeaseToken { get; set; }
        public string AccessTokenSha256 { get; set; }
        public string CreatedUtc { get; set; }
        public string ExpiresUtc { get; set; }
        public string LastCallbackUtc { get; set; }
        public long LastSequence { get; set; }
        public bool IsCompleted { get; set; }
        public string CompletedUtc { get; set; }
    }
}
