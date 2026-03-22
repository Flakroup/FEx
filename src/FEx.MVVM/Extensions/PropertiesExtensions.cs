using FEx.Agnostics.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FEx.MVVM.Extensions;

public static class PropertiesExtensions
{
    public const string WrongExpressionMessage =
        "Wrong expression\nshould be called with expression like\n() => PropertyName";

    private const string WrongUnaryExpressionMessage =
        "Wrong unary expression\nshould be called with expression like\n() => PropertyName";

    public static bool SetPropertyFromExpression<T>(this object target,
                                                    Expression<Func<T>> expression,
                                                    T value) =>
        SetPropertyFromExpression(target, expression, value, null);

    public static bool SetPropertyFromExpression<T>(this object target,
                                                    Expression<Func<T>> expression,
                                                    T value,
                                                    Action<string> onSet)
    {
        expression.Guard(nameof(expression));

        var memberExpression = FindMemberExpression(expression);

        memberExpression.Guard(nameof(memberExpression), WrongExpressionMessage);

        if (memberExpression.Member is not PropertyInfo member)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        if (member.DeclaringType == null)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        if (target != null
            && !member.DeclaringType.IsInstanceOfType(target))
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        var setMethod = member.GetSetMethod(true);

        if (setMethod.IsStatic)
            throw new ArgumentException(WrongExpressionMessage, nameof(expression));

        var getMethod = member.GetGetMethod(true);

        if (getMethod.Invoke(target, null) is not T oldValue
            || EqualityComparer<T>.Default.Equals(oldValue, value))
            return false;

        setMethod.Invoke(target, [value]);
        onSet?.Invoke(member.Name);

        return true;
    }

    public static MemberExpression FindMemberExpression<T>(Expression<Func<T>> expression)
    {
        if (expression.Body is UnaryExpression body)
        {
            if (body.Operand is not MemberExpression member)
                throw new ArgumentException(WrongUnaryExpressionMessage, nameof(expression));

            return member;
        }

        return expression.Body as MemberExpression;
    }

    public static MemberExpression FindMemberExpression<TSender, T>(Expression<Func<TSender, T>> expression)
    {
        if (expression.Body is UnaryExpression body)
        {
            if (body.Operand is not MemberExpression member)
                throw new ArgumentException(WrongUnaryExpressionMessage, nameof(expression));

            return member;
        }

        return expression.Body as MemberExpression;
    }

    public static void LinkChild<T, TProp>(this T sender,
                                           Expression<Func<T, TProp>> property,
                                           Action<TProp> onPropertyChange,
                                           ILink parentLink) where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, (_, _, v) => onPropertyChange(v), parentLink);

    public static void LinkChild<T, TProp>(this T sender,
                                           Expression<Func<T, TProp>> property,
                                           Action<ILink, TProp> onPropertyChange,
                                           ILink parentLink) where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, (l, _, v) => onPropertyChange(l, v), parentLink);

    public static void LinkChild<T, TProp>(this T sender,
                                           Expression<Func<T, TProp>> property,
                                           Action<ILink, TProp, TProp> onPropertyChange,
                                           ILink parentLink) where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, onPropertyChange, parentLink);

    public static void Link<T, TProp>(this T sender,
                                      Expression<Func<T, TProp>> property,
                                      Action<TProp> onPropertyChange) where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, (_, _, v) => onPropertyChange(v));

    public static void Link<T, TProp>(this T sender,
                                      Expression<Func<T, TProp>> property,
                                      Action<ILink, TProp> onPropertyChange)
        where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, (l, _, v) => onPropertyChange(l, v));

    public static void Link<T, TProp>(this T sender,
                                      Expression<Func<T, TProp>> property,
                                      Action<ILink, TProp, TProp> onPropertyChange)
        where T : class, ILinkableNotifyPropertyChanged =>
        sender.InternalLink(property, onPropertyChange);

    private static void InternalLink<T, TProp>(this T sender,
                                               Expression<Func<T, TProp>> property,
                                               Action<ILink, TProp, TProp> onPropertyChange,
                                               ILink parentLink = null) where T : class, ILinkableNotifyPropertyChanged
    {
        sender.Guard(nameof(sender));
        property.Guard(nameof(property));

        MemberExpression memberExpression =
            FindMemberExpression(property).Guard(nameof(memberExpression), WrongExpressionMessage);

        var propertyName = memberExpression.Member.Name;
        var getPropertyValue = property.Compile();

        var link = new Link(typeof(TProp),
            sender,
            propertyName,
            GetPropertyValue,
            OnPropertyChange,
            default(TProp),
            parentLink);

        sender.AddLink(link);

        return;

        object GetPropertyValue(ILinkableNotifyPropertyChanged p) => getPropertyValue((T)p);

        void OnPropertyChange(ILink l, object oldValue, object newValue) =>
            onPropertyChange(l, (TProp)oldValue, (TProp)newValue);
    }
}