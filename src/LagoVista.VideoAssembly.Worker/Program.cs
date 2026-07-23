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
            var notificationSettings = builder.Configuration.GetSection("RabbitNotifications").Get<VideoProcessorNotificationSettings>() ?? new VideoProcessorNotificationSettings();

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(notificationSettings);
            builder.Services.AddSingleton<VideoProcessorNotificationPublisher>();
            builder.Services.AddSingleton<VideoAssemblyRequestValidator>();
            builder.Services.AddSingleton<VideoMediaImportRequestValidator>();
            builder.Services.AddSingleton<VideoAssemblyWorkspaceFactory>();
            builder.Services.AddSingleton<ProcessRunner>();
            builder.Services.AddSingleton<AssSubtitleDocumentBuilder>();
            builder.Services.AddSingleton<FfprobeMediaInspectionService>();
            builder.Services.AddSingleton<TransparentVideoCropper>();
            builder.Services.AddSingleton<VideoThumbnailExtractor>();
            builder.Services.AddHttpClient<VideoProcessorRequestLoader>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddHttpClient<VideoAssemblySourceDownloader>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddHttpClient<VideoMediaImportService>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddHttpClient<AzureBlobSasUploader>(client => client.Timeout = Timeout.InfiniteTimeSpan);
            builder.Services.AddHttpClient<VideoProcessorCallbackClient>(client => client.Timeout = TimeSpan.FromSeconds(options.CallbackTimeoutSeconds));
            builder.Services.AddHttpClient<VimeoUploadSessionClient>(client => client.Timeout = TimeSpan.FromMinutes(options.HttpTimeoutMinutes));
            builder.Services.AddHttpClient<TusVideoUploader>(client => client.Timeout = Timeout.InfiniteTimeSpan);
            builder.Services.AddSingleton<IVideoAssemblyService, FfmpegVideoAssemblyService>();

            using var host = builder.Build();
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };

            try
            {
                var loadedRequest = await host.Services.GetRequiredService<VideoProcessorRequestLoader>().LoadAsync(invocation, cancellation.Token);
                switch (loadedRequest.JobType)
                {
                    case VideoProcessorJobType.VideoAssembly:
                        return await ExecuteAssemblyAsync(host.Services, loadedRequest.VideoAssemblyRequest, cancellation.Token);
                    case VideoProcessorJobType.VideoMediaImport:
                        return await ExecuteMediaImportAsync(host.Services, loadedRequest.VideoMediaImportRequest, cancellation.Token);
                    default:
                        Console.Error.WriteLine($"Unsupported video processor job type '{loadedRequest.JobType}'.");
                        return 4;
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Video processing was cancelled.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 3;
            }
        }

        private static async Task<int> ExecuteAssemblyAsync(IServiceProvider services, VideoAssemblyRequest request, CancellationToken cancellationToken)
        {
            var errors = services.GetRequiredService<VideoAssemblyRequestValidator>().Validate(request);
            if (errors.Count > 0)
            {
                Console.Error.WriteLine(String.Join(Environment.NewLine, errors));
                return 4;
            }

            Console.WriteLine($"Loaded assembly request '{request.RequestId}', attempt '{request.AttemptId}', production '{request.ProductionId}'.");
            var callbackReporter = new VideoAssemblyCallbackReporter(request, services.GetRequiredService<VideoProcessorCallbackClient>(), services.GetRequiredService<VideoProcessorNotificationPublisher>(), cancellationToken);
            await callbackReporter.SendStartedAsync();

            var result = await services.GetRequiredService<IVideoAssemblyService>().AssembleAsync(request, callbackReporter, cancellationToken);
            if (!result.Successful)
            {
                await callbackReporter.SendFailedAsync(result.ErrorMessage);
                Console.Error.WriteLine(result.ErrorMessage);
                return 1;
            }

            await callbackReporter.SendCompletedAsync(result);
            Console.WriteLine($"Output: {result.OutputFilePath}");
            Console.WriteLine($"Duration: {result.OutputDurationSeconds} seconds");
            Console.WriteLine($"Size: {result.OutputSizeBytes} bytes");
            Console.WriteLine($"SHA-256: {result.Sha256}");
            if (!String.IsNullOrWhiteSpace(result.VimeoVideoUri)) Console.WriteLine($"Vimeo URI: {result.VimeoVideoUri}");
            if (!String.IsNullOrWhiteSpace(result.VimeoVideoId)) Console.WriteLine($"Vimeo ID: {result.VimeoVideoId}");
            return 0;
        }

        private static async Task<int> ExecuteMediaImportAsync(IServiceProvider services, VideoMediaImportRequest request, CancellationToken cancellationToken)
        {
            var errors = services.GetRequiredService<VideoMediaImportRequestValidator>().Validate(request);
            if (errors.Count > 0)
            {
                Console.Error.WriteLine(String.Join(Environment.NewLine, errors));
                return 4;
            }

            Console.WriteLine($"Loaded media import request '{request.RequestId}', attempt '{request.AttemptId}', production '{request.ProductionId}'.");
            var result = await services.GetRequiredService<VideoMediaImportService>().ExecuteAsync(request, cancellationToken);
            if (!result.Successful)
            {
                Console.Error.WriteLine(result.ErrorMessage);
                return 1;
            }

            foreach (var output in result.Outputs) Console.WriteLine($"{output.Type}: {output.StorageReferenceName} ({output.SizeBytes} bytes, SHA-256 {output.Sha256})");
            return 0;
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

    public sealed class LoadedVideoProcessorRequest
    {
        public VideoProcessorJobType JobType { get; set; }
        public VideoAssemblyRequest VideoAssemblyRequest { get; set; }
        public VideoMediaImportRequest VideoMediaImportRequest { get; set; }
    }

    public sealed class VideoProcessorRequestLoader
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public VideoProcessorRequestLoader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<LoadedVideoProcessorRequest> LoadAsync(WorkerInvocation invocation, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(invocation.RequestFile))
            {
                var json = await File.ReadAllTextAsync(invocation.RequestFile, cancellationToken);
                return Deserialize(json);
            }

            using var response = await _httpClient.GetAsync(invocation.RequestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Deserialize(jsonContent);
        }

        private LoadedVideoProcessorRequest Deserialize(string json)
        {
            using var document = JsonDocument.Parse(json);
            var jobType = ResolveJobType(document.RootElement);

            switch (jobType)
            {
                case VideoProcessorJobType.VideoAssembly:
                    return new LoadedVideoProcessorRequest { JobType = jobType, VideoAssemblyRequest = JsonSerializer.Deserialize<VideoAssemblyRequest>(json, _jsonOptions) ?? throw new InvalidOperationException("The video assembly request was empty or invalid.") };
                case VideoProcessorJobType.VideoMediaImport:
                    return new LoadedVideoProcessorRequest { JobType = jobType, VideoMediaImportRequest = JsonSerializer.Deserialize<VideoMediaImportRequest>(json, _jsonOptions) ?? throw new InvalidOperationException("The video media import request was empty or invalid.") };
                default:
                    throw new InvalidOperationException($"Unsupported video processor job type '{jobType}'.");
            }
        }

        private static VideoProcessorJobType ResolveJobType(JsonElement root)
        {
            if (!root.TryGetProperty("jobType", out var jobTypeElement)) return VideoProcessorJobType.VideoAssembly;
            if (jobTypeElement.ValueKind == JsonValueKind.Number && jobTypeElement.TryGetInt32(out var numericValue)) return (VideoProcessorJobType)numericValue;
            if (jobTypeElement.ValueKind == JsonValueKind.String && Enum.TryParse<VideoProcessorJobType>(jobTypeElement.GetString(), true, out var parsedValue)) return parsedValue;
            throw new InvalidOperationException("The request contains an invalid jobType value.");
        }
    }
}
