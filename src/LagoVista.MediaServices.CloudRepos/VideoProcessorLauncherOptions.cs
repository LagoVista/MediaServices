namespace LagoVista.MediaServices.Services
{
    public sealed class VideoProcessorLauncherOptions
    {
        public string Namespace { get; set; } = "video-processing";
        public string Registry { get; set; } = "replace-with-registry";
        public string ImageVersion { get; set; } = "replace-with-version";
        public string JobTemplateResourceName { get; set; } = "LagoVista.MediaServices.Resources.VideoProcessorJob.yaml";
    }
}
