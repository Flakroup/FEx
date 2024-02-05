using FEx.Abstractions.Interfaces;
using FEx.Basics;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.Extensions.Base.Models;
using FEx.Extensions.Web;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Clients;

public class HttpClientEx : HttpClient, INotifyPropertyChanged, IDownloadBase
{
    private const int BufferSize = 81920;

    private readonly bool _ownCTS;
    private string _filePath;
    private Uri _url;
    private string _dirPath;
    private DownloadState _state;
    private bool _isRunning;
    private bool _isDownloaded;
    private double _progressValue;
    private double _progressMaximum;

    public CancellationTokenSource CancellationTokenSource { get; }

    public WebRequestParams Pars { get; set; }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                DirPath = Directory.GetParent(FilePath).FullName;

                Directory.CreateDirectory(DirPath
                                          ?? throw new InvalidOperationException("Target directory path is null."));
            }
        }
    }

    public Uri Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string DirPath
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

    protected byte[] Buffer { get; }

    private static ISynchronizedAccessService LockSrv => FExBasics.SynchronizedAccessService;

    private CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="pars">The <see cref="T:WebRequestParams" /> parameters for processing HTTP response messages.</param>
    /// <param name="disposeHandler">
    ///     <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    ///     <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public HttpClientEx(WebRequestParams pars = null,
                        bool disposeHandler = true,
                        CancellationTokenSource cancellationTokenSource = default)
        : this(pars.GetHttpClientHandler(), disposeHandler, cancellationTokenSource)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="handler">
    ///     The <see cref="T:System.Net.Http.HttpMessageHandler" /> responsible for processing the HTTP
    ///     response messages.
    /// </param>
    /// <param name="disposeHandler">
    ///     <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    ///     <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public HttpClientEx(HttpClientHandler handler,
                        bool disposeHandler = true,
                        CancellationTokenSource cancellationTokenSource = default)
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
            CancellationTokenSource = new CancellationTokenSource();
        }
    }

    public int CompareTo(object obj) =>
        Equals(obj)
            ? 0
            : GetHashCode().CompareTo(obj.GetHashCode());

    public int CompareTo(IDownloadBase other) =>
        Equals(other)
            ? 0
            : GetHashCode().CompareTo(other.GetHashCode());

    public bool Equals(IDownloadBase other) => other is not null && FilePath == other.FilePath && Url == other.Url;

    public static bool operator ==(HttpClientEx left, HttpClientEx right) => Equals(left, right);

    public static bool operator !=(HttpClientEx left, HttpClientEx right) => !Equals(left, right);

    public override int GetHashCode() =>
        BitConverter.ToInt32(Encoding.UTF8.GetBytes($"{FilePath}@{Url.AbsoluteUri}"), 0);

    public override bool Equals(object obj) =>
        ReferenceEquals(this, obj) || obj is FlakHttpClient other && Equals(other);

    public async Task DownloadFileAsync(Uri url, string filePath, bool lockOnFilePath = true)
    {
        var retry = true;

        while (retry)
        {
            DState = DownloadState.Connecting;

            using HttpResponseMessage res = await GetAsync(url, HttpCompletionOption.ResponseHeadersRead,
                CancellationToken);

            try
            {
                using (HttpResponseMessage response = res.EnsureSuccessStatusCode())
                    await DoDownloadAsync(filePath, response, lockOnFilePath);

                retry = false;
                DState = DownloadState.Finished;
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException
                    && (int)res.StatusCode == 429)
                {
                    await Task.Delay(100, CancellationToken);
                    retry = !CancellationToken.IsCancellationRequested;
                }
                else
                {
                    DState = DownloadState.Failed;

                    throw;
                }
            }
        }
    }

    public async Task DoDownloadAsync(string filePath, HttpResponseMessage response, bool lockOnFilePath = true)
    {
        long length = response.Content.Headers.ContentLength ?? -1;

        ProgressMaximum = length > 0
            ? length
            : 0;

        ProgressValue = 0;
        DState = DownloadState.InProgress;

        if (length > 0)
        {
            if (lockOnFilePath)
                await LockSrv.WaitAsync(filePath, cancellationToken: CancellationToken);

            try
            {
#if NETSTANDARD
                using Stream streamResponse = await response.Content.ReadAsStreamAsync();
#else
                await using Stream streamResponse = await response.Content.ReadAsStreamAsync(CancellationToken);
#endif
                if (streamResponse is not null)
                {
                    string dirPath = Directory.GetParent(filePath).FullName;
                    Directory.CreateDirectory(dirPath);

                    using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                        FileShare.None);

                    if (fileStream.Length != length)
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        Array.Clear(Buffer, 0, Buffer.Length);

                        while (true)
                        {
                            int num = await streamResponse.ReadAsync(Buffer, 0, Buffer.Length, CancellationToken);
                            int bytesRead;

                            if ((bytesRead = num) != 0)
                            {
                                await fileStream.WriteAsync(Buffer, 0, bytesRead, CancellationToken);
                                ProgressValue += num;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        ProgressValue = fileStream.Length;
                    }
                }

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    fileStream.SetLength(length);
            }
            finally
            {
                if (lockOnFilePath)
                    LockSrv.Release(filePath);
            }
        }
    }

    public async Task DelayAsync()
    {
        await Task.Delay(10, CancellationToken);
        //delay for subsequent connections
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing || _ownCTS)
            CancellationTokenSource.Dispose();
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null)
            return;

        void EventDelegate() => NotifyChanged(propertyName);
        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);
    }

    protected virtual bool SetProperty<TRet>(ref TRet backingField,
                                             TRet newValue,
                                             Action<TRet> onPropertyChanged = null,
                                             [CallerMemberName] string propertyName = null)
    {
        if (EqualityHelper.IsEqual(ref backingField, newValue))
            return false;

        backingField = newValue;
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
    #endregion
}