namespace LagoVista.MediaServices.Services
{
    public sealed class VideoProcessorLauncherOptions
    {
        public string Namespace { get; set; } = "video-processing";
        public string ConfigMapName { get; set; } = "video-processor-launcher-config";
        public string WorkerImageConfigKey { get; set; } = "WorkerImage";
        public string JobTemplateResourceName { get; set; } = "LagoVista.MediaServices.CloudRepos.Resources.VideoProcessorJob.yaml";
    }
}
