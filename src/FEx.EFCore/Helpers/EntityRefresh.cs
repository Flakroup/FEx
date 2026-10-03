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
/// Refreshes a detached, cached entity in place from a freshly loaded copy of the same row, driven by EF metadata:
/// scalar properties, owned types (recursively), complex properties and the navigations the reload carries. A
/// refresh is all or nothing: <see cref="TryPlan" /> refuses when any member cannot be brought to the reloaded state,
/// because a current concurrency token over stale data would turn the next conflict into a silent overwrite.
/// </summary>
internal static class EntityRefresh
{
    /// <summary>
    /// Captures the values <see cref="TryPlan" /> would overwrite, to tell later whether the instance was edited.
    /// </summary>
    public static List<object?> Snapshot(IEntityType entityType, object entity)
    {
        var values = new List<object?>();
        AddSnapshot(entityType, entity, values);

        return values;
    }

    public static bool Matches(IEntityType entityType, object entity, List<object?> snapshot)
    {
        var current = Snapshot(entityType, entity);

        return current.Count == snapshot.Count && current.SequenceEqual(snapshot);
    }

    /// <summary>
    /// Plans copying <paramref name="from" /> into <paramref name="to" />. Returns <c>null</c> when the copy would not
    /// be faithful: a shadow or inaccessible member, a skip navigation, or a navigation the reload did not carry
    /// while the cached instance holds one.
    /// </summary>
    public static Plan? TryPlan(IEntityType entityType, object from, object to)
    {
        var plan = new Plan();

        return TryPlan(entityType, from, to, plan) ? plan : null;
    }

    private static bool TryPlan(IEntityType entityType, object from, object to, Plan plan)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.IsKey())
                continue;

            if (property.IsShadowProperty())
            {
                // A shadow foreign key follows the navigation assigned below; any other shadow value cannot be copied.
                if (property.GetContainingForeignKeys().Any(fk => fk.DependentToPrincipal is null))
                    return false;

                continue;
            }

            if (!Accessor.TryCreate(property, out var accessor))
                return false;

            var value = accessor.Get(from);
            plan.Add(() => accessor.Set(to, value), property.IsConcurrencyToken);
        }

#if NET
        if (entityType.GetSkipNavigations().Any())
            return false;

        foreach (var complexProperty in entityType.GetComplexProperties())
        {
            if (!Accessor.TryCreate(complexProperty, out var accessor))
                return false;

            var value = accessor.Get(from);
            plan.Add(() => accessor.Set(to, value), false);
        }
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
            var isCollection = !onDependent && !fk.IsUnique;

            if (fk.IsOwnership)
            {
                // Owned types always load with their owner, so the reload is complete.
                if (!isCollection && reloaded is not null && cached is not null)
                {
                    if (!TryPlan(fk.DeclaringEntityType, reloaded, cached, plan))
                        return false;
                }
                else
                {
                    plan.Add(() => accessor.Set(to, reloaded), false);
                }

                continue;
            }

            if (isCollection)
            {
                // An empty or missing collection may just not be included; it cannot replace cached items.
                if (!IsEmpty(reloaded))
                    plan.Add(() => accessor.Set(to, reloaded), false);
                else if (!IsEmpty(cached))
                    return false;

                continue;
            }

            if (reloaded is not null)
                plan.Add(() => accessor.Set(to, reloaded), false);
            else if (cached is not null)
            {
                // Missing from the reload: only a dependent whose reloaded foreign key is null has no related row.
                if (!onDependent || !ForeignKeyIsNull(fk, from))
                    return false;

                plan.Add(() => accessor.Set(to, null), false);
            }
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

#if NET
        foreach (var complexProperty in entityType.GetComplexProperties())
        {
            if (Accessor.TryCreate(complexProperty, out var accessor))
                values.Add(accessor.Get(entity));
        }
#endif

        foreach (var navigation in entityType.GetNavigations())
        {
            var fk = navigation.ForeignKey;
            var onDependent = ReferenceEquals(fk.DependentToPrincipal, navigation);

            if ((fk.IsOwnership && onDependent) || !Accessor.TryCreate(navigation, out var accessor))
                continue;

            var value = accessor.Get(entity);

            if (fk.IsOwnership && fk.IsUnique && value is not null)
            {
                values.Add(new Reference(value));
                AddSnapshot(fk.DeclaringEntityType, value, values);
            }
            else if (value is IEnumerable items and not string)
            {
                values.Add(new Reference(value));
                values.AddRange(items.Cast<object?>().Select(item => (object?)new Reference(item)));
            }
            else
            {
                values.Add(new Reference(value));
            }
        }
    }

    private static bool ForeignKeyIsNull(IForeignKey fk, object entity) =>
        fk.Properties.All(p => !p.IsShadowProperty() && Accessor.TryCreate(p, out var a) && a.Get(entity) is null);

    private static bool IsEmpty(object? collection) =>
        collection is not IEnumerable items || !items.Cast<object?>().Any();

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

    // Compared by identity: a navigation or collection item counts as changed only when it was replaced.
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
