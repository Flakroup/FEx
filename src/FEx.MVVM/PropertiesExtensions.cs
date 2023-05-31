using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FEx.MVVM;

public static class PropertiesExtensions
{
    public static bool SetPropertyFromExpression<T>(this object target,
                                                    Expression<Func<T>> expression,
                                                    T value,
                                                    Action<string> onSet = null)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        MemberExpression memberExpression = FindMemberExpression(expression);

        if (memberExpression == null)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        if (memberExpression.Member is not PropertyInfo member)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        if (member.DeclaringType == null)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        if (target != null
            && !member.DeclaringType.IsInstanceOfType(target))
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        MethodInfo setMethod = member.GetSetMethod(true);
        if (setMethod.IsStatic)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        MethodInfo getMethod = member.GetGetMethod(true);

        if (getMethod.Invoke(target, null) is not T oldValue
            || EqualityComparer<T>.Default.Equals(oldValue, value))
            return false;

        setMethod.Invoke(target, new object[] { value });
        onSet?.Invoke(member.Name);
        return true;
    }

    private static MemberExpression FindMemberExpression<T>(Expression<Func<T>> expression)
    {
        if (expression.Body is UnaryExpression body)
        {
            if (body.Operand is not MemberExpression member)
                throw new ArgumentException(WrongUnaryExpressionMessage, nameof(expression));

            return member;
        }

        return expression.Body as MemberExpression;
    }

    private const string WrongExpressionMessage =
        "Wrong expression\nshould be called with expression like\n() => PropertyName";

    private const string WrongUnaryExpressionMessage =
        "Wrong unary expression\nshould be called with expression like\n() => PropertyName";
}