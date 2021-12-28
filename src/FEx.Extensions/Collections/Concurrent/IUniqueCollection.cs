namespace FEx.Extensions.Collections.Concurrent;

public interface IUniqueCollection<out TColl, T>
    where TColl : class, IList<T>
{
    bool AddUnique(T item, Action<TColl, T> onAdded = null, bool notifyOfChange = true);
    void AddUniqueRange(IEnumerable<T> collection);
}