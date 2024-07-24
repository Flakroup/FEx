using FEx.Basics.Abstractions.Collections.Concurrent;
using FEx.Basics.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Basics.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ConcurrentSortableCollection<T> : BaseConcurrentCollection<List<T>, T> where T : IComparable<T>
{
    public ConcurrentSortableCollection(IEnumerable<T> collection = null,
                                        bool useBaseConstructor = true,
                                        bool passIndexOfRemovedItem = false,
                                        bool useResetOnBulkOperations = true,
                                        bool sendAsyncEvents = true)
        : base(collection, useBaseConstructor, passIndexOfRemovedItem, useResetOnBulkOperations, sendAsyncEvents)
    {
    }

    public ConcurrentSortableCollection()
        : this(null)
    {
    }

    /// <summary>
    ///     Sorts the elements using the default comparer.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    public void Sort() => Write(() =>
                               {
                                   Collection.Sort();
                                   OnCollectionReset();
                               });

    public void Sort(ListSortDirection order) => Write(() =>
                                                      {
                                                          if (order == ListSortDirection.Ascending)
                                                              Collection.Sort((a, b) => a.CompareTo(b));
                                                          else
                                                              Collection.Sort((a, b) => -1 * a.CompareTo(b));

                                                          OnCollectionReset();
                                                      });

    /// <summary>
    ///     Sorts the elements using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    ///     elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The implementation of <paramref name="comparer" /> caused an error during
    ///     the sort. For example, <paramref name="comparer" /> might not return 0 when comparing an item with itself.
    /// </exception>
    public void Sort(IComparer<T> comparer) => Write(() =>
                                                    {
                                                        Collection.Sort(comparer);
                                                        OnCollectionReset();
                                                    });

    /// <summary>
    ///     Sorts the elements in a range of elements using the specified comparer.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The length of the range to sort.</param>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
    ///     elements, or null to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="index" /> is less than 0.-or-
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="index" /> and <paramref name="count" /> do not specify a
    ///     valid range in the <see cref="T:System.Collections.Generic.List`1" />.-or-The implementation of
    /// <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not
    ///     return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="comparer" /> is null, and the default comparer
    /// <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the
    /// <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for
    ///     type <typeparamref name="T" />.
    /// </exception>
    public void Sort(int index, int count, IComparer<T> comparer) => Write(() =>
                                                                          {
                                                                              Collection.Sort(index, count, comparer);
                                                                              OnCollectionReset();
                                                                          });

    /// <summary>Sorts the elements using the specified <see cref="T:System.Comparison`1" />.</summary>
    /// <param name="comparison">The <see cref="T:System.Comparison`1" /> to use when comparing elements.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="comparison" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The implementation of <paramref name="comparison" /> caused an error
    ///     during the sort. For example, <paramref name="comparison" /> might not return 0 when comparing an item with itself.
    /// </exception>
    public void Sort(Comparison<T> comparison) => Write(() =>
                                                       {
                                                           Collection.Sort(comparison);
                                                           OnCollectionReset();
                                                       });
}