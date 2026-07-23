using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class TransparentVideoCropResult
    {
        public string OutputPath { get; set; }
        public bool WasCropped { get; set; }
        public int SourceWidth { get; set; }
        public int SourceHeight { get; set; }
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; }
        public int CropHeight { get; set; }
    }

    public sealed class TransparentVideoCropper
    {
        private const int AlphaThreshold = 8;
        private const int SafetyPaddingPixels = 12;
        private static readonly Regex CropRegex = new Regex(@"crop=(?<width>\d+):(?<height>\d+):(?<x>\d+):(?<y>\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ProcessRunner _processRunner;
        private readonly VideoAssemblyOptions _options;

        public TransparentVideoCropper(ProcessRunner processRunner, VideoAssemblyOptions options)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<TransparentVideoCropResult> CropAsync(string sourcePath, string outputPath, MediaInspectionResult sourceInspection, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("The transparent presenter video could not be found.", sourcePath);
            if (String.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));
            if (sourceInspection == null) throw new ArgumentNullException(nameof(sourceInspection));

            var unchanged = new TransparentVideoCropResult
            {
                OutputPath = sourcePath,
                SourceWidth = sourceInspection.Width,
                SourceHeight = sourceInspection.Height,
                CropWidth = sourceInspection.Width,
                CropHeight = sourceInspection.Height
            };

            if (!String.Equals(Path.GetExtension(sourcePath), ".webm", StringComparison.OrdinalIgnoreCase)) return unchanged;
            if (sourceInspection.Width <= 0 || sourceInspection.Height <= 0) return unchanged;

            var detectionArguments = $"-hide_banner -c:v libvpx-vp9 -i {ProcessRunner.Quote(sourcePath)} -map 0:v:0 -an -vf \"alphaextract,cropdetect=limit={AlphaThreshold}:round=2:reset=0\" -f null -";
            var detectionResult = await _processRunner.RunAsync(_options.FfmpegPath, detectionArguments, cancellationToken: cancellationToken);

            if (detectionResult.ExitCode != 0)
            {
                Console.WriteLine($"Transparent crop detection was skipped. {detectionResult.StandardError}");
                return unchanged;
            }

            var bounds = ParseVisibleBounds(detectionResult.StandardError, sourceInspection.Width, sourceInspection.Height);
            if (bounds == null) return unchanged;

            var cropX = MakeEvenDown(Math.Max(0, bounds.Value.X - SafetyPaddingPixels));
            var cropY = MakeEvenDown(Math.Max(0, bounds.Value.Y - SafetyPaddingPixels));
            var cropRight = Math.Min(sourceInspection.Width, bounds.Value.Right + SafetyPaddingPixels);
            var cropBottom = Math.Min(sourceInspection.Height, bounds.Value.Bottom + SafetyPaddingPixels);
            var cropWidth = MakeEvenDown(cropRight - cropX);
            var cropHeight = MakeEvenDown(cropBottom - cropY);

            if (cropWidth <= 0 || cropHeight <= 0) return unchanged;
            if (cropWidth >= sourceInspection.Width - 2 && cropHeight >= sourceInspection.Height - 2) return unchanged;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var cropFilter = $"crop={cropWidth}:{cropHeight}:{cropX}:{cropY}";
            var cropArguments = $"-y -hide_banner -c:v libvpx-vp9 -i {ProcessRunner.Quote(sourcePath)} -map 0:v:0 -map 0:a? -vf \"{cropFilter}\" -c:v libvpx-vp9 -pix_fmt yuva420p -auto-alt-ref 0 -crf 24 -b:v 0 -c:a copy {ProcessRunner.Quote(outputPath)}";
            var cropResult = await _processRunner.RunAsync(_options.FfmpegPath, cropArguments, cancellationToken: cancellationToken);

            if (cropResult.ExitCode != 0 || !File.Exists(outputPath))
            {
                Console.WriteLine($"Transparent video crop was skipped. {cropResult.StandardError}");
                TryDelete(outputPath);
                return unchanged;
            }

            Console.WriteLine($"Cropped transparent presenter from {sourceInspection.Width}x{sourceInspection.Height} to {cropWidth}x{cropHeight} at {cropX},{cropY}.");

            return new TransparentVideoCropResult
            {
                OutputPath = outputPath,
                WasCropped = true,
                SourceWidth = sourceInspection.Width,
                SourceHeight = sourceInspection.Height,
                CropX = cropX,
                CropY = cropY,
                CropWidth = cropWidth,
                CropHeight = cropHeight
            };
        }

        private static VisibleBounds? ParseVisibleBounds(string output, int sourceWidth, int sourceHeight)
        {
            if (String.IsNullOrWhiteSpace(output)) return null;

            var minX = sourceWidth;
            var minY = sourceHeight;
            var maxX = 0;
            var maxY = 0;
            var found = false;

            foreach (Match match in CropRegex.Matches(output))
            {
                if (!Int32.TryParse(match.Groups["width"].Value, out var width)) continue;
                if (!Int32.TryParse(match.Groups["height"].Value, out var height)) continue;
                if (!Int32.TryParse(match.Groups["x"].Value, out var x)) continue;
                if (!Int32.TryParse(match.Groups["y"].Value, out var y)) continue;
                if (width <= 0 || height <= 0) continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x + width);
                maxY = Math.Max(maxY, y + height);
                found = true;
            }

            if (!found) return null;

            minX = Math.Max(0, Math.Min(sourceWidth, minX));
            minY = Math.Max(0, Math.Min(sourceHeight, minY));
            maxX = Math.Max(minX, Math.Min(sourceWidth, maxX));
            maxY = Math.Max(minY, Math.Min(sourceHeight, maxY));

            return new VisibleBounds(minX, minY, maxX, maxY);
        }

        private static int MakeEvenDown(int value)
        {
            return Math.Max(0, value - value % 2);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }

        private readonly struct VisibleBounds
        {
            public VisibleBounds(int x, int y, int right, int bottom)
            {
                X = x;
                Y = y;
                Right = right;
                Bottom = bottom;
            }

            public int X { get; }
            public int Y { get; }
            public int Right { get; }
            public int Bottom { get; }
        }
    }
}
