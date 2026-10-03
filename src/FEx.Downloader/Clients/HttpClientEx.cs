using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Clients;

public class HttpClientEx : HttpClient, INotifyPropertyChanged, IDownloadBase
{
    private const int BufferSize = 81920;
    private const double MaxRetryDelayMs = 30_000;

    private readonly bool _ownCTS;
    private string? _filePath;
    private Uri? _url;
    private string? _dirPath;
    private DownloadState _state;
    private bool _isRunning;
    private bool _isDownloaded;
    private double _progressValue;
    private double _progressMaximum;

    public CancellationTokenSource CancellationTokenSource { get; }

    public WebRequestParams? Pars { get; set; }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                DirPath = Directory.GetParent(FilePath.Guard(nameof(FilePath)))?.FullName;

                Directory.CreateDirectory(DirPath
                                          ?? throw new InvalidOperationException("Target directory path is null."));
            }
        }
    }

    public Uri? Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string? DirPath
    {
        get => _dirPath;
        protected set => SetProperty(ref _dirPath, value);
    }

    public DownloadState DState
    {
        get => _state;
        protected set
        {
            if (SetProperty(ref _state, value))
            {
                IsRunning = DState is DownloadState.Connecting or DownloadState.InProgress;
                IsDownloaded = DState == DownloadState.Finished;
            }
        }
    }

    public bool IsDownloaded
    {
        get => _isDownloaded;
        private set
        {
            if (SetProperty(ref _isDownloaded, value))
                ProgressValue = ProgressMaximum;
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        protected set => SetProperty(ref _isRunning, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        protected set => SetProperty(ref _progressValue, value);
    }

    public double ProgressMaximum
    {
        get => _progressMaximum;
        protected set => SetProperty(ref _progressMaximum, value);
    }

    /// <summary>Maximum number of retries after HTTP 429 responses before the download fails.</summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Maximum number of bytes accepted from a response that has no Content-Length header (default 4 GiB); such a
    /// response would otherwise be streamed to disk for as long as the server keeps sending.
    /// </summary>
    public long MaxDownloadBytes { get; set; } = 4L * 1024 * 1024 * 1024;

    /// <summary>Delay before the first retry; it doubles on every further attempt (capped at 30s).</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    protected byte[] Buffer { get; }

    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;

    private CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    public HttpClientEx()
        : this(new WebRequestParams(), true, null)
    {
    }

    public HttpClientEx(WebRequestParams pars)
        : this(pars, true, null)
    {
    }

    public HttpClientEx(WebRequestParams pars, bool disposeHandler)
        : this(pars, disposeHandler, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="pars">The <see cref="WebRequestParams" /> parameters for processing HTTP response messages.</param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public HttpClientEx(WebRequestParams pars, bool disposeHandler, CancellationTokenSource? cancellationTokenSource)
        : this(pars.GetHttpClientHandler(), disposeHandler, cancellationTokenSource)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="handler">
    /// The <see cref="HttpMessageHandler" /> responsible for processing the HTTP
    /// response messages.
    /// </param>
    public HttpClientEx(HttpClientHandler handler)
        : this(handler, true, null)
    {
    }

    public HttpClientEx(HttpClientHandler handler, bool disposeHandler)
        : this(handler, disposeHandler, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="handler">
    /// The <see cref="HttpMessageHandler" /> responsible for processing the HTTP
    /// response messages.
    /// </param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public HttpClientEx(HttpClientHandler handler, bool disposeHandler, CancellationTokenSource? cancellationTokenSource)
        : base(handler, disposeHandler)
    {
        Buffer = new byte[BufferSize];
        DefaultRequestHeaders.ExpectContinue = false;

        if (cancellationTokenSource is not null)
        {
            CancellationTokenSource = cancellationTokenSource;
        }
        else
        {
            _ownCTS = true;
            CancellationTokenSource = new();
        }
    }

    public int CompareTo(object? obj) =>
        Equals(obj)
            ? 0
            // Non-equal branch: obj is effectively non-null at all call sites (preserves prior behavior).
            : GetHashCode().CompareTo(obj!.GetHashCode());

    public int CompareTo(IDownloadBase? other) =>
        Equals(other)
            ? 0
            // Non-equal branch: other is effectively non-null at all call sites (preserves prior behavior).
            : GetHashCode().CompareTo(other!.GetHashCode());

    public bool Equals(IDownloadBase? other) => other is not null && FilePath == other.FilePath && Url == other.Url;

    public static bool operator ==(HttpClientEx left, HttpClientEx right) => Equals(left, right);

    public static bool operator !=(HttpClientEx left, HttpClientEx right) => !Equals(left, right);

    public override int GetHashCode()
#if NETSTANDARD
    {
        unchecked
        {
            return (FilePath?.GetHashCode() ?? 0) * 397 ^ (Url?.AbsoluteUri?.GetHashCode() ?? 0);
        }
    }
#else
        =>
            HashCode.Combine(FilePath, Url?.AbsoluteUri);
#endif

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is FlakHttpClient other && Equals(other);

    public Task DownloadFileAsync(Uri url, string filePath) => DownloadFileAsync(url, filePath, true);

    public async Task DownloadFileAsync(Uri url, string filePath, bool lockOnFilePath)
    {
        var attempt = 0;

        try
        {
            while (true)
            {
                DState = DownloadState.Connecting;

                using (var res = await GetAsync(url, HttpCompletionOption.ResponseHeadersRead, CancellationToken))
                {
                    try
                    {
                        using var response = res.EnsureSuccessStatusCode();
                        await DoDownloadAsync(filePath, response, lockOnFilePath);

                        DState = DownloadState.Finished;

                        return;
                    }
                    catch (HttpRequestException) when ((int)res.StatusCode == 429 && attempt < MaxRetryAttempts)
                    {
                        // rate limited: back off below, after the response is disposed
                    }
                }

                await Task.Delay(ComputeRetryDelay(RetryBaseDelay, attempt++), CancellationToken);
            }
        }
        catch
        {
            DState = DownloadState.Failed;

            throw;
        }
    }

    /// <summary>Exponential backoff: <paramref name="baseDelay" /> * 2^attempt, capped at 30 seconds.</summary>
    internal static TimeSpan ComputeRetryDelay(TimeSpan baseDelay, int attempt) =>
        TimeSpan.FromMilliseconds(Math.Min(baseDelay.TotalMilliseconds * Math.Pow(2, Math.Min(attempt, 30)),
            MaxRetryDelayMs));

    public Task DoDownloadAsync(string filePath, HttpResponseMessage response) =>
        DoDownloadAsync(filePath, response, true);

    /// <summary>
    /// Downloads the response body to <paramref name="filePath" />. The body is written to a temporary file next to the
    /// target and moved into place only after it was fully received, so a failed download never damages an existing file.
    /// </summary>
    public async Task DoDownloadAsync(string filePath, HttpResponseMessage response, bool lockOnFilePath)
    {
        // -1 means the server sent no Content-Length (e.g. chunked encoding): stream until EOF.
        var length = response.Content.Headers.ContentLength ?? -1;

        ProgressMaximum = length > 0
            ? length
            : 0;

        ProgressValue = 0;
        DState = DownloadState.InProgress;

        if (lockOnFilePath)
            await LockSrv.WaitAsync(filePath, cancellationToken: CancellationToken);

        try
        {
            var dirPath = Directory.GetParent(filePath)?.FullName
                          ?? throw new InvalidOperationException("Target directory path is null.");
            Directory.CreateDirectory(dirPath);

            if (length >= 0 && File.Exists(filePath) && new FileInfo(filePath).Length == length)
            {
                ProgressValue = length;

                return;
            }

#if NETSTANDARD
            using var streamResponse = await response.Content.ReadAsStreamAsync();
#else
            await using var streamResponse = await response.Content.ReadAsStreamAsync(CancellationToken);
#endif
            var tempPath = Path.Combine(dirPath, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Array.Clear(Buffer, 0, Buffer.Length);

                    int bytesRead;

                    while ((bytesRead = await streamResponse.ReadAsync(Buffer, 0, Buffer.Length, CancellationToken))
                           != 0)
                    {
                        ProgressValue += bytesRead;

                        if (length < 0 && ProgressValue > MaxDownloadBytes)
                            throw new IOException($"Response without Content-Length exceeded {MaxDownloadBytes} bytes.");

                        await fileStream.WriteAsync(Buffer, 0, bytesRead, CancellationToken);
                    }
                }

                if (File.Exists(filePath))
                    File.Replace(tempPath, filePath, null);
                else
                    File.Move(tempPath, filePath);
            }
            catch
            {
                File.Delete(tempPath);

                throw;
            }
        }
        finally
        {
            if (lockOnFilePath)
                LockSrv.Release(filePath);
        }
    }

    public async Task DelayAsync() => await Task.Delay(10, CancellationToken); //delay for subsequent connections

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _ownCTS)
#pragma warning disable IDISP007 // conditional on _ownCTS ownership flag
            CancellationTokenSource.Dispose();
#pragma warning restore IDISP007
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

#pragma warning disable S2360 // CallerMemberName requires optional parameter
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
            return;

        FExCoreStatics.Dispatcher.InvokeOnMainThread(EventDelegate, this);

        return;

        void EventDelegate() => NotifyChanged(propertyName);
    }

    protected virtual bool SetProperty<TRet>(ref TRet backingField,
                                             TRet newValue,
                                             Action<TRet>? onPropertyChanged = null,
                                             [CallerMemberName] string? propertyName = null)
    {
        if (EqualityHelper.IsEqual(ref backingField, newValue))
            return false;

        backingField = newValue;
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);

        return true;
    }

#pragma warning restore S2360

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
    #endregion
}