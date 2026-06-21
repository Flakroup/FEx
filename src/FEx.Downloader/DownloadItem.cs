using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Clients;
using FEx.Downloader.Enums;
using FEx.Downloader.Extensions;
using FEx.FileSystem;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Extensions;
using FEx.MVVM.Utilities;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader;

public class DownloadItem : ProgressAggregator, IDownloadItem
{
    private string _runningTasks;
    private DownloadState _state;
    private long _dataLength;
    private double _totalPrg;
    private bool _isDownloaded;
    private int _openConnections;
    private string _filePath;
    private string _elapsedTime;
    private long _elapsedMilliseconds;
    private bool _isRunning;
    private string _dirPath;
    private string _fileName;
    private string _fileNameWithoutExtension;
    private FileInfo _file;
    private long _ping;
    private bool _targetIsNotCreated;
    private int _maxOpenConnections;
    private bool _isFinished;
    public static long BufferLength { get; } = (long)FileLengthConverter.GetLength(LengthType.Megabytes);

    public string MD5Checksum { get; }
    public Uri Url { get; }

    public bool ReportProgress { get; }
    public Stopwatch DownloadStopwatch { get; }
    public WebRequestParams Pars { get; }
    // Borrowed from the shared ISynchronizedAccessService (keyed by file path); its lifetime is owned by LockSrv,
    // so this item must NOT dispose it (doing so corrupted the cached lock - see Dispose). Hence IDISP002 suppressed.
#pragma warning disable IDISP002
    public SemaphoreSlim Semaphore { get; private set; }
#pragma warning restore IDISP002
    public SemaphoreSlim TotalSemaphore { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    public Task DownloadFileTask { get; set; }
    public bool IsSpeededUp { get; protected set; }

    public int ParallelRanges { get; private set; }

#pragma warning disable IDISP008 // semaphore from LockSrv, ownership managed externally
    public WebResponse Response { get; protected set; }
#pragma warning restore IDISP008

    public DirectoryInfo TempDirectory { get; protected set; }

    public bool OmitQuery { get; set; }

    public FileInfo File
    {
        get => _file;
        set
        {
            if (TargetIsNotCreated && SetProperty(ref _file, value))
            {
                FilePath = File?.FullName;
                FileName = File?.Name;
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(File?.Name);
                DirPath = File?.DirectoryName;

                if (File?.Directory is null)
                    throw new InvalidOperationException("Target directory path is null.");

                File?.Directory?.Create();
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (TargetIsNotCreated)
                SetProperty(ref _filePath,
                    value,
                    fP =>
                    {
                        if (fP != File?.FullName)
                            File = new(fP);

                        if (fP is not null)
#pragma warning disable IDISP003 // semaphore from LockSrv, not owned
                            Semaphore = LockSrv.EnsureLock(fP);
#pragma warning restore IDISP003
                    });
        }
    }

    public string FileNameWithoutExtension
    {
        get => _fileNameWithoutExtension;
        set
        {
            if (TargetIsNotCreated)
                SetProperty(ref _fileNameWithoutExtension,
                    value,
                    fN =>
                    {
                        if (fN != Path.GetFileNameWithoutExtension(File?.Name))
                            File = new(Path.Combine(DirPath, fN + Path.GetExtension(FilePath)));
                    });
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (TargetIsNotCreated)
                SetProperty(ref _fileName,
                    value,
                    fN =>
                    {
                        if (fN != File?.Name)
                            File = new(Path.Combine(DirPath, fN));
                    });
        }
    }

    public string DirPath
    {
        get => _dirPath;
        protected set
        {
            if (TargetIsNotCreated)
                SetProperty(ref _dirPath,
                    value,
                    dP =>
                    {
                        if (dP != File?.DirectoryName
                            && FileName is not null)
                            File = new(Path.Combine(dP, FileName));
                    });
        }
    }

    public long Ping
    {
        get => _ping;
        private set => SetProperty(ref _ping, value);
    }

    public string RunningTasks
    {
        get => _runningTasks;
        set => SetProperty(ref _runningTasks, value);
    }

    public bool IsDownloaded
    {
        get => _isDownloaded;
        private set
        {
            if (SetProperty(ref _isDownloaded, value) && IsDownloaded)
                Value = new FileInfo(FilePath).Length;
        }
    }

    public bool TargetIsNotCreated
    {
        get => _targetIsNotCreated;
        protected set => SetProperty(ref _targetIsNotCreated, value);
    }

    public DownloadState DState
    {
        get => _state;
        protected set => SetProperty(ref _state, value, RefreshState);
    }

    public bool IsFinished
    {
        get => _isFinished;
        private set => SetProperty(ref _isFinished, value);
    }

    public long DataLength
    {
        get => _dataLength;
        private set =>
            SetProperty(ref _dataLength,
                value,
                v =>
                {
                    if (ReportProgress)
                        PrgSet(TotalPrg, v);
                });
    }

    public double TotalPrg
    {
        get => _totalPrg;
        private set => SetProperty(ref _totalPrg, value);
    }

    public string ElapsedTime
    {
        get => _elapsedTime;
        protected set => SetProperty(ref _elapsedTime, value);
    }

    public long ElapsedMiliseconds
    {
        get => _elapsedMilliseconds;
        protected set => SetProperty(ref _elapsedMilliseconds, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        protected set => SetProperty(ref _isRunning, value);
    }

    public int MaxOpenConnections
    {
        get => _maxOpenConnections;
        private set => SetProperty(ref _maxOpenConnections, value);
    }

    protected IProgress<double> Prg { get; }
    protected IProgress<bool> ConnPrg { get; }
    protected Dictionary<int, DownloadRange> Ranges { get; }

    protected int OpenConnections
    {
        get => _openConnections;
        set
        {
            if (SetProperty(ref _openConnections, value))
                OnOpenConnectionsChanged(value);
        }
    }

    protected CancellationToken CancellationToken => CancellationTokenSource.Token;
    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;

    private DownloadItem(Uri url,
                         string filePath,
                         bool reportProgress,
                         WebRequestParams pars,
                         int parallelRanges,
                         long dataLength,
                         string md5Checksum,
                         CancellationToken cancellationToken)
    {
        ReportProgress = reportProgress;

        if (ReportProgress)
        {
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            Prg = new ProgressWithCompletion<double>(PrgHandlerAsync);
            ConnPrg = new Progress<bool>(HandleConnected);
#pragma warning restore VSTHRD012 // Provide JoinableTaskFactory where allowed
        }

        CancellationTokenSource = cancellationToken == CancellationToken.None
            ? new()
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Url = url;
        DState = DownloadState.None;
        RefreshState(DState);
        MD5Checksum = md5Checksum;

        if (filePath is not null)
            FilePath = filePath;

        Pars = pars;
        TotalSemaphore = new(1, 1);
        Ranges = [];
        ParallelRanges = parallelRanges;
        DataLength = dataLength;
        DownloadStopwatch = new();
    }

    public async Task CancelAsync()
    {
        if (IsRunning)
        {
#if NETSTANDARD
            CancellationTokenSource.Cancel();
#else
            await CancellationTokenSource.CancelAsync();
#endif
            using var cts = new CancellationTokenSource();
            await Semaphore.WaitAsync(cts.Token);
            DState = DownloadState.Cancelled;
            Semaphore.Release();
        }
    }

    public void StartDownload() =>
        // ReSharper disable MethodSupportsCancellation
        DownloadFileTask = Task.Run(DownloadFileAsync); // ReSharper restore MethodSupportsCancellation

    public async Task<bool> DownloadFileAsync()
    {
        var retry = true;
        DownloadStopwatch.Start();

        if (File.Exists
            && MD5Checksum is not null)
            IsDownloaded = CompareChecksum(false);

        if (!IsDownloaded)
        {
            while (retry
                   && DState != DownloadState.Finished
                   && DState != DownloadState.Cancelled
                   && DState != DownloadState.ChecksumMismatch)
            {
                await Semaphore.WaitAsync(CancellationToken);

                try
                {
                    DState = DownloadState.Connecting;
                    IsIndeterminate = true;
                    Mode = ProgressOperationMode.Stream;

                    if (ReportProgress)
                        Timer.Start();

                    if (Response is null)
                        await GetResponseAsync();

                    if (Response is not null)
                    {
                        DataLength = Response.ContentLength;

                        if (DataLength < BufferLength)
                        {
                            await DownloadSmallItemAsync();
                        }
                        else
                        {
                            var canBeSpeedUp = await CheckIfCanBeSpeededUpAsync();

                            if (canBeSpeedUp)
                            {
                                await RunParallelDownloadFileAsync(PrepareTemporaryCacheDirectory());
                            }
                            else
                            {
                                DState = DownloadState.Connecting;

                                using var client = new FlakWebClient(Pars, (_, e) => this.SetCurrentDownloadState(e));
                                DState = DownloadState.InProgress;
                                await client.DownloadFileWithProgressAsync(Url, FilePath);
                            }

                            DState = DownloadState.Finished;
                            TotalPrg = DataLength;
                            PrgSetEnd();
                        }

                        UpdateProgressInfo();
                    }
                    else
                    {
                        DState = DownloadState.Failed;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is WebException
                        {
                            Status: WebExceptionStatus.ProtocolError, Response: HttpWebResponse response
                        }
                        && (int)response.StatusCode == 429)
                    {
                        await Task.Delay(100, CancellationToken);
                        retry = !CancellationToken.IsCancellationRequested;
                    }
                    else
                    {
                        retry = false;

                        DState = CancellationToken.IsCancellationRequested
                            ? DownloadState.Cancelled
                            : DownloadState.Failed;

                        if (DState == DownloadState.Failed)
                            ex.HandleException(false);
                    }
                }
                finally
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        DState = DownloadState.Cancelled;
                        PrgSetEnd();
                    }

                    Semaphore?.Release();
                }
            }

            Response?.Dispose();

            if (IsDownloaded && MD5Checksum is not null)
                IsDownloaded = CompareChecksum();
        }

        DownloadStopwatch.Stop();

        return IsDownloaded;
    }

    public static Task<DownloadItem> CreateAsync(IDownloadStub downloadItem, bool reportProgress) =>
        CreateAsync(downloadItem, reportProgress, CancellationToken.None);

    public static async Task<DownloadItem> CreateAsync(IDownloadStub downloadItem,
                                                       bool reportProgress,
                                                       CancellationToken cancellationToken)
    {
        if (downloadItem.FilePath is null
            && downloadItem.DirPath is not null
            && downloadItem is DownloadStub ds)
            await ds.LoadTargetFileNameAsync();

        return await CreateAsync(downloadItem.Url,
            downloadItem.FilePath,
            reportProgress,
            downloadItem.Pars,
            downloadItem.ParallelRanges,
            downloadItem.DataLength,
            downloadItem.MD5Checksum,
            cancellationToken);
    }

    public static Task<DownloadItem> CreateAsync(string url, string path, bool reportProgress) =>
        CreateAsync(new Uri(url), path, reportProgress, null, 50, -1, null, default);

    public static async Task<DownloadItem> CreateAsync(string url,
                                                       string path,
                                                       bool reportProgress,
                                                       WebRequestParams pars,
                                                       int parallelChunks,
                                                       long dataLength,
                                                       string md5Checksum,
                                                       CancellationToken cancellationToken) =>
        await CreateAsync(new Uri(url),
            path,
            reportProgress,
            pars,
            parallelChunks,
            dataLength,
            md5Checksum,
            cancellationToken);

    public static Task<DownloadItem> CreateAsync(Uri url, string path, bool reportProgress) =>
        CreateAsync(url, path, reportProgress, null, 50, -1, null, default);

    public static async Task<DownloadItem> CreateAsync(Uri url,
                                                       string path,
                                                       bool reportProgress,
                                                       WebRequestParams pars,
                                                       int parallelChunks,
                                                       long dataLength,
                                                       string md5Checksum,
                                                       CancellationToken cancellationToken)
    {
        var res = new DownloadItem(url,
            path,
            reportProgress,
            pars,
            parallelChunks,
            dataLength,
            md5Checksum,
            cancellationToken);

        if (res.DataLength < 0)
            await res.SetDataLengthAsync();

        return res;
    }

    public static DownloadItem CreateFromResponse(HttpWebResponse response, string filePath, bool reportProgress) =>
        CreateFromResponse(response, filePath, reportProgress, null, 0, 50, null, default);

    public static DownloadItem CreateFromResponse(HttpWebResponse response,
                                                  string filePath,
                                                  bool reportProgress,
                                                  WebRequestParams pars,
                                                  long ping,
                                                  int parallelChunks,
                                                  string md5Checksum,
                                                  CancellationToken cancellationToken)
    {
        var res = new DownloadItem(response.ResponseUri,
            filePath,
            reportProgress,
            pars,
            parallelChunks,
            response.ContentLength,
            md5Checksum,
            cancellationToken);

        res.SetResponse(response);
        res.Ping = ping;

        return res;
    }

    public void SetTemporaryCacheDirectory(string path) => TempDirectory = new(path);

    protected override void TimerCallback()
    {
        base.TimerCallback();

        if (!ReportProgress)
            return;

        ElapsedMiliseconds = DownloadStopwatch.ElapsedMilliseconds;
        ElapsedTime = ElapsedMiliseconds.GetTime();
    }

    protected async Task GetResponseAsync()
    {
        var req = Url.GetHttpRequest(Pars);
        var sw = Stopwatch.StartNew();
        var response = await req.GetResponseAsync();
        sw.Stop();
        SetResponse(response);
        Ping = sw.ElapsedMilliseconds;
    }

    protected void HandleConnected(bool isConnected)
    {
        if (CancellationToken.IsCancellationRequested)
            return;

        try
        {
            var value = isConnected
                ? Interlocked.Increment(ref _openConnections)
                : Interlocked.Decrement(ref _openConnections);

            OnOpenConnectionsChanged(value);
        }
        catch
        {
            //ignored
        }
    }

    protected async Task AddTotalPrgAsync(double addedValue)
    {
        try
        {
            if (DState != DownloadState.Finished
                && DState != DownloadState.Failed
                && DState != DownloadState.Cancelled)
            {
                await TotalSemaphore.WaitAsync(CancellationToken);

                if (DState != DownloadState.Finished
                    && DState != DownloadState.Failed
                    && DState != DownloadState.Cancelled)
                    TotalPrg += IsSpeededUp
                        ? addedValue / 2D
                        : addedValue;

                TotalSemaphore.Release();
            }
        }
        catch
        {
            //ignored
        }
    }

    private void OnOpenConnectionsChanged(int value)
    {
        if (value > MaxOpenConnections)
            MaxOpenConnections = value;

        SetRunningTasks();
    }

    private void RefreshState(DownloadState state)
    {
        IsRunning = state is DownloadState.Connecting or DownloadState.InProgress;
        IsDownloaded = state == DownloadState.Finished;

        TargetIsNotCreated = state is DownloadState.None
            or DownloadState.Connecting
            or DownloadState.InProgress
            or DownloadState.Failed
            or DownloadState.Cleanup;

        IsFinished = state is DownloadState.Finished
            or DownloadState.Failed
            or DownloadState.Cancelled
            or DownloadState.ChecksumMismatch;
    }

    private async Task SetDataLengthAsync()
    {
        var sw = new Stopwatch();
        DataLength = await Url.GetHttpFileSizeAsync(Pars, sw);
        Ping = sw.ElapsedMilliseconds;
    }

    private void SetResponse(WebResponse response) => Response = response;

    private async Task DownloadSmallItemAsync()
    {
        using var streamResponse = Response.GetResponseStream();

        if (streamResponse is not null)
        {
            var successful = false;

            do
            {
                try
                {
                    using (var fileStream = File.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        fileStream.SetLength(0);
#if NETSTANDARD2_0
                        await streamResponse.CopyToAsync(fileStream);
#else
                        await streamResponse.CopyToAsync(fileStream, CancellationToken);
#endif
                    }

                    successful = true;
                }
                catch (Exception ex)
                {
                    ex.HandleException(false);
                    await Task.Delay(100, CancellationToken);
                }
            } while (!successful);

            File.Refresh();
            TotalPrg = File.Length;
            PrgSetEnd();

            DState = DownloadState.Finished;
        }
        else
        {
            DState = DownloadState.Failed;
        }
    }

    private async Task<bool> CheckIfCanBeSpeededUpAsync()
    {
        if (Response is null)
            await GetResponseAsync();

        var resultHeaders = Response.GetAllHeaders();
        var responseUri = Response.ResponseUri;

        if (Response is not null)
        {
            Response?.Dispose();
            Response = null;
            GC.Collect();
        }

        var (canBeSpeedUp, _) =
            await responseUri.TryGetRangeAsync(resultHeaders, 0, Convert.ToInt32(BufferLength), Pars);

        if (canBeSpeedUp)
        {
            var speed = await GetNetworkSpeedAsync();

            if (speed > 0)
            {
                var (length, _) = FileLengthConverter.ConvertFileLength(speed, LengthType.Bytes, LengthType.Megabytes);

                // SYSLIB0014: ServicePointManager.DefaultConnectionLimit is obsolete on net5+
                // (no-op for HttpClient); the value is still read here to cap parallel ranges and
                // remains meaningful on legacy TFMs.
#pragma warning disable SYSLIB0014
                ParallelRanges = Convert.ToInt32(Math.Max(Math.Min(Math.Ceiling(ParallelRanges * length),
                        ServicePointManager.DefaultConnectionLimit / 2D),
                    1));
#pragma warning restore SYSLIB0014
            }
            else
            {
                canBeSpeedUp = false;
            }
        }

        return canBeSpeedUp;
    }

    private async Task<double> GetNetworkSpeedAsync()
    {
        var sw = Stopwatch.StartNew();
        var isSuccess = await DownloadBufferAsync();
        sw.Stop();

        return isSuccess
            ? BufferLength / sw.Elapsed.TotalSeconds
            : -1;
    }

    private async Task<bool> DownloadBufferAsync()
    {
        var myHttpWebRequest = Url.GetHttpRequest(Pars);
        myHttpWebRequest.AddRange(0, BufferLength);

        using var res = await myHttpWebRequest.GetResponseAsync();
        using var response = (HttpWebResponse)res;
        var retrievedContentRange = response.GetContentRange();

        if (retrievedContentRange?.From is null
            || retrievedContentRange.To is null
            || retrievedContentRange.From.Value != 0
            || retrievedContentRange.To.Value != BufferLength
            || response.ContentLength != BufferLength + 1)
            return false;

#if NETSTANDARD
        using var streamResponse = response.GetResponseStream();
#else
        await using var streamResponse = response.GetResponseStream();
#endif

#if NETSTANDARD
        if (streamResponse is null)
            return false;
#endif

        using var ms = await streamResponse.CopyToMemoryStreamAsync(cancellationToken: CancellationToken);

        return true;
    }

    private DirectoryInfo PrepareTemporaryCacheDirectory()
    {
        if (TempDirectory is null
            && FilePath is not null)
            TempDirectory = new(Path.Combine(Path.GetPathRoot(Path.GetTempPath()) == Path.GetPathRoot(FilePath)
                    ? Path.GetTempPath()
                    : Path.GetPathRoot(FilePath),
                "TempFlakWebCache"));

        var dirName = (OmitQuery && Url.Query.IsNotNullOrEmptyString()
            ? Url.AbsoluteUri.Replace(Url.Query, string.Empty)
            : Url.AbsoluteUri).GenerateMd5OfString();

        var dir = TempDirectory.GetDescendantDirectory(dirName);
        dir.Create();

        return dir;
    }

    private async Task RunParallelDownloadFileAsync(DirectoryInfo dir)
    {
        IsSpeededUp = true;
        DState = DownloadState.InProgress;
        PrgSetMax(DataLength);

        Ranges.Values.ForEachInEnumerable(x => x?.Dispose());
        Ranges.Clear();
        var operatingSize = Math.Max(Math.Floor(DataLength / (double)ParallelRanges), BufferLength);
        var rangesCount = (int)Math.Ceiling(DataLength / operatingSize);
        long offset = 0;

        foreach (var rangeNo in Enumerable.Range(0, rangesCount))
        {
            var end = (long)Math.Min(offset + operatingSize - 1, DataLength - 1);

            Ranges.Add(rangeNo,
                new(offset, end, dir, Url, Pars, BufferLength, FilePath, DataLength, Prg, ConnPrg, CancellationToken));

            offset = end + 1;
        }

        var unfinishedRanges = Ranges?.Where(x => x.Value?.DState != DownloadState.Finished)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToArray();

        while (unfinishedRanges.IsNotNullOrEmptyList())
        {
            if (ParallelRanges > 1
                && unfinishedRanges.Length > 1)
                await unfinishedRanges.WithWhenAllTasksAsync(x => Ranges[x].DoDownloadAsync());
            else
                foreach (var x in unfinishedRanges)
                    await Ranges[x].DoDownloadAsync();

            unfinishedRanges = Ranges?.Where(x => x.Value?.DState != DownloadState.Finished)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToArray();
        }

        TotalPrg = Maximum / 2;
        PrgSetEnd();

        //List<Range> res = ranges.Where(x => x.State == DownloadState.Finished).OrderBy(x => x.No).ToList();
        DState = DownloadState.TargetCreation;

        PrgSetMax(DataLength);

        using (var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            fileStream.SetLength(0);

        using (var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            foreach (var range in Ranges.Values.OrderBy(x => x.From))
            {
                foreach (var chunk in range.Chunks.Values.OrderBy(x => x.From))
                {
                    using (var stream = chunk.File.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fileStream.Seek(chunk.From, SeekOrigin.Begin);
                        await stream.CopyToAsync(fileStream, DownloadRange.BufferSize, CancellationToken);
                        await fileStream.FlushAsync(CancellationToken);
                        Prg?.Report(stream.Length);
                    }

                    chunk.File.Delete();
                }
            }
        }
#if !NETSTANDARD2_0
        await
#endif
            using (var fileStream =
                   new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            fileStream.SetLength(DataLength);

        DState = DownloadState.Cleanup;
        IsIndeterminate = true;
        FileSystemUtilities.ProcessDirectory(dir, null, FileOperation.Delete, false, ReportProgress);
        DState = DownloadState.Finished;
        TotalPrg = DataLength;
        PrgSetEnd();
    }

    private bool CompareChecksum(bool setChecksumMismatchStatus = true)
    {
        if (MD5Checksum.IsEqual(File.GenerateMd5OfFile()))
        {
            DState = DownloadState.Finished;

            return true;
        }

        if (setChecksumMismatchStatus)
            DState = DownloadState.ChecksumMismatch;

        return false;
    }

    private void SetRunningTasks() => RunningTasks = $"{OpenConnections}/{Ranges.Count}";

    private async Task PrgHandlerAsync(double prgDelta)
    {
        PrgAdd(prgDelta);
        await AddTotalPrgAsync(prgDelta);
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Semaphore is borrowed from the shared ISynchronizedAccessService (LockSrv.EnsureLock, keyed by file
            // path) and is NOT owned by this DownloadItem - its lifetime is managed by LockSrv. Disposing it here
            // left a disposed SemaphoreSlim cached in LockSrv, so the next EnsureLock(samePath) (e.g. the retry
            // loop in ImdbwsDatasetsService) returned it and threw ObjectDisposedException. Do not dispose it.
            TotalSemaphore?.Dispose();
            CancellationTokenSource?.Dispose();
            DownloadFileTask?.Dispose();
            Response?.Dispose();
        }

        base.Dispose(disposing);
    }
    #endregion

    #region IComparable
    public override bool Equals(object obj) => Equals(obj as IDownloadBase);

    public bool Equals(IDownloadBase other) => other is not null && FilePath == other.FilePath && Url == other.Url;

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

    public int CompareTo(object obj) =>
        Equals(obj)
            ? 0
            : GetHashCode().CompareTo(obj.GetHashCode());

    public int CompareTo(IDownloadBase other) =>
        Equals(other)
            ? 0
            : GetHashCode().CompareTo(other.GetHashCode());

    public static bool operator ==(DownloadItem a, DownloadItem b)
    {
        if (ReferenceEquals(a, b))
            return true;

        return (object)a is not null && (object)b is not null && a.FilePath == b.FilePath && a.Url == b.Url;
    }

    public static bool operator !=(DownloadItem a, DownloadItem b) => !(a == b);
    #endregion
}