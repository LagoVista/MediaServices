using LagoVista.VideoAssembly.Contracts;
using System;
using System.IO;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoAssemblyWorkspace : IDisposable
    {
        private bool _disposed;

        public VideoAssemblyWorkspace(string rootPath)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }
        public bool Preserve { get; set; }
        public string ConcatManifestPath => Path.Combine(RootPath, "concat.txt");
        public string OutputPath => Path.Combine(RootPath, "output.mp4");
        public string ThumbnailPath => Path.Combine(RootPath, "thumbnail.jpg");

        public string GetSourcePath(int index, VideoAssemblyBlockType blockType)
        {
            var extension = blockType == VideoAssemblyBlockType.Image ? ".image" : ".video";
            return Path.Combine(RootPath, $"block-{index:000}-source{extension}");
        }

        public string GetBackgroundPath(int index)
        {
            return Path.Combine(RootPath, $"block-{index:000}-background");
        }

        public string GetNormalizedPath(int index)
        {
            return Path.Combine(RootPath, $"block-{index:000}-normalized.mp4");
        }

        public string GetSubtitlePath(int index)
        {
            return Path.Combine(RootPath, $"block-{index:000}-labels.ass");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (Preserve || !Directory.Exists(RootPath)) return;
            Directory.Delete(RootPath, true);
        }
    }

    public sealed class VideoAssemblyWorkspaceFactory
    {
        private readonly VideoAssemblyOptions _options;

        public VideoAssemblyWorkspaceFactory(VideoAssemblyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public VideoAssemblyWorkspace Create(VideoAssemblyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var workspaceRoot = String.IsNullOrWhiteSpace(_options.WorkspaceRoot) ? Path.Combine(Path.GetTempPath(), "lago-video-assembly") : _options.WorkspaceRoot;
            return new VideoAssemblyWorkspace(Path.Combine(workspaceRoot, SanitizePathSegment(request.RequestId), SanitizePathSegment(request.AttemptId)));
        }

        private static string SanitizePathSegment(string value)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars()) value = value.Replace(invalidCharacter, '_');
            return value;
        }
    }
}
