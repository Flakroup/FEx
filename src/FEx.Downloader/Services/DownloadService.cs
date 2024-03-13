using FEx.Asyncx.Helpers;
using FEx.Basics.Collections.Concurrent;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.Extensions;
using FEx.Extensions.Collections.Dictionaries;
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
    public static int DefaultParallelDownloads { get; set; } = 10;
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    public ObservableConcurrentDictionary<DownloadIndex, IDownloadItem> Downloads { get; }
    public AsyncQueue<DownloadIndex, bool> Queue { get; }
    public bool IncludeFinished { get; set; }
    public bool SetFinished { get; set; }
    public bool CalculateTotalProgress { get; set; }

    public int ParallelDownloads
    {
        get => Queue.Limit;
        set => Queue.Limit = value;
    }

    public DownloadService()
    {
        Subscriptions = [];
        Downloads = [];
        Queue = new AsyncQueue<DownloadIndex, bool>(ex => ex.HandleException(), DefaultParallelDownloads);
        SetFinished = true;
    }

    public async Task<IDownloadItem> AddDownloadStubAsync(IDownloadStub stub,
                                                          bool cancelAndReplaceOldOne = true,
                                                          bool startDownload = true)
    {
        var idx = new DownloadIndex(stub);

        if (Downloads.ContainsKey(idx)
            && !cancelAndReplaceOldOne)
            return Downloads[idx];

        IDownloadItem di = stub is IDownloadItem idi
            ? idi
            : await DownloadItem.CreateAsync(stub, true);

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
        foreach (DownloadIndex idx in Downloads.Where(x => x.Value.DState == DownloadState.None)
                     .Select(x => x.Key)
                     .ToArray())
            StartDownload(idx);
    }

    public async Task WaitForAllDownloadsAsync()
    {
        Task[] tasks = GetUnfinishedDownloadsTasks();

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
                DetachListeners(di);
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

    private void AttachListeners(IDownloadItem di)
    {
        Subscriptions.ReplaceAndDisposeOldValue(di.Url.AbsoluteUri.GenerateMd5OfString(),
            () => di.WhenAnyValue(x => x.TotalPrg, x => x.DState)
                .Sample(FExMvvm.DefaultUIRefreshInterval)
                .Subscribe(_ => OnDownloadPropertyChanged()));
    }

    private void DetachListeners(IDownloadItem di)
    {
        Subscriptions.TryGetKeyValue(di.Url.AbsoluteUri.GenerateMd5OfString())?.Dispose();
    }

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
        (double val, double max, int finished) = CalculateProgress();

        this.SetCurrentDownloadState(val, max);

        if (SetFinished)
            StatusInfo = $"Finished: {finished} / {Downloads.Count}";
    }

    private (double value, double maximum, int finished) CalculateProgress()
    {
        double val = 0;
        double max = 0;
        var finished = 0;

        foreach (IDownloadItem d in Downloads.Values)
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

    private void StartDownload(DownloadIndex idx)
    {
        Downloads[idx].DownloadFileTask = Queue.GetOrAddAsync(idx, () => Downloads[idx].DownloadFileAsync());
    }

    private Task[] GetUnfinishedDownloadsTasks()
    {
        return Downloads.Select(x => x.Value.DownloadFileTask).Where(x => x?.IsFinished() == false).ToArray();
    }
}