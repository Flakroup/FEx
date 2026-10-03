using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FEx.EFCore.Helpers;

/// <summary>
/// Refreshes a detached, cached entity in place from a no-tracking reload of the same row, driven by the EF metadata of
/// its runtime type. Deliberately narrow: it copies scalar properties and owned references (recursively) and never puts
/// an instance from the reload into the cached graph. A refresh is all or nothing: <see cref="TryPlan" /> refuses
/// whenever any part of the graph could stay stale, because a current concurrency token over stale data would turn the
/// next conflict into a silent overwrite.
/// </summary>
internal static class EntityRefresh
{
    /// <summary>
    /// Captures what <see cref="TryPlan" /> could overwrite or relies on: scalar values, owned references by value
    /// (recursively) and every other navigation by identity.
    /// </summary>
    public static List<object?> Snapshot(IEntityType entityType, object entity)
    {
        var values = new List<object?>();
        AddSnapshot(entityType, entity, values);

        return values;
    }

    public static bool Matches(IEntityType entityType, object entity, List<object?> snapshot) =>
        Snapshot(entityType, entity).SequenceEqual(snapshot);

    /// <summary>
    /// Plans copying <paramref name="from" /> into <paramref name="to" />, or returns <c>null</c> when the copy would
    /// leave part of the cached graph stale. Refused: a shadow value other than a key or the discriminator, a skip
    /// navigation, an owned collection, a complex property, an owned reference present on only one side, a
    /// principal-side navigation (collection or inverse reference) the cached instance holds, and a reference
    /// navigation whose foreign key changed. A reference navigation with an unchanged foreign key keeps the cached
    /// principal instance, so instances shared across the cache stay shared.
    /// </summary>
    public static Plan? TryPlan(IEntityType entityType, object from, object to)
    {
        var plan = new Plan();

        return TryPlan(entityType, from, to, plan) ? plan : null;
    }

    private static bool TryPlan(IEntityType entityType, object from, object to, Plan plan)
    {
        var discriminator = GetDiscriminator(entityType);

        foreach (var property in entityType.GetProperties())
        {
            if (property.IsKey())
                continue;

            if (property.IsShadowProperty())
            {
                // The runtime types match, so the discriminator does too; any other shadow value cannot be compared
                // or copied (a shadow foreign key or token, for example).
                if (ReferenceEquals(property, discriminator))
                    continue;

                return false;
            }

            if (!Accessor.TryCreate(property, out var accessor))
                return false;

            var value = accessor.Get(from);
            plan.Add(() => accessor.Set(to, value), property.IsConcurrencyToken);
        }

#if NET
        if (entityType.GetSkipNavigations().Any())
            return false;

        // ponytail: complex properties are refused, not copied: a mutable complex object edited in place between the
        // rejected save and the write-back could not be told apart from the reloaded one.
        if (entityType.GetComplexProperties().Any())
            return false;
#endif

        foreach (var navigation in entityType.GetNavigations())
        {
            var fk = navigation.ForeignKey;
            var onDependent = ReferenceEquals(fk.DependentToPrincipal, navigation);

            // The owned type's way back to its owner, which is the instance being refreshed.
            if (fk.IsOwnership && onDependent)
                continue;

            if (!Accessor.TryCreate(navigation, out var accessor))
                return false;

            var reloaded = accessor.Get(from);
            var cached = accessor.Get(to);

            if (fk.IsOwnership)
            {
                // ponytail: owned collections are refused, not copied: their items carry no key of their own to match a
                // racing in-place edit against.
                if (!fk.IsUnique)
                    return false;

                if (reloaded is null && cached is null)
                    continue;

                if (reloaded is null || cached is null
                                     || !TryPlan(fk.DeclaringEntityType, reloaded, cached, plan))
                    return false;

                continue;
            }

            // A collection, or the inverse side of a one-to-one: refreshing it would mean putting reloaded instances
            // into the cached graph. Only an empty one is safe to leave as is.
            if (!onDependent)
            {
                if (!IsNullOrEmpty(cached))
                    return false;

                continue;
            }

            // A reference to a principal: keep the cached instance (possibly shared with other cached entities) while
            // the row still points at the same principal; the foreign key value itself is copied as a scalar above.
            if (!ForeignKeyEquals(fk, from, to))
                return false;
        }

        return true;
    }

    private static void AddSnapshot(IEntityType entityType, object entity, List<object?> values)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (!property.IsShadowProperty() && Accessor.TryCreate(property, out var accessor))
                values.Add(accessor.Get(entity));
        }

        foreach (var navigation in entityType.GetNavigations())
        {
            var fk = navigation.ForeignKey;

            if ((fk.IsOwnership && ReferenceEquals(fk.DependentToPrincipal, navigation))
                || !Accessor.TryCreate(navigation, out var accessor))
                continue;

            var value = accessor.Get(entity);
            values.Add(new Reference(value));

            if (fk.IsOwnership && fk.IsUnique && value is not null)
                AddSnapshot(fk.DeclaringEntityType, value, values);
        }
    }

    // Shadow foreign key properties are refused before navigations are looked at, so only CLR values are compared.
    private static bool ForeignKeyEquals(IForeignKey fk, object from, object to) =>
        fk.Properties.Where(p => !p.IsShadowProperty())
            .All(p => Accessor.TryCreate(p, out var accessor) && Equals(accessor.Get(from), accessor.Get(to)));

    private static bool IsNullOrEmpty(object? value) =>
        value is null || (value is IEnumerable items && !items.Cast<object?>().Any());

    private static IProperty? GetDiscriminator(IEntityType entityType) =>
#if NET
        entityType.FindDiscriminatorProperty();
#else
        entityType.GetDiscriminatorProperty();
#endif

    /// <summary>
    /// Assignments to apply; concurrency tokens go last, so a failure part-way leaves the stale token in place and
    /// the next save of the instance is rejected instead of overwriting.
    /// </summary>
    public sealed class Plan
    {
        private readonly List<Action> _values = [];
        private readonly List<Action> _tokens = [];

        public void Add(Action assignment, bool isConcurrencyToken) =>
            (isConcurrencyToken ? _tokens : _values).Add(assignment);

        public void Apply()
        {
            foreach (var assignment in _values)
                assignment();

            foreach (var assignment in _tokens)
                assignment();
        }
    }

    // Compared by identity: a navigation counts as changed only when it was replaced.
    private sealed class Reference
    {
        private readonly object? _target;

        public Reference(object? target) => _target = target;

        public override bool Equals(object? obj) => obj is Reference other && ReferenceEquals(_target, other._target);

        public override int GetHashCode() => _target is null ? 0 : RuntimeHelpers.GetHashCode(_target);
    }

    // Through the property setter where there is one, so the instance raises PropertyChanged; else the backing field.
    private readonly struct Accessor
    {
        private readonly PropertyInfo? _property;
        private readonly FieldInfo? _field;

        private Accessor(PropertyInfo? property, FieldInfo? field)
        {
            _property = property;
            _field = field;
        }

        public static bool TryCreate(IPropertyBase member, out Accessor accessor)
        {
            if (member.PropertyInfo is { GetMethod: not null, SetMethod: not null } property
                && property.GetIndexParameters().Length == 0)
                accessor = new(property, null);
            else if (member.FieldInfo is { IsInitOnly: false } field)
                accessor = new(null, field);
            else
                accessor = default;

            return accessor._property is not null || accessor._field is not null;
        }

        public object? Get(object entity) => _property is not null ? _property.GetValue(entity) : _field!.GetValue(entity);

        public void Set(object entity, object? value)
        {
            if (_property is not null)
                _property.SetValue(entity, value);
            else
                _field!.SetValue(entity, value);
        }
    }
}
