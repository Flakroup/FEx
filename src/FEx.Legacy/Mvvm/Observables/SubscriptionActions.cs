using System;

namespace FEx.Legacy.Mvvm.Observables;

public class SubscriptionActions<T>
{
    public Action<T> OnNext { get; }
    public Action<Exception> OnError { get; }
    public Action OnCompleted { get; }

    public SubscriptionActions(Action<T> onNext)
        : this(onNext, null, null)
    {
    }

    public SubscriptionActions(Action<T> onNext, Action<Exception> onError, Action onCompleted)
    {
        OnNext = onNext;
        OnError = onError;
        OnCompleted = onCompleted;
    }

    public IDisposable GetSubscription(IObservable<T> observable) => GetSubscription(observable, default);

    public IDisposable GetSubscription(IObservable<T> observable, T subscriptionArgument)
    {
        if (OnNext is not null)
        {
            OnNext(subscriptionArgument);

            return OnError is not null && OnCompleted is not null ? observable.Subscribe(OnNext, OnError, OnCompleted) :
                OnError is not null ? observable.Subscribe(OnNext, OnError) :
                OnCompleted is not null ? observable.Subscribe(OnNext, OnCompleted) : observable.Subscribe(OnNext);
        }

        return null;
    }
}