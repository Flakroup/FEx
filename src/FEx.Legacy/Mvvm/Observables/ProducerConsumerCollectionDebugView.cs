using System.Collections.Concurrent;
using System.Diagnostics;

namespace FEx.Legacy.Mvvm.Observables;

/// <summary>
/// Debug view for the IProducerConsumerCollection.
/// Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="T">Specifies the type of the data being aggregated.</typeparam>
internal sealed class ProducerConsumerCollectionDebugView<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Values => [.. Collection];

    private IProducerConsumerCollection<T> Collection { get; }

    public ProducerConsumerCollectionDebugView(IProducerConsumerCollection<T> collection)
    {
        Collection = collection;
    }
}