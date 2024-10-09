using System.Collections.Generic;

namespace FEx.Common.Abstractions.Interfaces;

public interface IIndex<TKey, TValue> : IDictionary<TKey, TValue>
{
}