using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.Managers
{
    /// <summary>
    /// Temporary image assembly. No MediaResource exists until PublishAsync succeeds.
    /// The original canvas is reloaded and overwritten through provider-neutral cloud storage.
    /// </summary>
    public sealed class ImageAssemblyManager : IImageAssemblyManager
    {
        private const string ContainerPrefix = "media-image-assembly-";
        private const int MaxDimension = 4096;
        private const int MaxTileDimension = 512;
        private const int MaxTileBytes = 1024 * 1024;
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly ICloudFileStorageClient _storage;
        private readonly IMediaServicesManager _media;

        public ImageAssemblyManager(ICloudFileStorageClient storage, IMediaServicesManager media)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _media = media ?? throw new ArgumentNullException(nameof(media));
        }

        private sealed class Rect
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private sealed class AssemblyState
        {
            public string Id { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public DateTime CreatedUtc { get; set; }
            public string PublishedResourceId { get; set; }
            public List<Rect> Regions { get; set; } = new List<Rect>();
        }

        private static string Container(EntityHeader org) => ContainerPrefix + org.Id.ToLowerInvariant();
        private static string Canvas(string id) => id + ".png";
        private static string Manifest(string id) => id + ".json";

        private static bool ValidId(string id) =>
            !String.IsNullOrEmpty(id) && id.Length == 32 && id.All(c =>
                (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

        private async Task<AssemblyState> LoadAsync(string id, EntityHeader org)
        {
            var result = await _storage.GetFileAsync(Container(org), Manifest(id));
            if (!result.Successful) return null;
            return JsonConvert.DeserializeObject<AssemblyState>(System.Text.Encoding.UTF8.GetString(result.Result));
        }

        private async Task<InvokeResult> SaveAsync(AssemblyState state, EntityHeader org)
        {
            var result = await _storage.AddFileAsync(Container(org), Manifest(state.Id),
                JsonConvert.SerializeObject(state), "application/json");
            return result.ToInvokeResult();
        }

        private static ImageAssemblyStatus Status(AssemblyState state) => new ImageAssemblyStatus
        {
            Id = state.Id, Width = state.Width, Height = state.Height,
            Blocks = state.Regions.Count, Complete = IsCovered(state)
        };

        private static bool IsCovered(AssemblyState state)
        {
            var covered = new bool[state.Width * state.Height];
            foreach (var rect in state.Regions)
                for (var y = rect.Y; y < rect.Y + rect.Height; y++)
                    for (var x = rect.X; x < rect.X + rect.Width; x++)
                        covered[y * state.Width + x] = true;
            return covered.All(value => value);
        }

        public async Task<InvokeResult<ImageAssemblyStatus>> CreateAsync(int width, int height, EntityHeader org)
        {
            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension ||
                (long)width * height > 8_400_000)
                return InvokeResult<ImageAssemblyStatus>.FromError("Invalid canvas dimensions (maximum 4096 per side, 8.4 million pixels).");

            var state = new AssemblyState
            {
                Id = Guid.NewGuid().ToString("N"),
                Width = width,
                Height = height,
                CreatedUtc = DateTime.UtcNow
            };

            using (var canvas = new Image<Rgba32>(width, height))
            using (var output = new MemoryStream())
            {
                canvas.SaveAsPng(output);
                var stored = await _storage.AddFileAsync(Container(org), Canvas(state.Id),
                    output.ToArray(), "image/png", rejectUpdates: true);
                if (!stored.Successful)
                    return InvokeResult<ImageAssemblyStatus>.FromInvokeResult(stored.ToInvokeResult());
            }

            var saved = await SaveAsync(state, org);
            if (!saved.Successful)
                return InvokeResult<ImageAssemblyStatus>.FromInvokeResult(saved);
            return InvokeResult<ImageAssemblyStatus>.Create(Status(state));
        }

        public async Task<InvokeResult<ImageAssemblyStatus>> AppendAsync(string id, int x, int y, string imageData, EntityHeader org)
        {
            if (!ValidId(id)) return InvokeResult<ImageAssemblyStatus>.FromError("Invalid image assembly ID.");
            if (String.IsNullOrWhiteSpace(imageData) || imageData.Length > MaxTileBytes * 4 / 3 + 8)
                return InvokeResult<ImageAssemblyStatus>.FromError("Tile data missing or exceeds 1 MiB.");

            byte[] tileBytes;
            try { tileBytes = Convert.FromBase64String(imageData); }
            catch (FormatException) { return InvokeResult<ImageAssemblyStatus>.FromError("Tile is not valid base64."); }
            if (tileBytes.Length > MaxTileBytes)
                return InvokeResult<ImageAssemblyStatus>.FromError("Tile exceeds 1 MiB.");

            var gate = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                var state = await LoadAsync(id, org);
                if (state == null || state.CreatedUtc + Lifetime < DateTime.UtcNow || state.PublishedResourceId != null)
                    return InvokeResult<ImageAssemblyStatus>.FromError("Image assembly not found, expired, or already published.");

                using (var tile = Image.Load<Rgba32>(tileBytes))
                {
                    if (tile.Width > MaxTileDimension || tile.Height > MaxTileDimension ||
                        x < 0 || y < 0 || (long)x + tile.Width > state.Width || (long)y + tile.Height > state.Height)
                        return InvokeResult<ImageAssemblyStatus>.FromError("Tile dimensions or location are outside the canvas.");

                    var existing = await _storage.GetFileAsync(Container(org), Canvas(id));
                    if (!existing.Successful)
                        return InvokeResult<ImageAssemblyStatus>.FromInvokeResult(existing.ToInvokeResult());

                    using (var canvas = Image.Load<Rgba32>(existing.Result))
                    {
                        if (canvas.Width != state.Width || canvas.Height != state.Height)
                            return InvokeResult<ImageAssemblyStatus>.FromError("Stored canvas dimensions do not match the assembly.");

                        canvas.Mutate(ctx => ctx.DrawImage(tile, new Point(x, y), 1f));
                        using (var output = new MemoryStream())
                        {
                            canvas.SaveAsPng(output);
                            var stored = await _storage.AddFileAsync(Container(org), Canvas(id),
                                output.ToArray(), "image/png");
                            if (!stored.Successful)
                                return InvokeResult<ImageAssemblyStatus>.FromInvokeResult(stored.ToInvokeResult());
                        }
                    }

                    state.Regions.Add(new Rect { X = x, Y = y, Width = tile.Width, Height = tile.Height });
                    var saved = await SaveAsync(state, org);
                    if (!saved.Successful)
                        return InvokeResult<ImageAssemblyStatus>.FromInvokeResult(saved);
                }

                return InvokeResult<ImageAssemblyStatus>.Create(Status(state));
            }
            finally { gate.Release(); }
        }

        public async Task<InvokeResult<MediaResource>> PublishAsync(string id, string name, string mediaLibraryId,
            bool isPublic, EntityHeader org, EntityHeader user)
        {
            if (!ValidId(id)) return InvokeResult<MediaResource>.FromError("Invalid image assembly ID.");

            var gate = Locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                var state = await LoadAsync(id, org);
                if (state == null || state.CreatedUtc + Lifetime < DateTime.UtcNow || state.PublishedResourceId != null)
                    return InvokeResult<MediaResource>.FromError("Image assembly not found, expired, or already published.");
                if (!IsCovered(state))
                    return InvokeResult<MediaResource>.FromError("Image assembly is incomplete.");

                var canvas = await _storage.GetFileAsync(Container(org), Canvas(id));
                if (!canvas.Successful)
                    return InvokeResult<MediaResource>.FromInvokeResult(canvas.ToInvokeResult());

                // Stable resource ID across publish retries prevents accidental duplicate records.
                var resourceId = id.ToUpperInvariant();
                using (var stream = new MemoryStream(canvas.Result, false))
                {
                    var result = await _media.AddResourceMediaAsync(resourceId, stream,
                        (String.IsNullOrWhiteSpace(name) ? "assembled-image" : name) + ".png",
                        "image/png", org, user, saveResourceRecord: true, isPublic: isPublic,
                        resourceName: name, mediaLibraryId: mediaLibraryId);
                    if (!result.Successful) return result;

                    state.PublishedResourceId = resourceId;
                    var saved = await SaveAsync(state, org);
                    if (!saved.Successful) return InvokeResult<MediaResource>.FromInvokeResult(saved);
                    await _storage.DeleteFileAsync(Container(org), Canvas(id));
                    return result;
                }
            }
            finally { gate.Release(); }
        }
    }
}
