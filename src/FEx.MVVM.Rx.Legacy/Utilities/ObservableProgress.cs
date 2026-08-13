using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Extensions;
using FEx.MVVM.Rx.Legacy.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace FEx.MVVM.Rx.Legacy.Utilities;

/// <summary>
/// A progress reporter that exposes progress updates as an observable stream. This is a hot observable.
/// </summary>
/// <typeparam name="T">The type of progress updates.</typeparam>
public sealed class ObservableProgress<T> : IObservable<T>, IDisposableProgress<T>
{
#pragma warning disable IDISP008 // dual injection pattern, class manages lifecycle
    private readonly ISubject<T> _subject;
#pragma warning restore IDISP008
    private int _isDisposed;

    public bool IsDisposed
    {
        get => Interlocked.CompareExchange(ref _isDisposed, 1, 1) == 1;
        private set
        {
            if (value)
                Interlocked.CompareExchange(ref _isDisposed, 1, 0);
            else
                Interlocked.CompareExchange(ref _isDisposed, 0, 1);
        }
    }

    /// <summary>
    /// Creates an observable progress that uses a replay subject with a single-element buffer, ensuring all new
    /// subscriptions immediately receive the last progress update.
    /// </summary>
    public ObservableProgress()
        : this(new ReplaySubject<T>(1))
    {
    }

    /// <summary>
    /// Creates an observable progress that uses a replay subject with a single-element buffer, ensuring all new
    /// subscriptions immediately receive the last progress update.
    /// </summary>
    /// <param name="scheduler">The scheduler to inject into the replay subject.</param>
    public ObservableProgress(IScheduler scheduler)
        : this(new ReplaySubject<T>(1, scheduler))
    {
    }

    /// <summary>
    /// Creates an observable progress that uses the specified subject.
    /// </summary>
    /// <param name="subject">The subject used for progress updates.</param>
    public ObservableProgress(ISubject<T> subject)
    {
        _subject = subject;
    }

    IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => _subject.Subscribe(observer);

    void IProgress<T>.Report(T value)
    {
        if (!IsDisposed)
            _subject?.OnNext(value);
    }

    /// <summary>
    /// Creates a progress handler with common UI options: updates are buffered in <paramref name="sampleTimeSpan" />
    /// intervals, and the <paramref name="handler" /> is executed on the UI thread. This method must be called from the UI
    /// thread. The UI should already be initialized with the default state; <paramref name="handler" /> is not invoked
    /// with an initial value.
    /// </summary>
    /// <param name="sampleTimeSpan">The time span interval to sample progress updates.</param>
    /// <param name="handler">The progress update handler that updates the UI.</param>
    /// <param name="predicate">This predicate will be used to limit subscription triggering by Where statement.</param>
    /// <param name="limitToCurrentThread">Subscription triggering will be limited to current thread by ObserveOn statement.</param>
    /// <returns></returns>
    public static IDisposableProgress<T> CreateForUiWithBuffer(TimeSpan sampleTimeSpan, Action<IList<T>> handler) =>
        CreateForUiWithBuffer(sampleTimeSpan, handler, null, false);

    public static IDisposableProgress<T> CreateForUiWithBuffer(TimeSpan sampleTimeSpan,
                                                               Action<IList<T>> handler,
                                                               Func<IList<T>, bool> predicate) =>
        CreateForUiWithBuffer(sampleTimeSpan, handler, predicate, false);

    public static IDisposableProgress<T> CreateForUiWithBuffer(TimeSpan sampleTimeSpan,
                                                               Action<IList<T>> handler,
                                                               Func<IList<T>, bool>? predicate,
                                                               bool limitToCurrentThread) =>
        Create(handler, p => p.Buffer(sampleTimeSpan).Where(x => predicate?.Invoke(x) ?? true), limitToCurrentThread);

    public static IDisposableProgress<T> Create<TRet>(Action<TRet> handler,
                                                      Func<ObservableProgress<T>, IObservable<TRet>> subFunc) =>
        Create(handler, subFunc, false);

    public static IDisposableProgress<T> Create<TRet>(Action<TRet> handler,
                                                      Func<ObservableProgress<T>, IObservable<TRet>> subFunc,
                                                      bool limitToCurrentThread) =>
        new ObservableProgressWithSubscription(new(new Subject<T>()),
            p => Subscribe(subFunc(p), handler, limitToCurrentThread));

    /// <summary>
    /// Creates a progress handler with common UI options: updates are sampled on <paramref name="sampleTimeSpan" />
    /// intervals, and the <paramref name="handler" /> is executed on the UI thread. This method must be called from the UI
    /// thread. The UI should already be initialized with the default state; <paramref name="handler" /> is not invoked
    /// with an initial value.
    /// </summary>
    /// <param name="sampleTimeSpan">The time span interval to sample progress updates.</param>
    /// <param name="handler">The progress update handler that updates the UI.</param>
    /// <param name="scheduler">The scheduler to inject into the <c>Sample</c> operator.</param>
    /// <param name="limitToCurrentThread">Subscription triggering will be limited to current thread by ObserveOn statement.</param>
    public static IDisposableProgress<T> CreateForUiWithSample(TimeSpan sampleTimeSpan, Action<T> handler) =>
        CreateForUiWithSample(sampleTimeSpan, handler, null, false);

    public static IDisposableProgress<T> CreateForUiWithSample(TimeSpan sampleTimeSpan,
                                                               Action<T> handler,
                                                               IScheduler scheduler) =>
        CreateForUiWithSample(sampleTimeSpan, handler, scheduler, false);

    public static IDisposableProgress<T> CreateForUiWithSample(TimeSpan sampleTimeSpan,
                                                               Action<T> handler,
                                                               IScheduler? scheduler,
                                                               bool limitToCurrentThread) =>
        Create(handler, p => p.Sample(sampleTimeSpan, scheduler ?? DefaultScheduler.Instance), limitToCurrentThread);

    public static IDisposableProgress<T> CreateForUiWithSample(Action<T> handler) =>
        CreateForUiWithSample(handler, false);

    public static IDisposableProgress<T> CreateForUiWithSample(Action<T> handler, bool limitToCurrentThread) =>
        CreateForUiWithSample(TimeSpan.FromMilliseconds(100), handler, null, limitToCurrentThread);

    public static IDisposableProgress<T> CreateForUiWithSample(Action<T> handler, IScheduler scheduler) =>
        CreateForUiWithSample(TimeSpan.FromMilliseconds(100), handler, scheduler, false);

    public static IDisposableProgress<T> CreateForUiWithSample(Action<T> handler,
                                                               IScheduler scheduler,
                                                               bool limitToCurrentThread) =>
        CreateForUiWithSample(TimeSpan.FromMilliseconds(100), handler, scheduler, limitToCurrentThread);

    private static IDisposable Subscribe<TRet>(IObservable<TRet> observable,
                                               Action<TRet> handler,
                                               bool limitToCurrentThread)
    {
        if (limitToCurrentThread)
        {
            // ObserveOn needs a live synchronization context; Guard throws ArgumentNullException off the UI thread (unchanged from the prior ObserveOn(null) throw).
            var uiScheduler = SynchronizationContextExtensions.Get().Guard(nameof(SynchronizationContext));
            observable = observable.ObserveOn(uiScheduler);
        }

        return observable.Subscribe(handler);
    }

    #region IDisposable
    public void Dispose()
    {
        IsDisposed = true;
        _subject?.OnCompleted();
        var disposableSubject = _subject as IDisposable;
        disposableSubject?.Dispose();
        // ReSharper disable RedundantAssignment
        disposableSubject = null;
        // ReSharper restore RedundantAssignment
    }
    #endregion

    private sealed class ObservableProgressWithSubscription : IDisposableProgress<T>
    {
        private readonly IDisposableProgress<T> _progress;
        private readonly IDisposable _subscription;
        private int _isDisposed;

        public bool IsDisposed
        {
            get => Interlocked.CompareExchange(ref _isDisposed, 1, 1) == 1;
            private set
            {
                if (value)
                    Interlocked.CompareExchange(ref _isDisposed, 1, 0);
                else
                    Interlocked.CompareExchange(ref _isDisposed, 0, 1);
            }
        }

        public ObservableProgressWithSubscription(ObservableProgress<T> progress,
                                                  Func<ObservableProgress<T>, IDisposable> subscription)
            : this(progress, subscription(progress))
        {
        }

        public ObservableProgressWithSubscription(ObservableProgress<T> progress, IDisposable subscription)
        {
            _progress = progress;
            _subscription = subscription;
        }

        public void Report(T value) => _progress?.Report(value);

        #region IDisposable
        public void Dispose()
        {
            IsDisposed = true;
#pragma warning disable IDISP007 // class owns these fields, proper dispose
            _progress?.Dispose();
            _subscription?.Dispose();
#pragma warning restore IDISP007
        }
        #endregion
    }
}