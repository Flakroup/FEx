using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Extensions;
using FEx.MVVM.Models;
using FEx.MVVM.Services;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.ViewModels;

public class ProgressListenerViewModel<T> : ThreadingAwareViewModel, IProgressListenerViewModel<T>
    where T : class, IProgressAggregator, new()
{
    private bool _isDisposed;

    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    public T Progress { get; }

    public IAppInfoProvider Application => FExCoreStatics.AppInfoProvider;

    protected static ProgressService ProgressSrv => ProgressService.Instance;

    protected Stopwatch Watch { get; }

    protected IFExTimer Timer => Progress.Timer;
    protected Stopwatch Stopwatch => Progress.Stopwatch;

    public ProgressListenerViewModel(bool useMainProgressContainer = false, params IAsyncInitializable[] dependencies)
        : base(dependencies)
    {
        Progress = ProgressSrv.GetOrAddContainer<T>(useMainProgressContainer);
        Subscriptions = new();
        Watch = new();

        if (useMainProgressContainer)
            Progress.Link(p => p.StatusInfo,
                statusInfo =>
                {
                    if (statusInfo.IsNullOrEmptyOrWhiteSpace())
                        return;

                    _logger.Information(statusInfo);
                });
    }

    public bool SubscribeToProgress<TCon>(IProgressReceiver<TCon> progressReceiver,
                                          params string[] iProgressReceiverProperties)
        where TCon : IProgressAggregator =>
        ProgressSrv.SubscribeToProgress(this, progressReceiver, iProgressReceiverProperties);

    public bool SubscribeToProgress(string containerId, params string[] iProgressReceiverProperties) =>
        ProgressSrv.SubscribeToProgress(Progress, containerId, iProgressReceiverProperties);

    public bool SubscribeToProgress(IProgressAggregator container, params string[] iProgressReceiverProperties) =>
        ProgressSrv.SubscribeToProgress(this, container, iProgressReceiverProperties);

    public bool UnsubscribeFromProgress<TCon>(IProgressReceiver<TCon> progressReceiver)
        where TCon : IProgressAggregator =>
        ProgressSrv.UnsubscribeFromProgress(this, progressReceiver);

    public bool UnsubscribeFromProgress(string containerId) =>
        ProgressSrv.UnsubscribeFromProgress(Progress, containerId);

    public bool UnsubscribeFromProgress(IProgressAggregator container) =>
        ProgressSrv.UnsubscribeFromProgress(this, container);

    public override void PostMainJob(bool showTimeInfo = true)
    {
        base.PostMainJob(showTimeInfo);
        Watch.Stop();
        IsUiUnlocked = true;
        Progress.Stop();

        if (showTimeInfo)
            Progress.SetStatusInfo($"Done in {Watch.GetTime()}");
    }

    public override void PreMainJob()
    {
        base.PreMainJob();
        IsUiUnlocked = false;
        Progress.Start();
        Watch.Restart();
    }

    public void PrgSet(ProgressSnapshot snapshot) => Progress.PrgSet(snapshot.Value, snapshot.Maximum, ProgressChangeMode.Set);

    public void PrgSet(double? val, double? max = null, ProgressChangeMode mode = ProgressChangeMode.Set) =>
        Progress.PrgSet(val, max, mode);

    public void Busy() => Progress.Busy();

    public void Idle() => Progress.Idle();

    public void PrgSetEnd() => Progress.PrgSetEnd();

    public void PrgSetMax(double max) => Progress.PrgSetMax(max);

    public void PrgAdd(double val = 1) => Progress.PrgAdd(val);

    public void PrgMaxAdd(double addedValue) => Progress.PrgMaxAdd(addedValue);

    protected void SubscribeToProgressExcept<TProgress>(TProgress producer, params string[] iProgressReceiverProperties)
        where TProgress : IProgressAggregator
    {
        var props = ProgressAggregatorExtensions.ListenerPropertyNames.ToArray();

        if (!iProgressReceiverProperties.IsNullOrEmpty())
            props = props.Except(iProgressReceiverProperties).ToArray();

        SubscribeToProgress(producer, props);
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            ProgressSrv.RemoveContainer(Progress.Id);
            Parallel.ForEach(Subscriptions.Values, io => io.Dispose());

            base.Dispose(true);
        }

        _isDisposed = true;
    }
    #endregion
}