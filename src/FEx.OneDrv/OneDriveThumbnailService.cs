using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using Microsoft.Graph.Models;
using Polly;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

/// <summary>Graph thumbnail downloader with an on-disk cache keyed by item ID and size.</summary>
public sealed class OneDriveThumbnailService : IOneDriveThumbnailService
{
    private static readonly HttpClient SharedHttpClient = new();

    private readonly IGraphServiceClientCache _graphCache;
    private readonly IFExLogger _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    public OneDriveThumbnailService(IGraphServiceClientCache graphCache, OneDriveOptions options, IFExLogger logger)
        : this(graphCache, options, logger, SharedHttpClient)
    {
    }

    internal OneDriveThumbnailService(IGraphServiceClientCache graphCache,
                                      OneDriveOptions options,
                                      IFExLogger logger,
                                      HttpClient httpClient)
    {
        _graphCache = graphCache ?? throw new ArgumentNullException(nameof(graphCache));
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _pipeline = GraphResiliencePipeline.Create(_logger);
        _cacheDir = string.IsNullOrWhiteSpace(options.TokenCachePath)
            ? GetDefaultCacheDir()
            : Path.Combine(Path.GetDirectoryName(options.TokenCachePath) ?? string.Empty, "thumbnails");
    }

    public Task<byte[]?> GetThumbnailAsync(string itemId, CancellationToken cancellationToken) =>
        GetThumbnailAsync(itemId, ThumbnailSize.Medium, cancellationToken);

    public async Task<byte[]?> GetThumbnailAsync(string itemId, ThumbnailSize size, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentNullException(nameof(itemId));

        var cacheStem = GetCacheStem(itemId, size);
        var cached = await TryReadCachedAsync(cacheStem, cancellationToken);

        if (cached != null)
            return cached;

        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);
        var thumbnails = await _pipeline.ExecuteAsync(async cancelToken =>
                await client.Drives[driveId].Items[itemId].Thumbnails.GetAsync(cancellationToken: cancelToken),
            cancellationToken);

        var url = thumbnails?.Value?.Count > 0
            ? SelectSize(thumbnails.Value[0], size)?.Url
            : null;

        if (url == null)
            return null;

        var (bytes, mediaType) = await _pipeline.ExecuteAsync(async cancelToken =>
            {
                using var resp = await _httpClient.GetAsync(url, cancelToken);
                resp.EnsureSuccessStatusCode();

#if NETSTANDARD
                var content = await resp.Content.ReadAsByteArrayAsync();
#else
                var content = await resp.Content.ReadAsByteArrayAsync(cancelToken);
#endif

                return (content, resp.Content.Headers.ContentType?.MediaType);
            },
            cancellationToken);

        var cachePath = Path.Combine(_cacheDir, $"{cacheStem}.{GetExtension(mediaType)}");
        Directory.CreateDirectory(_cacheDir);

        await TryWriteCacheAsync(cachePath, bytes, cancellationToken);

        _logger.Information($"Cached thumbnail for item {itemId}");

        return bytes;
    }

    private static Thumbnail? SelectSize(ThumbnailSet set, ThumbnailSize size) =>
        size switch
        {
            ThumbnailSize.Small => set.Small,
            ThumbnailSize.Large => set.Large,
            _ => set.Medium
        };

    // The stem is the lowercase hex SHA-256 of the item ID: a fixed alphabet, so neither wildcards nor characters that
    // are illegal in file names can ever reach the file system or the search pattern, and raw Graph IDs stay out of
    // file names.
    internal static string GetCacheStem(string itemId, ThumbnailSize size)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(itemId));
        var hex = string.Concat(hash.Select(x => x.ToString("x2")));

        return $"{hex}.{size.ToString().ToLowerInvariant()}";
    }

    // Per-user location: a machine-wide temp directory would be readable (and pre-creatable) by other local users.
    internal static string GetDefaultCacheDir()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrEmpty(root))
            root = Path.GetTempPath();

        return Path.Combine(root, "FEx.OneDrv", "thumbnails");
    }

    internal static string GetExtension(string? mediaType) =>
        mediaType?.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/bmp" => "bmp",
            "image/jpeg" or "image/jpg" => "jpg",
            _ => "bin"
        };

    // Best effort: the bytes are already downloaded, so a failed cache write must not fail the call. Each write uses its
    // own temp file (concurrent callers for the same item must not share a handle) and moves it into place, so a
    // cancelled or crashed write can never leave a truncated file that later passes for a cache hit.
    private async Task TryWriteCacheAsync(string cachePath, byte[] bytes, CancellationToken cancellationToken)
    {
        var tempPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";

        try
        {
#if NETSTANDARD
            await Task.Run(() => File.WriteAllBytes(tempPath, bytes), cancellationToken);

            if (File.Exists(cachePath))
                File.Delete(cachePath);

            File.Move(tempPath, cachePath);
#else
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            File.Move(tempPath, cachePath, true);
#endif
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warning($"Could not cache thumbnail at {cachePath}: {ex.Message}");

            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
                // Nothing more to do; a stray .tmp file is never served.
            }
        }
    }

    // An empty file or one that is locked by a concurrent writer counts as a miss, never as data or an error.
    private async Task<byte[]?> TryReadCachedAsync(string cacheStem, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_cacheDir))
            return null;

        var path = Directory.EnumerateFiles(_cacheDir, $"{cacheStem}.*")
            .FirstOrDefault(f => !f.EndsWith(".tmp", StringComparison.Ordinal));

        if (path is null)
            return null;

        try
        {
#if NETSTANDARD
            var bytes = await Task.Run(() => File.ReadAllBytes(path), cancellationToken);
#else
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
#endif

            return bytes.Length == 0 ? null : bytes;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
