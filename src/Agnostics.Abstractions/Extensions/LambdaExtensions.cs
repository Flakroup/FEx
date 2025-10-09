using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class LambdaExtensions<T>
{
    /// <summary>
    ///     Returns a expression that always returns true
    /// </summary>
    /// <returns>A expression that always returns true</returns>
    public static Expression<Func<T, bool>> TrueExp
    {
        get { return value => true; }
    }

    /// <summary>
    ///     Returns a expression that always returns false
    /// </summary>
    /// <returns>A expression that always returns false</returns>
    public static Expression<Func<T, bool>> FalseExp
    {
        get { return value => false; }
    }
}

public static class LambdaExtensions
{
    /// <summary>
    ///     Combines 2 expressions with an logical AND.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first">First expression.</param>
    /// <param name="second">Second expression.</param>
    /// <returns>An expression where first and second are combined with an logical AND.</returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first,
                                                   Expression<Func<T, bool>> second)
    {
        ParameterExpression p = first.Parameters[0];

        var visitor = new SubstExpressionVisitor
        {
            Subst =
            {
                [second.Parameters[0]] = p
            }
        };

        Expression body = Expression.AndAlso(first.Body,
            visitor.Visit(second.Body) ?? throw new InvalidOperationException());

        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    /// <summary>
    ///     Combines 2 expressions with an logical OR.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first">First expression.</param>
    /// <param name="second">Second expression.</param>
    /// <returns>An expression where first and second are combined with an logical OR.</returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first,
                                                  Expression<Func<T, bool>> second)
    {
        ParameterExpression p = first.Parameters[0];

        var visitor = new SubstExpressionVisitor
        {
            Subst =
            {
                [second.Parameters[0]] = p
            }
        };

        Expression body = Expression.OrElse(first.Body,
            visitor.Visit(second.Body) ?? throw new InvalidOperationException());

        return Expression.Lambda<Func<T, bool>>(body, p);
    }
}

internal class SubstExpressionVisitor : ExpressionVisitor
{
    public IDictionary<Expression, Expression> Subst = new Dictionary<Expression, Expression>();

    protected override Expression VisitParameter(ParameterExpression node) =>
        Subst.TryGetValue(node, out Expression newValue)
            ? newValue
            : node;
}