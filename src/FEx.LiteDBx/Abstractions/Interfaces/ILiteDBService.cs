using FEx.LiteDbx.Enums;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface ILiteDBService : IFileLocalStorageService
{
    /// <summary>
    ///     Caches object
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    void Add<T>(T item) where T : ICacheableItem;

    /// <summary>
    ///     Caches the range of objects
    /// </summary>
    /// <param name="items">Collection of objects to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    void Add<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    ///     Updates an existing object. Removes the entire collection.
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">Type of an object to be cached</typeparam>
    /// <remarks>
    ///     The entire collection of cached objects of specified type will be removed in the case of exception caught.
    ///     Item will be added anyways.
    /// </remarks>
    void Update<T>(T item) where T : ICacheableItem;

    /// <summary>
    ///     Removes cached object of specified type
    /// </summary>
    /// <param name="id">The local storage id of the object to be removed</param>
    /// <typeparam name="T">Type of an object to be removed</typeparam>
    void Delete<T>(ObjectId id) where T : ICacheableItem;

    /// <summary>
    ///     Removes cached objects
    /// </summary>
    /// <param name="items">Objects to be removed</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    void Delete<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    ///     Removes cached objects that meet criteria of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be removed</typeparam>
    void Delete<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem;

    /// <summary>
    ///     Tries to find an object in cache that meets the requirements of the predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of an object to be found</typeparam>
    /// <returns>
    ///     Returns the cached object that fits the requirements of the predicate parameter. Returns "default" when object
    ///     not found of exception caught.
    /// </returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    T FirstOrDefault<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem;

    /// <summary>
    ///     The entire collection of cached objects of specified type matching predicate or all of them if predicate is null.
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <typeparam name="T">Type of objects to be retrieved</typeparam>
    /// <returns>Returns all cached objects of specified type matching predicate or all of them if predicate is null</returns>
    /// <remarks>The entire collection of cached objects of specified type will be removed in the case of exception caught.</remarks>
    IReadOnlyList<T> Get<T>(Expression<Func<T, bool>> predicate = null) where T : ICacheableItem;

    /// <summary>
    ///     Calls the ClearCache method of inheritors of IClearCache for the selected Cached Type
    /// </summary>
    /// <param name="clearCacheReason">Clear Cache Reason</param>
    void ClearCache(ClearCacheReason clearCacheReason);

    void Replace<T>(T item) where T : ICacheableItem;
}