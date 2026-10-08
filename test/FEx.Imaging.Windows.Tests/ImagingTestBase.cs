using FEx.Agnostics.Abstractions.Enums;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Services;
using FEx.EFCore.Configuration;
using FEx.Imaging.Windows.Model;
using FEx.Platforms;
using FEx.Platforms.Abstractions.Interfaces;
using NSubstitute;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

/// <summary>
/// The imaging types raise property changes through the static dispatcher and read the registry service through
/// <see cref="FExPlatforms" />; both are replaced here so a test touches neither a UI thread nor the registry.
/// Process-global state, so every test class that derives from this one runs serially.
/// </summary>
[Collection(ImagingTestCollection.Name)]
public abstract class ImagingTestBase : IDisposable
{
    protected static readonly Uri ImageUrl = new("https://images.test/photos/cat.png");

    protected string Dir { get; }

    protected IExceptionHandler ExceptionHandler { get; } = Substitute.For<IExceptionHandler>();

    /// <summary>
    /// A lock service of its own per test: <c>IndexEntry.Dispose</c> disposes the semaphore it got for its URL, and the
    /// default service is static, so the next test using the same URL would be handed a disposed semaphore.
    /// </summary>
    protected SynchronizedAccessService Locks { get; } = new();

    protected ImagingTestBase()
    {
        Dir = Path.Combine(Path.GetTempPath(), "FExImagingTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Dir);

        var registry = Substitute.For<IRegistryService>();
        registry.GetDefaultExtension(Arg.Any<MediaTypes>()).Returns(".svg");
        _ = new FExPlatforms(registry);

        FExCoreStatics.Configure(dispatcherFactory: static () => new InlineDispatcher(),
            exceptionHandlerFactory: () => ExceptionHandler,
            synchronizedAccessServiceFactory: () => Locks);
    }

    public void Dispose()
    {
        Dispose(true);
        FExCoreStatics.SetDefaults();
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Locks.Dispose();

        try
        {
            Directory.Delete(Dir, true);
        }
        catch (IOException)
        {
            // a decoder may still hold a handle for a moment; the folder lives under the temp path anyway
        }
        catch (UnauthorizedAccessException)
        {
            // same as above
        }
    }

    protected static byte[] Png(int width, int height)
    {
        using var bitmap = new System.Drawing.Bitmap(width, height);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);

        return stream.ToArray();
    }

    protected string WriteFile(string name, byte[] content, string? dir = null)
    {
        var folder = dir ?? Dir;
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, content);

        return path;
    }

    protected FilesCacheServiceConfig CreateConfig(string? dir = null,
                                                   TimeSpan? validPeriod = null,
                                                   bool cacheAll = false) =>
        new(Substitute.For<IDbServiceConfig>(),
            new(dir ?? Dir),
            false,
            validPeriod,
            cacheAll);

    /// <summary>Marks a value owned by someone else (a cache, a container) so it is not mistaken for a new disposable.</summary>
    protected static T Borrow<T>(T value) => value;

    protected static byte[] ReadFile(string path) => File.ReadAllBytes(path);

    protected static CancellationTokenSource CancelledSource()
    {
        var source = new CancellationTokenSource();
        source.Cancel();

        return source;
    }

    protected static string FileNameOf(Uri url) => IndexEntry.GetFileName(url)!;

    protected static HttpResponseMessage PngResponse(Uri url, byte[] content) =>
        new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
            {
                Headers =
                {
                    ContentType = new("image/png")
                }
            },
            RequestMessage = new(HttpMethod.Get, url)
        };
}
