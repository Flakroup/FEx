using FEx.MVVM.Abstractions.Interfaces;

namespace FEx.MVVM.Interfaces;

public interface IProgressService
{
    bool SubscribeToProgress<T, TCon>(IProgressReceiver<T> receiver,
                                      IProgressReceiver<TCon> producer,
                                      params string[] iProgressReceiverProperties) where T : IProgressAggregator
        where TCon : IProgressAggregator;

    bool SubscribeToProgress<TCon>(IProgressReceiver<TCon> receiver,
                                   IProgressAggregator container,
                                   params string[] iProgressReceiverProperties) where TCon : IProgressAggregator;

    bool SubscribeToProgress(IProgressAggregator receiver,
                             string containerId,
                             params string[] iProgressReceiverProperties);

    bool UnsubscribeFromProgress<T, TCon>(IProgressReceiver<T> receiver, IProgressReceiver<TCon> producer)
        where T : IProgressAggregator where TCon : IProgressAggregator;

    bool UnsubscribeFromProgress<TCon>(IProgressReceiver<TCon> receiver, IProgressAggregator container)
        where TCon : IProgressAggregator;

    bool UnsubscribeFromProgress(IProgressAggregator receiver, string containerId);
    TCon GetOrAddContainer<TCon>(bool isMain) where TCon : class, IProgressAggregator, new();
    void RemoveContainer(string id);
}