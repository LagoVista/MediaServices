using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LagoVista.VideoAssembly
{
    public sealed class AssSubtitleDocumentBuilder
    {
        private readonly VideoAssemblyOptions _options;

        public AssSubtitleDocumentBuilder(VideoAssemblyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string Build(VideoAssemblyBlock block, double blockDurationSeconds)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));

            var document = new StringBuilder();
            document.AppendLine("[Script Info]");
            document.AppendLine("ScriptType: v4.00+");
            document.AppendLine("PlayResX: 1920");
            document.AppendLine("PlayResY: 1080");
            document.AppendLine("ScaledBorderAndShadow: yes");
            document.AppendLine("WrapStyle: 2");
            document.AppendLine();
            document.AppendLine("[V4+ Styles]");
            document.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            document.AppendLine($"Style: Default,{_options.FontFamily},48,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1");
            document.AppendLine();
            document.AppendLine("[Events]");
            document.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var label in block.Labels ?? new List<VideoAssemblyTextLabel>())
            {
                if (String.IsNullOrWhiteSpace(label?.Text)) continue;

                var startSeconds = Math.Min(blockDurationSeconds, Math.Max(0, label.DelaySeconds));
                var requestedEndSeconds = label.VisibleDurationSeconds.HasValue ? startSeconds + label.VisibleDurationSeconds.Value : blockDurationSeconds;
                var endSeconds = Math.Min(blockDurationSeconds, Math.Max(startSeconds + 0.01, requestedEndSeconds));
                if (startSeconds >= blockDurationSeconds) continue;

                var weight = label.Bold ? 800 : 400;
                var color = ConvertColor(label.Color);
                var alignment = ResolveAlignment(label.Alignment);
                var fadeInMilliseconds = (int)Math.Round(label.FadeInSeconds * 1000);
                var fadeOutMilliseconds = (int)Math.Round(label.FadeOutSeconds * 1000);
                var fade = fadeInMilliseconds > 0 || fadeOutMilliseconds > 0 ? $"\\fad({fadeInMilliseconds},{fadeOutMilliseconds})" : String.Empty;
                var text = EscapeText(WrapText(label.Text, label.FontSize, label.MaxWidth));
                var overrides = $"{{\\an{alignment}\\pos({label.X},{label.Y})\\fn{EscapeOverride(_options.FontFamily)}\\b{weight}\\fs{label.FontSize}\\c{color}\\bord0\\shad0{fade}}}";
                document.AppendLine($"Dialogue: 0,{FormatTime(startSeconds)},{FormatTime(endSeconds)},Default,,0,0,0,,{overrides}{text}");
            }

            return document.ToString();
        }

        private static int ResolveAlignment(VideoAssemblyTextAlignment alignment)
        {
            switch (alignment)
            {
                case VideoAssemblyTextAlignment.Center:
                    return 8;
                case VideoAssemblyTextAlignment.Right:
                    return 9;
                default:
                    return 7;
            }
        }

        private static string WrapText(string text, int fontSize, int? maxWidth)
        {
            if (!maxWidth.HasValue || maxWidth.Value <= 0 || String.IsNullOrWhiteSpace(text)) return text;

            var approximateCharactersPerLine = Math.Max(1, (int)Math.Floor(maxWidth.Value / Math.Max(1, fontSize * 0.56)));
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length > 0 && currentLine.Length + 1 + word.Length > approximateCharactersPerLine)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                if (currentLine.Length > 0) currentLine.Append(' ');
                currentLine.Append(word);
            }

            if (currentLine.Length > 0) lines.Add(currentLine.ToString());
            return String.Join("\n", lines);
        }

        private static string EscapeText(string value)
        {
            return value.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\r\n", "\\N").Replace("\n", "\\N").Replace("\r", "\\N");
        }

        private static string EscapeOverride(string value)
        {
            return (value ?? String.Empty).Replace("\\", "\\\\").Replace("{", String.Empty).Replace("}", String.Empty);
        }

        private static string ConvertColor(string htmlColor)
        {
            var value = (htmlColor ?? "#FFFFFF").TrimStart('#');
            var red = value.Substring(0, 2);
            var green = value.Substring(2, 2);
            var blue = value.Substring(4, 2);
            return $"&H00{blue}{green}{red}&";
        }

        private static string FormatTime(double seconds)
        {
            var value = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return String.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}.{3:00}", (int)value.TotalHours, value.Minutes, value.Seconds, value.Milliseconds / 10);
        }
    }
}
