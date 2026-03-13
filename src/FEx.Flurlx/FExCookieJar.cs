using Flurl.Http;
using Flurl.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Flurlx;

/// <summary>
/// A collection of FlurlCookies that can be attached to one or more FlurlRequests, either explicitly via WithCookies
/// or implicitly via a CookieSession. Stores cookies received via Set-Cookie response headers.
/// </summary>
public class FExCookieJar : IReadOnlyCollection<FlurlCookie>
{
    private readonly ConcurrentDictionary<string, FlurlCookie> _dict = new();

    /// <inheritdoc />
    public int Count => _dict.Count;

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<FlurlCookie> GetEnumerator() => _dict.Values.GetEnumerator();

    /// <summary>
    /// Adds a cookie to the jar or replaces one with the same Name/Domain/Path.
    /// Throws FExInvalidCookieException if cookie is invalid.
    /// </summary>
    /// <param name="name">Name of the cookie.</param>
    /// <param name="value">Value of the cookie.</param>
    /// <param name="originUrl">URL of request that sent the original Set-Cookie header.</param>
    /// <param name="dateReceived">
    /// Date/time that original Set-Cookie header was received. Defaults to current date/time.
    /// Important for Max-Age to be enforced correctly.
    /// </param>
    public FExCookieJar
        AddOrReplace(string name, object value, string originUrl, DateTimeOffset? dateReceived = null) =>
        AddOrReplace(new(name, value.ToInvariantString(), originUrl, dateReceived));

    /// <summary>
    /// Adds a cookie to the jar or replaces one with the same Name/Domain/Path.
    /// Throws FExInvalidCookieException if cookie is invalid.
    /// </summary>
    public FExCookieJar AddOrReplace(FlurlCookie cookie)
    {
        if (!TryAddOrReplace(cookie, out var reason))
            throw new FExInvalidCookieException(reason);

        return this;
    }

    /// <summary>
    /// Adds a cookie to the jar or updates if one with the same Name/Domain/Path already exists,
    /// but only if it is valid and not expired.
    /// </summary>
    /// <returns>true if cookie is valid and was added or updated. If false, provides descriptive reason.</returns>
    public bool TryAddOrReplace(FlurlCookie cookie, out string reason)
    {
        if (cookie.IsExpired(out reason))
        {
            // when server sends an expired cookie, it's effectively an instruction for client to delete it.
            // https://stackoverflow.com/a/53573622/62600
            _dict.TryRemove(cookie.GetKey(), out _);

            return false;
        }

        _dict[cookie.GetKey()] = cookie;

        return true;
    }

    /// <summary>
    /// Removes all cookies matching the given predicate.
    /// </summary>
    public FExCookieJar Remove(Func<FlurlCookie, bool> predicate)
    {
        var keys = _dict.Where(kv => predicate(kv.Value)).Select(kv => kv.Key).ToList();

        foreach (var key in keys)
            _dict.TryRemove(key, out _);

        return this;
    }

    /// <summary>
    /// Removes all cookies from this CookieJar
    /// </summary>
    public FExCookieJar Clear()
    {
        _dict.Clear();

        return this;
    }
}

/// <summary>
/// Exception thrown when attempting to add or update an invalid FlurlCookie to a CookieJar.
/// </summary>
public class FExInvalidCookieException : Exception
{
    /// <summary>
    /// Creates a new FExInvalidCookieException.
    /// </summary>
    public FExInvalidCookieException(string reason)
        : base(reason)
    {
    }
}