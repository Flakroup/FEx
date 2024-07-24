using FEx.Common.Extensions;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FEx.Rx.Extensions;

public static class ObservableExtensions
{
    public static IDisposable SubscribeWithoutOverlap<T>(this IObservable<T> source, Action<T> action)
    {
#pragma warning disable IDISP001
        var sampler = new Subject<Unit>();
#pragma warning restore IDISP001

        IDisposable sub = source.Sample(sampler)
            .Subscribe(l =>
            {
                action(l);
                sampler.OnNext(Unit.Default);
            });

        // start sampling when we have a first value
#pragma warning disable IDISP004
        source.Take(1).Subscribe(_ => sampler.OnNext(Unit.Default));
#pragma warning restore IDISP004

        return sub;
    }

    public static IObservable<TResult> FromTdf<T, TResult>(this IObservable<T> source,
                                                           Func<IPropagatorBlock<T, TResult>> blockFactory) =>
        Observable.Defer(() =>
        {
            IPropagatorBlock<T, TResult> block = blockFactory();
#pragma warning disable IDISP004
            source.Subscribe(block.AsObserver());
#pragma warning restore IDISP004

            return block.AsObservable();
        });

    public static IObservable<TResult> FromTdf<T, TResult>(this IObservable<T> source,
                                                           Func<T, Task<TResult>> transformFunc) =>
        source.FromTdf(() => new TransformBlock<T, TResult>(transformFunc));

    public static IObservable<TResult> SelectTask<TSource, TResult>(this IObservable<TSource> source,
                                                                    Func<TSource, CancellationToken, Task<TResult>>
                                                                        func,
                                                                    CancellationToken cancellationToken = default) =>
        source.Select(value => Observable.FromAsync(token => func(value,
                cancellationToken != default
#pragma warning disable IDISP004
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, token).Token
#pragma warning restore IDISP004
                    : token)))
            .Switch();

    public static IObservable<TSource> SelectTask<TSource>(this IObservable<TSource> source,
                                                           Func<TSource, CancellationToken, Task> func,
                                                           CancellationToken cancellationToken = default) =>
        source.Select(value => Observable.FromAsync(async token =>
            {
                await func(value,
                    cancellationToken != default
#pragma warning disable IDISP004
                        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, token).Token
#pragma warning restore IDISP004
                        : token);

                return value;
            }))
            .Switch();

    public static IDisposable SubscribeTask<TSource>(this IObservable<TSource> source,
                                                     Func<TSource, CancellationToken, Task> func,
                                                     CancellationToken cancellationToken = default) =>
        source.SelectTask(func, cancellationToken).AsyncSubscribe();

    public static void SubscribeTask<TSource>(this IObservable<TSource> source,
                                              Func<TSource, CancellationToken, Task> func,
                                              CompositeDisposable disposable,
                                              CancellationToken cancellationToken = default) =>
        source.SelectTask(func, cancellationToken).AsyncSubscribe(disposable);

    public static IObservable<EventPattern<PropertyChangedEventArgs>>
        GetPropertyChangedObservable(this INotifyPropertyChanged notifyPropertyChanged) =>
        Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                ev => notifyPropertyChanged.PropertyChanged += ev,
                ev => notifyPropertyChanged.PropertyChanged -= ev)
            .Where(y => y?.EventArgs?.PropertyName is not null && y.Sender is not null);

    public static IObservable<EventPattern<PropertyChangedEventArgs>> GetPropertyChangedObservable(
        this INotifyPropertyChanged notifyPropertyChanged,
        string propertyName) =>
        notifyPropertyChanged.GetPropertyChangedObservable().Where(x => x.EventArgs.PropertyName == propertyName);

    public static IObservable<EventPattern<NotifyCollectionChangedEventArgs>>
        GetCollectionChangedObservable(this INotifyCollectionChanged notifyCollectionChanged) =>
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                ev => notifyCollectionChanged.CollectionChanged += ev,
                ev => notifyCollectionChanged.CollectionChanged -= ev)
            .Where(y => y?.EventArgs is not null);

    public static void TryGetLastValue<TResult>(this IObservable<TResult> source, out TResult value)
    {
        TResult result = default;
        using IDisposable subscription = source.Subscribe(x => result = x);
        value = result;
    }

    public static IObservable<T> MergeMany<T>(this IObservable<T> source, params IObservable<T>[] observables) =>
        observables.Aggregate(source, (current, observable) => current.Merge(observable));

    public static IDisposable AsyncSubscribe<T>(this IObservable<T> source, Action<T> onNext = null)
    {
        IObservable<T> observable = source.ObserveOn(Scheduler.Default).SubscribeOn(Scheduler.Default);

        return onNext is not null
            ? observable.Subscribe(onNext)
            : observable.Subscribe();
    }

    public static void AsyncSubscribe<T>(this IObservable<T> source,
                                         CompositeDisposable disposable,
                                         Action<T> onNext = null)
    {
        IObservable<T> observable = source.ObserveOn(Scheduler.Default).SubscribeOn(Scheduler.Default);

        IDisposable subscription = onNext is not null
            ? observable.Subscribe(onNext)
            : observable.Subscribe();

        disposable.Add(subscription);
    }

    /// <summary>
    /// Ensures the provided disposable is disposed using the specified <see cref="CompositeDisposable" />.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the disposable.
    /// </typeparam>
    /// <param name="item">
    /// The disposable we are going to want to be disposed by the CompositeDisposable.
    /// </param>
    /// <param name="compositeDisposable">
    /// The <see cref="CompositeDisposable" /> to which <paramref name="item" /> will be added.
    /// </param>
    /// <returns>
    /// The disposable.
    /// </returns>
    public static T DisposeUsing<T>(this T item, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Guard(nameof(compositeDisposable)).Add(item);

        return item;
    }

    /// <summary>
    ///     Returns a task that will receive the last value or the exception produced by the observable sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="observable">Observable sequence to convert to a task.</param>
    /// <param name="cancellationToken">
    ///     Cancellation token that can be used to cancel the task, causing unsubscription from the
    ///     observable sequence.
    /// </param>
    /// <returns>A task that will receive the last element or the exception produced by the observable sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="observable" /> is <c>null</c>.</exception>
    public static Task<T> ToTaskAsync<T>(this IObservable<T> observable, CancellationToken cancellationToken) =>
        observable.ObserveOn(Scheduler.Default)
            .SubscribeOn(Scheduler.Default)
            .ToTask(cancellationToken, null, Scheduler.Default);
}