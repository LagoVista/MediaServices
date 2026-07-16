using k8s;
using k8s.Models;
using LagoVista.Core;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Services
{
    public sealed class KubernetesVideoProcessorLauncher : IVideoProcessorLauncher
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly VideoProcessorLauncherOptions _options;

        public KubernetesVideoProcessorLauncher(IKubernetes kubernetesClient, VideoProcessorLauncherOptions options)
        {
            _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }


        public async Task<InvokeResult<VideoProcessorLaunchResult>> LaunchAsync(VideoProcessorLaunchRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateRequest(request);
            if (!validationResult.Successful)
            {
                return validationResult.ToInvokeResult<VideoProcessorLaunchResult>();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var workerImageResult = await ResolveWorkerImageAsync(cancellationToken);
                if (!workerImageResult.Successful)
                {
                    return workerImageResult.ToInvokeResult<VideoProcessorLaunchResult>();
                }

                var jobName = CreateJobName(request.AttemptId);
                var yaml = LoadJobTemplate();
                yaml = ApplyTemplateValues(yaml, request, jobName, workerImageResult.Result);

                var job = KubernetesYaml.Deserialize<V1Job>(yaml);
                if (job == null)
                {
                    return InvokeResult<VideoProcessorLaunchResult>.FromError("The embedded video processor Kubernetes Job template could not be deserialized.");
                }

                var createdJob = await _kubernetesClient.BatchV1.CreateNamespacedJobAsync(job, _options.Namespace, cancellationToken: cancellationToken);
                var createdJobName = createdJob?.Metadata?.Name;

                return InvokeResult<VideoProcessorLaunchResult>.Create(new VideoProcessorLaunchResult
                {
                    Provider = "kubernetes",
                    LaunchId = createdJob?.Metadata?.Uid,
                    Namespace = _options.Namespace,
                    JobName = String.IsNullOrWhiteSpace(createdJobName) ? jobName : createdJobName,
                    LaunchedUtc = UtcTimestamp.Now
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return InvokeResult<VideoProcessorLaunchResult>.FromException("Could not launch the video processor Kubernetes Job.", ex);
            }
        }

        private async Task<InvokeResult<string>> ResolveWorkerImageAsync(CancellationToken cancellationToken)
        {
            var configMap = await _kubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(_options.ConfigMapName, _options.Namespace, cancellationToken: cancellationToken);
            if (configMap?.Data == null || !configMap.Data.TryGetValue(_options.WorkerImageConfigKey, out var workerImage) || String.IsNullOrWhiteSpace(workerImage))
            {
                return InvokeResult<string>.FromError($"Kubernetes ConfigMap '{_options.Namespace}/{_options.ConfigMapName}' does not contain a non-empty '{_options.WorkerImageConfigKey}' value.");
            }

            return InvokeResult<string>.Create(workerImage.Trim());
        }

        private string LoadJobTemplate()
        {
            var assembly = typeof(KubernetesVideoProcessorLauncher).GetTypeInfo().Assembly;

            using (var stream = assembly.GetManifestResourceStream(_options.JobTemplateResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Could not find embedded Kubernetes Job template '{_options.JobTemplateResourceName}'.");
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private string ApplyTemplateValues(string yaml, VideoProcessorLaunchRequest request, string jobName, string workerImage)
        {
            return yaml
                .Replace("video-processor-replace-with-attempt-id", jobName)
                .Replace("replace-with-job-type", ToLabelValue(request.JobType.ToString()))
                .Replace("replace-with-production-id", ToLabelValue(request.ProductionId))
                .Replace("replace-with-request-id", ToLabelValue(request.RequestId))
                .Replace("replace-with-attempt-id", ToLabelValue(request.AttemptId))
                .Replace("replace-with-worker-image", ToSingleQuotedYamlValue(workerImage))
                .Replace("replace-with-request-read-sas-url", ToSingleQuotedYamlValue(request.RequestUrl))
                .Replace("namespace: video-processing", $"namespace: {_options.Namespace}");
        }

        private static InvokeResult ValidateRequest(VideoProcessorLaunchRequest request)
        {
            if (request == null)
            {
                return InvokeResult.FromError("Video processor launch request is required.");
            }

            if (String.IsNullOrWhiteSpace(request.ProductionId))
            {
                return InvokeResult.FromError("Video processor production ID is required.");
            }

            if (String.IsNullOrWhiteSpace(request.RequestId))
            {
                return InvokeResult.FromError("Video processor request ID is required.");
            }

            if (String.IsNullOrWhiteSpace(request.AttemptId))
            {
                return InvokeResult.FromError("Video processor attempt ID is required.");
            }

            if (!Uri.TryCreate(request.RequestUrl, UriKind.Absolute, out var requestUri) || !String.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult.FromError("Video processor request URL must be an absolute HTTPS URL.");
            }

            return InvokeResult.Success;
        }

        private static string CreateJobName(string attemptId)
        {
            var suffix = ToDnsLabel(attemptId);
            var jobName = $"video-processor-{suffix}";
            return jobName.Length <= 63 ? jobName : jobName.Substring(0, 63).TrimEnd('-');
        }

        private static string ToDnsLabel(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "job";
            }

            var builder = new StringBuilder(value.Length);

            foreach (var character in value.ToLowerInvariant())
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-')
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append('-');
                }
            }

            var result = builder.ToString().Trim('-');
            return String.IsNullOrWhiteSpace(result) ? "job" : result;
        }

        private static string ToLabelValue(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var builder = new StringBuilder(value.Length);

            foreach (var character in value.ToLowerInvariant())
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-' || character == '_' || character == '.')
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append('-');
                }
            }

            var result = builder.ToString().Trim('-', '_', '.');
            if (String.IsNullOrWhiteSpace(result))
            {
                return "unknown";
            }

            return result.Length <= 63 ? result : result.Substring(0, 63).TrimEnd('-', '_', '.');
        }

        private static string ToSingleQuotedYamlValue(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }
    }
}
