using System;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Abstractions.Interfaces;

public interface IFExMemoryCache<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary where TKey : notnull
{
    TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory);
}