using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using FEx.OneDrv.Auth;
using Polly;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

public sealed class OneDriveThumbnailService : IOneDriveThumbnailService
{
    private static readonly HttpClient SharedHttpClient = new();

    private readonly IOneDriveAuthService _authService;
    private readonly OneDriveOptions _options;
    private readonly IFExLogger _logger;

    public OneDriveThumbnailService(IOneDriveAuthService authService, OneDriveOptions options, IFExLogger logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> GetThumbnailAsync(string itemId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentNullException(nameof(itemId));

        var cachePath = GetCachePath(itemId);
        if (File.Exists(cachePath))
        {
#if NETSTANDARD
            return await Task.Run(() => File.ReadAllBytes(cachePath), cancellationToken);
#else
            return await File.ReadAllBytesAsync(cachePath, cancellationToken);
#endif
        }

        var client = await GraphClientHelper.CreateAsync(_authService, cancellationToken);
        var driveId = await GraphClientHelper.GetDefaultDriveIdAsync(client, cancellationToken);
        var pipeline = GraphResiliencePipeline.Create(_logger);

        var thumbnails = await pipeline.ExecuteAsync(
            async cancelToken => await client.Drives[driveId].Items[itemId].Thumbnails.GetAsync(cancellationToken: cancelToken),
            cancellationToken);

        var url = thumbnails?.Value?.Count > 0 ? thumbnails.Value[0].Medium?.Url : null;
        if (url == null)
            return null;

        var bytes = await pipeline.ExecuteAsync(
            async cancelToken =>
            {
                using var resp = await SharedHttpClient.GetAsync(url, cancelToken);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsByteArrayAsync();
            },
            cancellationToken);

        var cacheDir = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrEmpty(cacheDir))
            Directory.CreateDirectory(cacheDir);

#if NETSTANDARD
        await Task.Run(() => File.WriteAllBytes(cachePath, bytes), cancellationToken);
#else
        await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken);
#endif
        _logger.Information($"Cached thumbnail for item {itemId}");
        return bytes;
    }

    private string GetCachePath(string itemId)
    {
        var baseDir = string.IsNullOrWhiteSpace(_options.TokenCachePath)
            ? Path.Combine(Path.GetTempPath(), "FEx.OneDrv", "thumbnails")
            : Path.Combine(Path.GetDirectoryName(_options.TokenCachePath) ?? string.Empty, "thumbnails");

        return Path.Combine(baseDir, $"{itemId}.jpg");
    }
}
