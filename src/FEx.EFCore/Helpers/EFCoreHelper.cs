using System;
using System.Linq.Expressions;

namespace FEx.EFCore.Helpers;

public static class EFCoreHelper
{
    public static Expression<Func<TValue, bool>> HasKey<TKey, TValue>(TKey key, string keyPropertyName)
    {
        var constant = Expression.Constant(key, typeof(TKey));
        var iParam = Expression.Parameter(typeof(TValue));
        var prop = Expression.Property(iParam, keyPropertyName);
        var equalTo = Expression.Equal(constant, prop);

        return Expression.Lambda<Func<TValue, bool>>(equalTo, iParam);
    }
}