using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class MediaInspectionResult
    {
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public double DurationSeconds { get; set; }
        public string VideoCodec { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double FramesPerSecond { get; set; }
        public string PixelFormat { get; set; }
        public bool HasAudio { get; set; }
        public string AudioCodec { get; set; }
        public int? AudioSampleRate { get; set; }
        public int? AudioChannels { get; set; }
    }

    public sealed class FfprobeMediaInspectionService
    {
        private readonly ProcessRunner _processRunner;
        private readonly VideoAssemblyOptions _options;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public FfprobeMediaInspectionService(ProcessRunner processRunner, VideoAssemblyOptions options)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<MediaInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var arguments = $"-v error -print_format json -show_format -show_streams {ProcessRunner.Quote(filePath)}";
            var processResult = await _processRunner.RunAsync(_options.FfprobePath, arguments, cancellationToken: cancellationToken);
            if (processResult.ExitCode != 0) throw new InvalidOperationException($"ffprobe failed for '{filePath}'. {processResult.StandardError}");

            var response = JsonSerializer.Deserialize<FfprobeResponse>(processResult.StandardOutput, _jsonOptions) ?? throw new InvalidOperationException($"ffprobe returned invalid JSON for '{filePath}'.");
            var video = response.Streams?.FirstOrDefault(stream => String.Equals(stream.CodecType, "video", StringComparison.OrdinalIgnoreCase));
            var audio = response.Streams?.FirstOrDefault(stream => String.Equals(stream.CodecType, "audio", StringComparison.OrdinalIgnoreCase));
            if (video == null) throw new InvalidOperationException($"The source '{filePath}' does not contain a video stream.");

            return new MediaInspectionResult
            {
                FilePath = filePath,
                SizeBytes = ParseLong(response.Format?.Size),
                DurationSeconds = ParseDouble(response.Format?.Duration),
                VideoCodec = video.CodecName,
                Width = video.Width,
                Height = video.Height,
                FramesPerSecond = ParseFrameRate(video.AverageFrameRate),
                PixelFormat = video.PixelFormat,
                HasAudio = audio != null,
                AudioCodec = audio?.CodecName,
                AudioSampleRate = ParseNullableInt(audio?.SampleRate),
                AudioChannels = audio?.Channels
            };
        }

        private static double ParseFrameRate(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return 0;
            var parts = value.Split('/');
            if (parts.Length == 2 && Double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var numerator) && Double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var denominator) && denominator != 0) return numerator / denominator;
            return ParseDouble(value);
        }

        private static double ParseDouble(string value)
        {
            return Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        private static long ParseLong(string value)
        {
            return Int64.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        private static int? ParseNullableInt(string value)
        {
            return Int32.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        private sealed class FfprobeResponse
        {
            [JsonPropertyName("streams")]
            public FfprobeStream[] Streams { get; set; }

            [JsonPropertyName("format")]
            public FfprobeFormat Format { get; set; }
        }

        private sealed class FfprobeStream
        {
            [JsonPropertyName("codec_name")]
            public string CodecName { get; set; }

            [JsonPropertyName("codec_type")]
            public string CodecType { get; set; }

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("avg_frame_rate")]
            public string AverageFrameRate { get; set; }

            [JsonPropertyName("pix_fmt")]
            public string PixelFormat { get; set; }

            [JsonPropertyName("sample_rate")]
            public string SampleRate { get; set; }

            [JsonPropertyName("channels")]
            public int? Channels { get; set; }
        }

        private sealed class FfprobeFormat
        {
            [JsonPropertyName("duration")]
            public string Duration { get; set; }

            [JsonPropertyName("size")]
            public string Size { get; set; }
        }
    }
}
