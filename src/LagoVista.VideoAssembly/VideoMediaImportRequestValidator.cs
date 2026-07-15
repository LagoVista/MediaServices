using LagoVista.VideoAssembly.Contracts;
using System;
using System.Collections.Generic;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoMediaImportRequestValidator
    {
        public IReadOnlyList<string> Validate(VideoMediaImportRequest request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("The video media import request is required.");
                return errors;
            }

            if (!String.Equals(request.Version, VideoAssemblyContractVersions.Current, StringComparison.Ordinal)) errors.Add($"Unsupported contract version '{request.Version}'.");
            if (request.JobType != VideoProcessorJobType.VideoMediaImport) errors.Add("JobType must be VideoMediaImport.");
            if (String.IsNullOrWhiteSpace(request.RequestId)) errors.Add("RequestId is required.");
            if (String.IsNullOrWhiteSpace(request.AttemptId)) errors.Add("AttemptId is required.");
            if (String.IsNullOrWhiteSpace(request.ProductionId)) errors.Add("ProductionId is required.");
            if (String.IsNullOrWhiteSpace(request.MediaResourceId)) errors.Add("MediaResourceId is required.");

            ValidateSource(request.Source, errors);
            ValidateDestination(request.VideoDestination, "VideoDestination", errors);

            var executionOptions = request.ExecutionOptions ?? new VideoMediaImportExecutionOptions();
            var thumbnailEnabled = executionOptions.GenerateThumbnail && request.Thumbnail?.Enabled != false;
            if (thumbnailEnabled)
            {
                if (request.Thumbnail == null) errors.Add("Thumbnail settings are required when thumbnail generation is enabled.");
                else
                {
                    if (request.Thumbnail.TimeSeconds.HasValue && request.Thumbnail.TimeSeconds.Value < 0) errors.Add("Thumbnail.TimeSeconds cannot be negative.");
                    ValidateDestination(request.Thumbnail.Destination, "Thumbnail.Destination", errors);
                }
            }

            if (executionOptions.SendCallbacks)
            {
                if (request.Callback == null)
                {
                    errors.Add("Callback settings are required when SendCallbacks is enabled.");
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(request.Callback.Url) && String.IsNullOrWhiteSpace(request.Callback.Path)) errors.Add("Callback.Url or Callback.Path is required when SendCallbacks is enabled.");
                    if (!String.IsNullOrWhiteSpace(request.Callback.Url) && !IsHttpUrl(request.Callback.Url)) errors.Add("Callback.Url must be an absolute HTTP or HTTPS URL.");
                    if (!String.IsNullOrWhiteSpace(request.Callback.Path) && !request.Callback.Path.StartsWith("/", StringComparison.Ordinal)) errors.Add("Callback.Path must begin with '/'.");
                    if (String.IsNullOrWhiteSpace(request.Callback.AccessToken)) errors.Add("Callback.AccessToken is required when SendCallbacks is enabled.");
                }
            }

            if (request.Limits == null)
            {
                errors.Add("Limits are required.");
            }
            else
            {
                if (request.Limits.MaxSourceFileBytes <= 0) errors.Add("Limits.MaxSourceFileBytes must be greater than zero.");
                if (request.Limits.MaxExecutionMinutes <= 0) errors.Add("Limits.MaxExecutionMinutes must be greater than zero.");
            }

            return errors;
        }

        private static void ValidateSource(VideoAssemblySource source, List<string> errors)
        {
            if (source == null)
            {
                errors.Add("Source is required.");
                return;
            }

            if (!IsHttpUrl(source.Url)) errors.Add("Source.Url must be an absolute HTTP or HTTPS URL.");
            if (String.IsNullOrWhiteSpace(source.FileName)) errors.Add("Source.FileName is required.");
            if (String.IsNullOrWhiteSpace(source.ContentType)) errors.Add("Source.ContentType is required.");
        }

        private static void ValidateDestination(VideoMediaImportDestination destination, string name, List<string> errors)
        {
            if (destination == null)
            {
                errors.Add($"{name} is required.");
                return;
            }

            if (!IsHttpUrl(destination.UploadUrl)) errors.Add($"{name}.UploadUrl must be an absolute HTTP or HTTPS URL.");
            if (String.IsNullOrWhiteSpace(destination.StorageReferenceName)) errors.Add($"{name}.StorageReferenceName is required.");
            if (String.IsNullOrWhiteSpace(destination.FileName)) errors.Add($"{name}.FileName is required.");
            if (String.IsNullOrWhiteSpace(destination.ContentType)) errors.Add($"{name}.ContentType is required.");
        }

        private static bool IsHttpUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
