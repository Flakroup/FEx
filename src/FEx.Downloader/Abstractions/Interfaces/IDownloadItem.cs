using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Abstractions.Interfaces;

public interface IDownloadItem : IDownloadStub, IDisposable, INotifyPropertyChanged, IProgressAggregatorProperties
{
    CancellationTokenSource CancellationTokenSource { get; }
    Task DownloadFileTask { get; set; }
    long ElapsedMiliseconds { get; }
    string ElapsedTime { get; }
    bool IsDownloaded { get; }
    bool IsFinished { get; }
    bool IsRunning { get; }
    bool OmitQuery { get; set; }
    long Ping { get; }
    WebResponse Response { get; }
    string RunningTasks { get; set; }
    bool TargetIsNotCreated { get; }
    SemaphoreSlim Semaphore { get; }
    double TotalPrg { get; }
    SemaphoreSlim TotalSemaphore { get; }
    string FileName { get; set; }
    string FileNameWithoutExtension { get; set; }
    FileInfo File { get; }

    Task CancelAsync();
    Task<bool> DownloadFileAsync();
    void StartDownload();
}