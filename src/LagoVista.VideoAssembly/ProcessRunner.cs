using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class ProcessExecutionResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
    }

    public sealed class ProcessRunner
    {
        public async Task<ProcessExecutionResult> RunAsync(string executablePath, string arguments, Action<string> standardOutputLine = null, Action<string> standardErrorLine = null, CancellationToken cancellationToken = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start()) throw new InvalidOperationException($"Unable to start executable '{executablePath}'.");

            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited) process.Kill(true);
                }
                catch
                {
                }
            });

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            var outputTask = ReadLinesAsync(process.StandardOutput, line => { standardOutput.AppendLine(line); standardOutputLine?.Invoke(line); }, cancellationToken);
            var errorTask = ReadLinesAsync(process.StandardError, line => { standardError.AppendLine(line); standardErrorLine?.Invoke(line); }, cancellationToken);
            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), outputTask, errorTask);

            return new ProcessExecutionResult { ExitCode = process.ExitCode, StandardOutput = standardOutput.ToString(), StandardError = standardError.ToString() };
        }

        public static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        private static async Task ReadLinesAsync(System.IO.StreamReader reader, Action<string> onLine, CancellationToken cancellationToken)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;
                onLine(line);
            }
        }
    }
}
