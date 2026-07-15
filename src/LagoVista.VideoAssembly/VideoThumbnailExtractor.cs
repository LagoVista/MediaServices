using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoThumbnailExtractor
    {
        private readonly ProcessRunner _processRunner;
        private readonly VideoAssemblyOptions _options;

        public VideoThumbnailExtractor(ProcessRunner processRunner, VideoAssemblyOptions options)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task ExtractAsync(string videoPath, string thumbnailPath, double requestedTimeSeconds, double videoDurationSeconds, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(videoPath)) throw new ArgumentNullException(nameof(videoPath));
            if (!File.Exists(videoPath)) throw new FileNotFoundException("The source video could not be found.", videoPath);
            if (String.IsNullOrWhiteSpace(thumbnailPath)) throw new ArgumentNullException(nameof(thumbnailPath));

            var thumbnailTimeSeconds = ResolveThumbnailTime(requestedTimeSeconds, videoDurationSeconds);
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath));

            var arguments = $"-y -ss {FormatSeconds(thumbnailTimeSeconds)} -i {ProcessRunner.Quote(videoPath)} -frames:v 1 -vf \"scale=1280:-2\" -q:v 2 {ProcessRunner.Quote(thumbnailPath)}";
            var result = await _processRunner.RunAsync(_options.FfmpegPath, arguments, cancellationToken: cancellationToken);
            if (result.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed while generating the video thumbnail. {result.StandardError}");
            if (!File.Exists(thumbnailPath)) throw new InvalidOperationException("FFmpeg did not create the requested video thumbnail.");
        }

        private static double ResolveThumbnailTime(double requestedTimeSeconds, double videoDurationSeconds)
        {
            if (videoDurationSeconds <= 0) return Math.Max(0, requestedTimeSeconds);
            if (requestedTimeSeconds >= 0 && requestedTimeSeconds < videoDurationSeconds) return requestedTimeSeconds;
            return Math.Max(0, videoDurationSeconds / 2.0);
        }

        private static string FormatSeconds(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
