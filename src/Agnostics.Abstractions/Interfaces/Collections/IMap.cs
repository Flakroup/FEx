namespace FEx.Agnostics.Abstractions.Interfaces.Collections;

public interface IMap<TForwardKey, TReverseKey>
{
    IIndex<TForwardKey, TReverseKey> ForwardIndex { get; }
    IIndex<TReverseKey, TForwardKey> ReverseIndex { get; }

    void Add(TForwardKey t1, TReverseKey t2);
    void Clear();
    void SetReadOnly();
}