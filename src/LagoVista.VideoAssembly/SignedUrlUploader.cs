using LagoVista.VideoAssembly.Contracts;
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
using Minio;
using Minio.DataModel.Args;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class SignedUrlUploader
    {
        private readonly HttpClient _httpClient;
        private readonly IS3ObjectStorageConnectionSettings _s3Settings;
        private const long MultipartThreshold = 8L * 1024 * 1024;

        public SignedUrlUploader(HttpClient httpClient, IServiceProvider services)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _s3Settings = services?.GetService(typeof(IS3ObjectStorageConnectionSettings)) as IS3ObjectStorageConnectionSettings;
        }

        public async Task<long> UploadAsync(string filePath, VideoMediaImportDestination destination, CancellationToken cancellationToken = default, IProgress<SignedUrlUploadProgress> progress = null)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("The file to upload could not be found.", filePath);
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (String.IsNullOrWhiteSpace(destination.UploadUrl)) throw new InvalidOperationException("The signed upload URL is required.");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length >= MultipartThreshold)
            {
                if (_s3Settings == null)
                    throw new InvalidOperationException("S3ObjectStorage settings are required for multipart video uploads. Supply configuration through the worker ConfigMap and Secret.");
                return await UploadS3Async(filePath, destination, fileInfo.Length, cancellationToken, progress);
            }

            var uploadUri = new Uri(destination.UploadUrl);
            var safeDestination = uploadUri.GetLeftPart(UriPartial.Path);

            Console.WriteLine($"[OBJECT STORAGE UPLOAD START] File={fileInfo.Name}, Size={fileInfo.Length} bytes, ContentType={destination.ContentType ?? "application/octet-stream"}, Destination={safeDestination}");
            progress?.Report(new SignedUrlUploadProgress { BytesCompleted = 0, BytesTotal = fileInfo.Length, PercentComplete = 0 });

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var progressStream = new ProgressReadStream(fileStream, fileInfo.Length, progress);
            using var content = new StreamContent(progressStream, 131072);
            content.Headers.ContentLength = fileInfo.Length;
            content.Headers.ContentType = new MediaTypeHeaderValue(String.IsNullOrWhiteSpace(destination.ContentType) ? "application/octet-stream" : destination.ContentType);

            using var request = new HttpRequestMessage(HttpMethod.Put, destination.UploadUrl)
            {
                Content = content
            };

            try
            {
                Console.WriteLine($"[OBJECT STORAGE HTTP PUT] Sending {fileInfo.Length} bytes to {safeDestination}.");
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                Console.WriteLine($"[OBJECT STORAGE HTTP RESPONSE] Status={(int)response.StatusCode} {response.StatusCode}, BytesRead={progressStream.BytesRead}/{fileInfo.Length}, ResponseBody={Truncate(responseContent, 2048)}");

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Signed object-storage upload failed with status {(int)response.StatusCode} ({response.StatusCode}): {responseContent}");

                progress?.Report(new SignedUrlUploadProgress { BytesCompleted = fileInfo.Length, BytesTotal = fileInfo.Length, PercentComplete = 100 });
                Console.WriteLine($"[OBJECT STORAGE UPLOAD COMPLETED] {fileInfo.Length}/{fileInfo.Length} bytes uploaded to {safeDestination}.");
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OBJECT STORAGE UPLOAD FAILED] Destination={safeDestination}, BytesRead={progressStream.BytesRead}/{fileInfo.Length}, Exception={ex}");
                throw;
            }
        }

        private async Task<long> UploadS3Async(string filePath, VideoMediaImportDestination destination, long fileLength, CancellationToken cancellationToken, IProgress<SignedUrlUploadProgress> progress)
        {
            // The hosting service already supplies the canonical S3 bucket/object path in its signed URL.
            // Reuse that identity while authenticating directly with the worker's scoped S3 credentials.
            var uri = new Uri(destination.UploadUrl);
            var path = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            var separator = path.IndexOf('/');
            if (separator <= 0 || separator == path.Length - 1)
                throw new InvalidOperationException("S3 upload destination must contain a bucket and object key.");
            var bucket = path.Substring(0, separator);
            var objectKey = path.Substring(separator + 1);
            var client = new MinioClient()
                .WithEndpoint(_s3Settings.Host, _s3Settings.Port)
                .WithCredentials(_s3Settings.AccessKey, _s3Settings.SecretKey)
                .WithSSL(_s3Settings.UseTls)
                .Build();

            Console.WriteLine($"[S3 MULTIPART UPLOAD START] Bucket={bucket}, Key={objectKey}, Bytes={fileLength}.");
            progress?.Report(new SignedUrlUploadProgress { BytesCompleted = 0, BytesTotal = fileLength, PercentComplete = 0 });
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var progressStream = new ProgressReadStream(fileStream, fileLength, progress);
                var args = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithStreamData(progressStream)
                    .WithObjectSize(fileLength)
                    .WithContentType(String.IsNullOrWhiteSpace(destination.ContentType) ? "application/octet-stream" : destination.ContentType);
                await client.PutObjectAsync(args, cancellationToken);
                progress?.Report(new SignedUrlUploadProgress { BytesCompleted = fileLength, BytesTotal = fileLength, PercentComplete = 100 });
                Console.WriteLine($"[S3 MULTIPART UPLOAD COMPLETE] Bucket={bucket}, Key={objectKey}, Bytes={fileLength}.");
                return fileLength;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[S3 MULTIPART UPLOAD FAILED] Bucket={bucket}, Key={objectKey}, Exception={ex}");
                throw;
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (String.IsNullOrEmpty(value) || value.Length <= maxLength) return value ?? String.Empty;
            return value.Substring(0, maxLength) + "...";
        }
    }

    public sealed class SignedUrlUploadProgress
    {
        public long BytesCompleted { get; set; }
        public long BytesTotal { get; set; }
        public int PercentComplete { get; set; }
    }

    internal sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public InlineProgress(Action<T> report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Report(T value)
        {
            _report(value);
        }
    }

    internal sealed class ProgressReadStream : Stream
    {
        private const long ReportIntervalBytes = 1048576;
        private readonly Stream _innerStream;
        private readonly long _length;
        private readonly IProgress<SignedUrlUploadProgress> _progress;
        private long _bytesRead;
        private long _lastReportedBytes;

        public ProgressReadStream(Stream innerStream, long length, IProgress<SignedUrlUploadProgress> progress)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _length = length;
            _progress = progress;
        }

        public long BytesRead => _bytesRead;

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _innerStream.Read(buffer, offset, count);
            Report(bytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            Report(bytesRead);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
            Report(bytesRead);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _innerStream.Dispose();
            base.Dispose(disposing);
        }

        private void Report(int bytesRead)
        {
            if (bytesRead <= 0) return;

            _bytesRead += bytesRead;
            if (_bytesRead < _length && _bytesRead - _lastReportedBytes < ReportIntervalBytes) return;

            _lastReportedBytes = _bytesRead;
            var percentComplete = _length <= 0 ? 100 : (int)Math.Min(100, _bytesRead * 100L / _length);
            Console.WriteLine($"[OBJECT STORAGE UPLOAD PROGRESS] {_bytesRead}/{_length} bytes ({percentComplete}%).");
            _progress?.Report(new SignedUrlUploadProgress { BytesCompleted = _bytesRead, BytesTotal = _length, PercentComplete = percentComplete });
        }
    }
}
