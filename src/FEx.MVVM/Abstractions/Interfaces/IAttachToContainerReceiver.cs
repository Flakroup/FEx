namespace FEx.MVVM.Abstractions.Interfaces;

public interface IAttachToContainerReceiver
{
    bool SubscribeToProgress<TCon>(IProgressReceiver<TCon> progressReceiver,
                                   params string[] iProgressReceiverProperties) where TCon : IProgressAggregator;

    bool SubscribeToProgress(string containerId, params string[] iProgressReceiverProperties);
    bool SubscribeToProgress(IProgressAggregator container, params string[] iProgressReceiverProperties);

    bool UnsubscribeFromProgress<TCon>(IProgressReceiver<TCon> progressReceiver) where TCon : IProgressAggregator;

    bool UnsubscribeFromProgress(string containerId);
    bool UnsubscribeFromProgress(IProgressAggregator container);
}