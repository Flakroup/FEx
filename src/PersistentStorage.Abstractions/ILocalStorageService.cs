using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FEx.PersistentStorage.Abstractions;

public interface ILocalStorageService : IFileLocalStorageService
{
    /// <summary>
    /// Returns all objects, or only those matching predicate, inside collection ordered by _id index.
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="predicate">Predicate</param>
    /// <returns></returns>
    IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem;

    /// <summary>
    /// Returns first object of collection or null if there are no results of query
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="predicate">Predicate</param>
    /// <returns></returns>
    T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem;

    /// <summary>
    /// Insert a new object into collection.
    /// </summary>
    /// <param name="item">Object to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    BsonValue Insert<T>(T item) where T : ICacheableItem;

    /// <summary>
    /// Insert an array of new objects into collection.
    /// </summary>
    /// <param name="items">Collection of objects to be cached</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    void Insert<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    /// Update object into collection.
    /// </summary>
    /// <param name="item">Object to be updated</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <returns>False if not found document in collection</returns>
    bool Update<T>(T item) where T : ICacheableItem;

    /// <summary>
    /// Insert or Update object based on _id key or predicate.
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="item">Object to be updated or inserted</param>
    /// <param name="predicate">Predicate</param>
    /// <returns>True if insert entity or false if update entity</returns>
    bool Upsert<T>(T item, Expression<Func<T, bool>> predicate) where T : ICacheableItem;

    /// <summary>
    /// Insert or Update objects based on _id key or predicate.
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="items">Objects to be updated or inserted</param>
    /// <param name="predicate">Predicate</param>
    void Upsert<T>(IEnumerable<T> items, Expression<Func<T, bool>> predicate) where T : ICacheableItem;

    /// <summary>
    /// Delete entity based on _id key
    /// </summary>
    /// <param name="id">Object _id to be deleted</param>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <returns>True if succeeded</returns>
    bool Delete<T>(ObjectId id) where T : ICacheableItem;

    /// <summary>
    /// Deletes entities based on their _id key
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="items">Object to be deleted</param>
    bool Delete<T>(IEnumerable<T> items) where T : ICacheableItem;

    /// <summary>
    /// Delete entity based on predicate filter expression
    /// </summary>
    /// <typeparam name="T">The type of cacheable object</typeparam>
    /// <param name="predicate">Predicate</param>
    /// <returns></returns>
    bool DeleteAll<T>(Expression<Func<T, bool>> predicate) where T : ICacheableItem;

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
}