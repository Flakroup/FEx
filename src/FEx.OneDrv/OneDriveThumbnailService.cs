using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using System;
using Microsoft.Graph.Models;
using Polly;
using System.IO;
using System.Linq;
using System.Net.Http;
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
            ? Path.Combine(Path.GetTempPath(), "FEx.OneDrv", "thumbnails")
            : Path.Combine(Path.GetDirectoryName(options.TokenCachePath) ?? string.Empty, "thumbnails");
    }

    public Task<byte[]?> GetThumbnailAsync(string itemId, CancellationToken cancellationToken) =>
        GetThumbnailAsync(itemId, ThumbnailSize.Medium, cancellationToken);

    public async Task<byte[]?> GetThumbnailAsync(string itemId, ThumbnailSize size, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentNullException(nameof(itemId));

        var cacheStem = GetCacheStem(itemId, size);
        var cachePath = FindCachedFile(cacheStem);

        if (cachePath != null)
        {
#if NETSTANDARD
            return await Task.Run(() => File.ReadAllBytes(cachePath), cancellationToken);
#else
            return await File.ReadAllBytesAsync(cachePath, cancellationToken);
#endif
        }

        var (client, driveId) = await _graphCache.GetAsync(cancellationToken);
        var pipeline = _pipeline;

        var thumbnails = await pipeline.ExecuteAsync(async cancelToken =>
                await client.Drives[driveId].Items[itemId].Thumbnails.GetAsync(cancellationToken: cancelToken),
            cancellationToken);

        var url = thumbnails?.Value?.Count > 0
            ? SelectSize(thumbnails.Value[0], size)?.Url
            : null;

        if (url == null)
            return null;

        var (bytes, mediaType) = await pipeline.ExecuteAsync(async cancelToken =>
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

        cachePath = Path.Combine(_cacheDir, $"{cacheStem}.{GetExtension(mediaType)}");
        Directory.CreateDirectory(_cacheDir);

#if NETSTANDARD
        await Task.Run(() => File.WriteAllBytes(cachePath, bytes), cancellationToken);
#else
        await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken);
#endif
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

    // Graph IDs can contain characters that are illegal in file names (e.g. '/'), so they are never used verbatim.
    internal static string GetCacheStem(string itemId, ThumbnailSize size)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\']).ToArray();
        var safeId = new string(itemId.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

        return $"{safeId}.{size.ToString().ToLowerInvariant()}";
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

    private string? FindCachedFile(string cacheStem) =>
        Directory.Exists(_cacheDir)
            ? Directory.EnumerateFiles(_cacheDir, $"{cacheStem}.*").FirstOrDefault()
            : null;
}
