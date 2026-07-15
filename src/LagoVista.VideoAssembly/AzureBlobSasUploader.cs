using LagoVista.VideoAssembly.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class AzureBlobSasUploader
    {
        private readonly HttpClient _httpClient;

        public AzureBlobSasUploader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<long> UploadAsync(string filePath, VideoMediaImportDestination destination, CancellationToken cancellationToken = default, IProgress<AzureBlobUploadProgress> progress = null)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("The file to upload could not be found.", filePath);
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (String.IsNullOrWhiteSpace(destination.UploadUrl)) throw new InvalidOperationException("The Azure Blob SAS upload URL is required.");

            var fileInfo = new FileInfo(filePath);
            progress?.Report(new AzureBlobUploadProgress { BytesCompleted = 0, BytesTotal = fileInfo.Length, PercentComplete = 0 });

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var progressStream = new ProgressReadStream(fileStream, fileInfo.Length, progress);
            using var content = new StreamContent(progressStream, 131072);
            content.Headers.ContentLength = fileInfo.Length;
            content.Headers.ContentType = new MediaTypeHeaderValue(String.IsNullOrWhiteSpace(destination.ContentType) ? "application/octet-stream" : destination.ContentType);

            using var request = new HttpRequestMessage(HttpMethod.Put, destination.UploadUrl);
            request.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
            request.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Azure Blob upload failed with status {(int)response.StatusCode}: {responseContent}");

            progress?.Report(new AzureBlobUploadProgress { BytesCompleted = fileInfo.Length, BytesTotal = fileInfo.Length, PercentComplete = 100 });
            return fileInfo.Length;
        }
    }

    public sealed class AzureBlobUploadProgress
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
        private readonly IProgress<AzureBlobUploadProgress> _progress;
        private long _bytesRead;
        private long _lastReportedBytes;

        public ProgressReadStream(Stream innerStream, long length, IProgress<AzureBlobUploadProgress> progress)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _length = length;
            _progress = progress;
        }

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
            _progress?.Report(new AzureBlobUploadProgress { BytesCompleted = _bytesRead, BytesTotal = _length, PercentComplete = percentComplete });
        }
    }
}
