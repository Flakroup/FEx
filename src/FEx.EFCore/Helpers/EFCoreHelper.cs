using System;
using System.Linq.Expressions;

namespace FEx.EFCore.Helpers;

public static class EFCoreHelper
{
    public static Expression<Func<TValue, bool>> HasKey<TKey, TValue>(TKey key, string keyPropertyName)
    {
        ConstantExpression constant = Expression.Constant(key, typeof(TKey));
        ParameterExpression iParam = Expression.Parameter(typeof(TValue));
        MemberExpression prop = Expression.Property(iParam, keyPropertyName);
        BinaryExpression equalTo = Expression.Equal(constant, prop);

        return Expression.Lambda<Func<TValue, bool>>(equalTo, iParam);
    }
}