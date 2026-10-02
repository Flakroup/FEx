using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FEx.WPFx.WpfBindingErrors;

/// <summary>
/// Converts WPF binding error into BindingException
/// </summary>
/// <remarks>
/// WPF Binding Error Testing
/// Copyright 2013 Benoit Blanchon
/// This has been inpired by
/// http://tech.pro/tutorial/940/wpf-snippet-detecting-binding-errors
/// </remarks>
public static class BindingExceptionThrower
{
    private static BindingErrorListener? _errorListener;

    public static JsonSerializerSettings DefaultSettings { get; } = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat
    };

    /// <summary>
    /// Gets a value indicating whether this instance is attached.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is attached; otherwise, <c>false</c>.
    /// </value>
    public static bool IsAttached => _errorListener is not null;

    // Set by Attach; null when no cache directory was given, in which case errors are only deduplicated in memory.
    internal static string? BindingErrorsCacheFile { get; private set; }

    private static SemaphoreSlim BindingErrorsCacheSemaphore { get; } = new(1, 1);

    // Loaded by Attach once the cache file location is known.
    private static HashSet<BindingException> BindingErrorsCache { get; set; } = [];

    /// <summary>
    /// Start listening WPF binding error
    /// </summary>
    /// <param name="bindingErrorsCacheDirectory">
    /// Directory of the persisted binding-error cache, so errors seen in a previous run are not reported
    /// again. When <c>null</c> nothing is persisted (a shared default location would let unrelated apps
    /// suppress each other's errors).
    /// </param>
    public static void Attach(string? bindingErrorsCacheDirectory)
    {
        BindingErrorsCacheFile = bindingErrorsCacheDirectory is null
            ? null
            : Path.Combine(bindingErrorsCacheDirectory, "BindingErrors.json");

        BindingErrorsCache = BindingErrorsCacheFile is null
            ? []
            : LoadCachedBindingErrors(BindingErrorsCacheFile);

        _errorListener = new();
        _errorListener.ErrorCatched += OnErrorCatched;
    }

    /// <summary>
    /// Stop listening WPF binding error
    /// </summary>
    public static void Detach()
    {
        if (_errorListener is null)
            return;

        _errorListener.ErrorCatched -= OnErrorCatched;
        _errorListener.Dispose();
        _errorListener = null;
    }

    /// <summary>
    /// Called when [error catched].
    /// </summary>
    /// <param name="eventCache">The event cache.</param>
    /// <param name="source">The source.</param>
    /// <param name="eventType">Type of the event.</param>
    /// <param name="message">The message.</param>
    /// <exception cref="BindingException"></exception>
    [DebuggerStepThrough]
    internal static void OnErrorCatched(TraceEventCache eventCache,
                                        string source,
                                        TraceEventType eventType,
                                        string? message)
    {
        if (eventType != TraceEventType.Error)
            return;

        var exception = new BindingException(eventCache, source, message);
        var shouldBeThrown = false;

        BindingErrorsCacheSemaphore.Wait();

        try
        {
            if (!BindingErrorsCache.Any(x => x.Equals(exception)))
            {
                BindingErrorsCache.Add(exception);
                shouldBeThrown = true;
                Persist();
            }
        }
        finally
        {
            BindingErrorsCacheSemaphore.Release();
        }

        if (shouldBeThrown)
            throw exception;
    }

    internal static HashSet<BindingException> LoadCachedBindingErrors(string cacheFile)
    {
        try
        {
            var dir = Path.GetDirectoryName(cacheFile);

            if (dir is not null)
                Directory.CreateDirectory(dir);

            if (!File.Exists(cacheFile))
                return [];

            var json = File.ReadAllText(cacheFile);

            return JsonConvert.DeserializeObject<HashSet<BindingException>>(json) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // An unreadable cache only means previously seen errors are reported again.
            return [];
        }
    }

    private static void Persist()
    {
        if (BindingErrorsCacheFile is null)
            return;

        try
        {
            var json = JsonConvert.SerializeObject(BindingErrorsCache, Formatting.Indented, DefaultSettings);
            File.WriteAllText(BindingErrorsCacheFile, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An unwritable cache must not replace the binding error being reported.
        }
    }
}
