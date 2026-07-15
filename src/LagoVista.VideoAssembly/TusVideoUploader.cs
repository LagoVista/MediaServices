using LagoVista.VideoAssembly.Contracts;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class TusVideoUploader
    {
        private const string TusVersion = "1.0.0";
        private readonly HttpClient _httpClient;
        private readonly VideoAssemblyOptions _options;

        public TusVideoUploader(HttpClient httpClient, VideoAssemblyOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task UploadAsync(string uploadUrl, string filePath, IProgress<VideoAssemblyProgress> progress, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(uploadUrl)) throw new ArgumentNullException(nameof(uploadUrl));
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("The assembled video file could not be found.", filePath);
            if (_options.TusChunkSizeBytes <= 0) throw new InvalidOperationException("TusChunkSizeBytes must be greater than zero.");

            var fileInfo = new FileInfo(filePath);
            var offset = await GetOffsetAsync(uploadUrl, cancellationToken);
            if (offset < 0 || offset > fileInfo.Length) throw new InvalidOperationException($"Vimeo returned an invalid TUS offset of {offset} for a file containing {fileInfo.Length} bytes.");

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            fileStream.Position = offset;

            while (offset < fileInfo.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunkLength = (int)Math.Min(_options.TusChunkSizeBytes, fileInfo.Length - offset);
                using var chunkStream = new LimitedReadStream(fileStream, chunkLength);
                using var content = new StreamContent(chunkStream, 131072);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");
                content.Headers.ContentLength = chunkLength;

                using var request = new HttpRequestMessage(new HttpMethod("PATCH"), uploadUrl);
                request.Headers.TryAddWithoutValidation("Tus-Resumable", TusVersion);
                request.Headers.TryAddWithoutValidation("Upload-Offset", offset.ToString(CultureInfo.InvariantCulture));
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"The Vimeo TUS upload failed with status {(int)response.StatusCode} at offset {offset}: {responseContent}");

                var returnedOffset = ReadOffset(response);
                var expectedOffset = offset + chunkLength;
                if (returnedOffset != expectedOffset) throw new InvalidOperationException($"Vimeo returned TUS offset {returnedOffset}, but offset {expectedOffset} was expected.");

                offset = returnedOffset;
                progress?.Report(new VideoAssemblyProgress
                {
                    Stage = VideoAssemblyStage.UploadingToVimeo,
                    PercentComplete = fileInfo.Length == 0 ? 100 : (int)Math.Min(100, offset * 100L / fileInfo.Length),
                    Message = "Uploading assembled video to Vimeo.",
                    BytesCompleted = offset,
                    BytesTotal = fileInfo.Length
                });
            }
        }

        private async Task<long> GetOffsetAsync(string uploadUrl, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, uploadUrl);
            request.Headers.TryAddWithoutValidation("Tus-Resumable", TusVersion);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"The Vimeo TUS offset request failed with status {(int)response.StatusCode}: {responseContent}");
            return ReadOffset(response);
        }

        private static long ReadOffset(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Upload-Offset", out var values)) throw new InvalidOperationException("The Vimeo TUS response did not contain an Upload-Offset header.");
            var value = String.Join(String.Empty, values);
            if (!Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset)) throw new InvalidOperationException($"The Vimeo TUS response contained invalid Upload-Offset value '{value}'.");
            return offset;
        }

        private sealed class LimitedReadStream : Stream
        {
            private readonly Stream _innerStream;
            private long _remaining;

            public LimitedReadStream(Stream innerStream, long length)
            {
                _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
                _remaining = length;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remaining <= 0) return 0;
                var bytesRead = _innerStream.Read(buffer, offset, (int)Math.Min(count, _remaining));
                _remaining -= bytesRead;
                return bytesRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_remaining <= 0) return 0;
                var bytesRead = await _innerStream.ReadAsync(buffer.Slice(0, (int)Math.Min(buffer.Length, _remaining)), cancellationToken);
                _remaining -= bytesRead;
                return bytesRead;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
