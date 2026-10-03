using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FEx.EFCore.Helpers;

/// <summary>
/// Copies a detached (cached) entity's own state onto the tracked entity of its database row, so EF's change tracking
/// writes only what differs: scalar properties, owned types (owned collection items added, updated or deleted) and the
/// foreign keys of reference navigations. Related non-owned entities are never attached, so they are never written.
/// </summary>
internal static class CachedValueApplier
{
    /// <summary>
    /// Applies <paramref name="cached" /> onto <paramref name="row" />, the tracked entry of the same row loaded with its
    /// owned types. The concurrency tokens keep the cached values as their original values, so a row another writer
    /// changed since the value was read fails the save with a <see cref="DbUpdateConcurrencyException" />.
    /// </summary>
    public static void Apply(DbContext ctx, EntityEntry row, object cached)
    {
        List<object> added = [];
        List<object> deleted = [];
        ApplyEntity(ctx, row, cached, added, deleted);

        // Items added to or removed from the loaded owned navigations, explicitly: the owner may have a client-set
        // key (EF would take it for an existing row) and the context may not detect changes on its own.
        foreach (var item in added)
            MarkAdded(ctx.Entry(item));

        foreach (var item in deleted)
            ctx.Entry(item).State = EntityState.Deleted;
    }

    private static void ApplyEntity(DbContext ctx,
                                    EntityEntry row,
                                    object cached,
                                    List<object> added,
                                    List<object> deleted)
    {
        var entityType = row.Metadata;
        row.CurrentValues.SetValues(cached);

        foreach (var navigation in entityType.GetNavigations())
        {
            var foreignKey = navigation.ForeignKey;

            if (!foreignKey.IsOwnership)
            {
                if (ReferenceEquals(navigation, foreignKey.DependentToPrincipal))
                    ApplyForeignKey(row, foreignKey, GetClrValue(navigation, cached));
            }
            else if (ReferenceEquals(navigation, foreignKey.PrincipalToDependent))
            {
                if (foreignKey.IsUnique)
                    ApplyOwnedReference(ctx, row, navigation, cached, added, deleted);
                else
                    ApplyOwnedCollection(ctx, row, navigation, cached, added, deleted);
            }
        }

        row.DetectChanges();
        KeepCachedConcurrencyTokens(row, cached);
    }

    // The navigation decides the key when it is set, whether the key is a shadow or a CLR property; an unset
    // navigation leaves the key as SetValues copied it (CLR) or as loaded (shadow), since it may just not be loaded.
    private static void ApplyForeignKey(EntityEntry row, IForeignKey foreignKey, object? principal)
    {
        if (principal is null)
            return;

        var principalKey = foreignKey.PrincipalKey.Properties;

        if (principalKey.Any(p => p.IsShadowProperty()))
            return;

        for (var i = 0; i < principalKey.Count; i++)
            row.Property(foreignKey.Properties[i].Name).CurrentValue = GetClrValue(principalKey[i], principal);
    }

    private static void ApplyOwnedReference(DbContext ctx,
                                            EntityEntry row,
                                            INavigation navigation,
                                            object cached,
                                            List<object> added,
                                            List<object> deleted)
    {
        var cachedOwned = GetClrValue(navigation, cached);
        var reference = row.Reference(navigation.Name);
        var loadedOwned = reference.CurrentValue;

        if (cachedOwned is null)
        {
            if (loadedOwned is null)
                return;

            reference.CurrentValue = null;
            deleted.Add(loadedOwned);
        }
        else if (loadedOwned is null)
        {
            reference.CurrentValue = cachedOwned;
            added.Add(cachedOwned);
        }
        else
        {
            ApplyEntity(ctx, ctx.Entry(loadedOwned), cachedOwned, added, deleted);
        }
    }

    private static void ApplyOwnedCollection(DbContext ctx,
                                             EntityEntry row,
                                             INavigation navigation,
                                             object cached,
                                             List<object> added,
                                             List<object> deleted)
    {
        var accessor = navigation.GetCollectionAccessor()!;
        var matches = ItemMatcher(navigation.ForeignKey);
        var unmatched = Items(row.Collection(navigation.Name).CurrentValue);
        List<(object Loaded, object Cached)> pairs = [];

        foreach (var cachedItem in Items(GetClrValue(navigation, cached) as IEnumerable))
        {
            var index = unmatched.FindIndex(loaded => matches(loaded, cachedItem));

            if (index < 0)
            {
                // The cached instance itself becomes the new row, so a key the database generates is written back to it.
                accessor.Add(row.Entity, cachedItem, false);
                added.Add(cachedItem);
            }
            else
            {
                pairs.Add((unmatched[index], cachedItem));
                unmatched.RemoveAt(index);
            }
        }

        foreach (var removed in unmatched)
        {
            accessor.Remove(row.Entity, removed);
            deleted.Add(removed);
        }

        foreach (var (loaded, cachedItem) in pairs)
            ApplyEntity(ctx, ctx.Entry(loaded), cachedItem, added, deleted);
    }

    // An item is the same row when its own key (the primary key without the owner's key) is equal. A key the CLR type
    // does not carry (a shadow key) cannot identify a cached item, so such items are matched by their values: an
    // unchanged item stays, an edited one is replaced.
    private static Func<object, object, bool> ItemMatcher(IForeignKey ownership)
    {
        var itemType = ownership.DeclaringEntityType;
        var keyProperties = itemType.FindPrimaryKey()!.Properties;
        var ownKey = keyProperties.Where(p => !ownership.Properties.Contains(p)).ToList();

        var compared = ownKey.Count > 0 && ownKey.TrueForAll(p => !p.IsShadowProperty())
            ? ownKey
            : itemType.GetProperties()
                .Where(p => !p.IsShadowProperty() && !keyProperties.Contains(p) && !ownership.Properties.Contains(p))
                .ToList();

        return (loaded, cachedItem) => compared.TrueForAll(p =>
            StructuralComparisons.StructuralEqualityComparer.Equals(GetClrValue(p, loaded), GetClrValue(p, cachedItem)));
    }

    // SetValues copied the cached tokens as current values; they also become the original values the UPDATE checks.
    // A token that differs from the row is marked modified, so the stale value is a conflict even when nothing else
    // changed.
    private static void KeepCachedConcurrencyTokens(EntityEntry row, object cached)
    {
        foreach (var token in row.Metadata.GetProperties().Where(p => p.IsConcurrencyToken && !p.IsShadowProperty()))
        {
            var entry = row.Property(token.Name);
            var cachedToken = GetClrValue(token, cached);
            var stale = !StructuralComparisons.StructuralEqualityComparer.Equals(entry.OriginalValue, cachedToken);
            entry.OriginalValue = cachedToken;

            if (stale && token.GetAfterSaveBehavior() == PropertySaveBehavior.Save)
                entry.IsModified = true;
        }
    }

    private static void MarkAdded(EntityEntry entry)
    {
        entry.State = EntityState.Added;

        foreach (var navigation in entry.Metadata.GetNavigations()
                     .Where(n => n.ForeignKey.IsOwnership && ReferenceEquals(n, n.ForeignKey.PrincipalToDependent)))
        {
            foreach (var owned in Items(navigation.ForeignKey.IsUnique
                         ? new[] { entry.Reference(navigation.Name).CurrentValue }
                         : entry.Collection(navigation.Name).CurrentValue))
                MarkAdded(entry.Context.Entry(owned));
        }
    }

    private static List<object> Items(IEnumerable? items) => items is null ? [] : [.. items.Cast<object?>().OfType<object>()];

    private static object? GetClrValue(IPropertyBase property, object entity) =>
        property.GetGetter().GetClrValue(entity);
}
