using LagoVista.VideoAssembly.Contracts;
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

        public SignedUrlUploader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<long> UploadAsync(string filePath, VideoMediaImportDestination destination, CancellationToken cancellationToken = default, IProgress<SignedUrlUploadProgress> progress = null)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("The file to upload could not be found.", filePath);
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (String.IsNullOrWhiteSpace(destination.UploadUrl)) throw new InvalidOperationException("The signed upload URL is required.");

            var fileInfo = new FileInfo(filePath);
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
