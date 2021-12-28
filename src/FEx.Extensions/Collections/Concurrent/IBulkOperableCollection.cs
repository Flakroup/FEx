namespace FEx.Extensions.Collections.Concurrent;

public interface IBulkOperableCollection<T>
{
    void AddRange(IEnumerable<T> collection);
    (bool hasRemovedAny, IList<T> removed) RemoveWhere(Func<T, bool> predicate);
}