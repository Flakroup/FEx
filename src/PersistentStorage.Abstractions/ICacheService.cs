using FEx.PersistentStorage.Abstractions.Enums;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FEx.PersistentStorage.Abstractions;

public interface ICacheService : IFileLocalStorageService
{
    /// <summary>
    /// Caches object
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    BsonValue Add<T>(T item) where T : ICacheableItem;

    /// <summary>
    /// Caches the range of objects
    /// </summary>
    /// <param name="items">Collection of objects to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    void Add<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    /// Updates an existing object. May remove the entire collection.
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">Type of object to be cached</typeparam>
    /// <remarks>
    /// The entire collection of cached objects of specified type will be removed in the case of exception caught.
    /// Item will be added anyway.
    /// </remarks>
    bool Update<T>(T item) where T : ICacheableItem;

    /// <summary>
    /// Insert or Update object based on _id key or predicate.
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="item">Object to be updated or inserted</param>
    /// <param name="predicate">Predicate</param>
    /// <returns>True if insert entity or false if update entity</returns>
    bool Upsert<T>(T item, Expression<Func<T, bool>>? predicate) where T : ICacheableItem;

    /// <summary>
    /// Insert or Update objects based on _id key or predicate.
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="items">Objects to be updated or inserted</param>
    /// <param name="predicate">Predicate</param>
    void Upsert<T>(IEnumerable<T> items, Expression<Func<T, bool>>? predicate) where T : ICacheableItem;

    /// <summary>
    /// Removes cached object of specified type
    /// </summary>
    /// <param name="id">The local storage id of the object to be removed</param>
    /// <typeparam name="T">Type of object to be removed</typeparam>
    bool Delete<T>(ObjectId id) where T : ICacheableItem;

    /// <summary>
    /// Removes cached objects
    /// </summary>
    /// <param name="items">Objects to be removed</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    bool Delete<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    /// Removes cached objects that meet criteria of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    bool Delete<T>(Expression<Func<T, bool>>? predicate) where T : ICacheableItem;

    /// <summary>
    /// Tries to find an object in cache that meets the requirements of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of object to be found</typeparam>
    /// <returns>
    /// Returns the cached object that fits the requirements of the predicate parameter. Returns "default" when object
    /// not found of exception caught.
    /// </returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    T? FirstOrDefault<T>(Expression<Func<T, bool>>? predicate) where T : ICacheableItem;

    /// <summary>
    /// The entire collection of cached objects of specified type matching predicate or all of them if predicate is null.
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be retrieved</typeparam>
    /// <returns>Returns all cached objects of specified type matching predicate or all of them if predicate is null</returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    IReadOnlyCollection<T> Get<T>(Expression<Func<T, bool>>? predicate) where T : ICacheableItem;

    /// <summary>
    /// Deletes all items in provided items collection and replaces it with that items
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Item to replace collection with</param>
    void ReplaceWith<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    /// Deletes all items in provided item collection and replaces it with that single item
    /// </summary>
    /// <typeparam name="T">Type of item</typeparam>
    /// <param name="item">Item to replace collection with</param>
    BsonValue ReplaceWith<T>(T item) where T : ICacheableItem;

    /// <summary>
    /// Calls the ClearCache method of inheritors of IClearCache for the selected Cached Type
    /// </summary>
    /// <param name="clearCacheReason">Clear Cache Reason</param>
    void ClearCache(ClearCacheReason clearCacheReason);
}