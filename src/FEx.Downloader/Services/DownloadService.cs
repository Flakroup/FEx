using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.MVVM;
using FEx.MVVM.Extensions;
using FEx.MVVM.Utilities;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FEx.Downloader.Services;

public class DownloadService : ProgressAggregator
{
    public static uint DefaultParallelDownloads { get; set; } = 10;
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    public ConcurrentObservableDictionary<DownloadIndex, IDownloadItem> Downloads { get; }
    public AsyncProcessingQueue Queue { get; }
    public bool IncludeFinished { get; set; }
    public bool SetFinished { get; set; }
    public bool CalculateTotalProgress { get; set; }

    public uint ParallelDownloads
    {
        get => Queue.ConcurrencyLimit;
        set => Queue.ConcurrencyLimit = value;
    }

    public DownloadService()
    {
        Subscriptions = [];
        Downloads = [];
        Queue = new(DefaultParallelDownloads);
        SetFinished = true;
    }

    public Task<IDownloadItem> AddDownloadStubAsync(IDownloadStub stub) => AddDownloadStubAsync(stub, true, true);

    public async Task<IDownloadItem> AddDownloadStubAsync(IDownloadStub stub,
                                                          bool cancelAndReplaceOldOne,
                                                          bool startDownload)
    {
        var idx = new DownloadIndex(stub);

        if (Downloads.TryGetValue(idx, out var value)
            && !cancelAndReplaceOldOne)
            return value;

        var di = stub as IDownloadItem ?? await DownloadItem.CreateAsync(stub, true);

        return await AddDownloadAsync(di, idx, cancelAndReplaceOldOne, startDownload);
    }

    public async Task<IDownloadItem> AddDownloadAsync(IDownloadItem di,
                                                      bool cancelAndReplaceOldOne = true,
                                                      bool startDownload = true)
    {
        var idx = new DownloadIndex(di);

        return Downloads.ContainsKey(idx) && !cancelAndReplaceOldOne
            ? Downloads[idx]
            : await AddDownloadAsync(di, idx, cancelAndReplaceOldOne, startDownload);
    }

    public void StartDownloads()
    {
        foreach (var idx in Downloads.Where(x => x.Value.DState == DownloadState.None).Select(x => x.Key).ToArray())
            StartDownload(idx);
    }

    public async Task WaitForAllDownloadsAsync()
    {
        var tasks = GetUnfinishedDownloadsTasks();

        while (tasks.Length > 0)
        {
            await Task.WhenAll(tasks);
            tasks = GetUnfinishedDownloadsTasks();
        }
    }

    protected async Task<IDownloadItem> AddDownloadAsync(IDownloadItem di,
                                                         DownloadIndex idx,
                                                         bool cancelAndReplaceOldOne = true,
                                                         bool startDownload = true)
    {
        if (Downloads.ContainsKey(idx))
        {
            if (cancelAndReplaceOldOne)
            {
                await Downloads[idx].CancelAsync();
                DetachListeners(Downloads[idx]);
                Downloads.ReplaceAndDisposeOldValue(idx, () => di);
                AttachListeners(di);
            }
            else
            {
                di = Downloads[idx];
            }
        }
        else
        {
            Downloads.TryAdd(idx, di);
            AttachListeners(di);
        }

        if (di.DState == DownloadState.None && startDownload)
            StartDownload(idx);

        return di;
    }

    private void AttachListeners(IDownloadItem di) =>
        Subscriptions.ReplaceAndDisposeOldValue(di.Url.AbsoluteUri.GenerateMd5OfString(),
            () => di.WhenAnyValue(x => x.TotalPrg, x => x.DState)
                .Sample(FExMvvm.DefaultUIRefreshInterval)
                .Subscribe(_ => OnDownloadPropertyChanged()));

    private void DetachListeners(IDownloadItem di) =>
        Subscriptions.TryGetKeyValue(di.Url.AbsoluteUri.GenerateMd5OfString())?.Dispose();

    private void OnDownloadPropertyChanged()
    {
        if (CalculateTotalProgress)
            try
            {
                UpdatePrg();
            }
            catch (Exception ex)
            {
                ex.HandleException();
            }
    }

    private void UpdatePrg()
    {
        var (val, max, finished) = CalculateProgress();

        this.SetCurrentDownloadState(val, max);

        if (SetFinished)
            SetStatusInfo($"Finished: {finished} / {Downloads.Count}");
    }

    private (double value, double maximum, int finished) CalculateProgress()
    {
        double val = 0;
        double max = 0;
        var finished = 0;

        foreach (var d in Downloads.Values)
        {
            if (d.IsFinished)
                finished++;

            if (IncludeFinished || !d.IsFinished)
            {
                max += d.Maximum;
                val += d.TotalPrg;
            }
        }

        return (val, max, finished);
    }

    private void StartDownload(DownloadIndex idx) =>
        Downloads[idx].DownloadFileTask = Queue.EnqueueAsync(() => Downloads[idx].DownloadFileAsync());

    // VSTHRD003: These DownloadFileTask instances are started within this service (StartDownload
    // enqueues them); collecting and returning them is intentional task tracking, not a foreign-task await.
#pragma warning disable VSTHRD003
    private Task[] GetUnfinishedDownloadsTasks() =>
        Downloads.Select(x => x.Value.DownloadFileTask).Where(x => x?.IsFinished() == false).ToArray();
#pragma warning restore VSTHRD003
}