using LagoVista.VideoAssembly;
using LagoVista.VideoAssembly.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly.Worker
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            WorkerInvocation invocation;
            try
            {
                invocation = WorkerInvocation.Parse(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }

            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", true, false).AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, false).AddEnvironmentVariables().AddCommandLine(args);
            var options = builder.Configuration.GetSection("VideoAssembly").Get<VideoAssemblyOptions>() ?? new VideoAssemblyOptions();
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<VideoAssemblyRequestValidator>();
            builder.Services.AddSingleton<VideoAssemblyWorkspaceFactory>();
            builder.Services.AddSingleton<ProcessRunner>();
            builder.Services.AddSingleton<AssSubtitleDocumentBuilder>();
            builder.Services.AddSingleton<FfprobeMediaInspectionService>();
            builder.Services.AddHttpClient<RequestLoader>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddHttpClient<VideoAssemblySourceDownloader>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddSingleton<IVideoAssemblyService, FfmpegVideoAssemblyService>();

            using var host = builder.Build();
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };

            try
            {
                var request = await host.Services.GetRequiredService<RequestLoader>().LoadAsync(invocation, cancellation.Token);
                var errors = host.Services.GetRequiredService<VideoAssemblyRequestValidator>().Validate(request);
                if (errors.Count > 0)
                {
                    Console.Error.WriteLine(String.Join(Environment.NewLine, errors));
                    return 4;
                }

                Console.WriteLine($"Loaded request '{request.RequestId}', attempt '{request.AttemptId}', production '{request.ProductionId}'.");
                var progress = new Progress<VideoAssemblyProgress>(WriteProgress);
                var result = await host.Services.GetRequiredService<IVideoAssemblyService>().AssembleAsync(request, progress, cancellation.Token);
                if (!result.Successful)
                {
                    Console.Error.WriteLine(result.ErrorMessage);
                    return 1;
                }

                Console.WriteLine($"Output: {result.OutputFilePath}");
                Console.WriteLine($"Duration: {result.OutputDurationSeconds} seconds");
                Console.WriteLine($"Size: {result.OutputSizeBytes} bytes");
                Console.WriteLine($"SHA-256: {result.Sha256}");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Video assembly was cancelled.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 3;
            }
        }

        private static void WriteProgress(VideoAssemblyProgress progress)
        {
            var percent = progress.PercentComplete.HasValue ? $" {progress.PercentComplete.Value}%" : String.Empty;
            var bytes = progress.BytesCompleted.HasValue ? $" {progress.BytesCompleted.Value}/{progress.BytesTotal?.ToString() ?? "?"} bytes" : String.Empty;
            Console.WriteLine($"[{progress.Stage}]{percent}{bytes} {progress.Message}");
        }
    }

    public sealed class WorkerInvocation
    {
        public string RequestUrl { get; private set; }
        public string RequestFile { get; private set; }

        public static WorkerInvocation Parse(string[] args)
        {
            var result = new WorkerInvocation { RequestUrl = Environment.GetEnvironmentVariable("VIDEO_ASSEMBLY_REQUEST_URL"), RequestFile = Environment.GetEnvironmentVariable("VIDEO_ASSEMBLY_REQUEST_FILE") };
            for (var index = 0; index < args.Length; index++)
            {
                if (String.Equals(args[index], "--request-url", StringComparison.OrdinalIgnoreCase)) result.RequestUrl = ReadValue(args, ref index);
                else if (String.Equals(args[index], "--request-file", StringComparison.OrdinalIgnoreCase)) result.RequestFile = ReadValue(args, ref index);
            }

            if (String.IsNullOrWhiteSpace(result.RequestUrl) == String.IsNullOrWhiteSpace(result.RequestFile)) throw new ArgumentException("Specify exactly one request source using --request-url, --request-file, VIDEO_ASSEMBLY_REQUEST_URL, or VIDEO_ASSEMBLY_REQUEST_FILE.");
            return result;
        }

        private static string ReadValue(string[] args, ref int index)
        {
            index++;
            if (index >= args.Length || String.IsNullOrWhiteSpace(args[index])) throw new ArgumentException("The request source argument requires a value.");
            return args[index];
        }
    }

    public sealed class RequestLoader
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public RequestLoader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<VideoAssemblyRequest> LoadAsync(WorkerInvocation invocation, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(invocation.RequestFile))
            {
                using var file = File.OpenRead(invocation.RequestFile);
                return await DeserializeAsync(file, cancellationToken);
            }

            using var response = await _httpClient.GetAsync(invocation.RequestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await DeserializeAsync(stream, cancellationToken);
        }

        private async Task<VideoAssemblyRequest> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            return await JsonSerializer.DeserializeAsync<VideoAssemblyRequest>(stream, _jsonOptions, cancellationToken) ?? throw new InvalidOperationException("The request document was empty or invalid.");
        }
    }
}
