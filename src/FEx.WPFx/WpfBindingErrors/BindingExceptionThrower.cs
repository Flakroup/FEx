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
    private static BindingErrorListener _errorListener;

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

    private static string BindingErrorsCacheFile { get; set; }

    private static SemaphoreSlim BindingErrorsCacheSemaphore { get; } = new(1, 1);

    private static HashSet<BindingException> BindingErrorsCache { get; } = GetCachedBindingErrors();

    /// <summary>
    /// Start listening WPF binding error
    /// </summary>
    public static void Attach(string bindingErrorsCacheDirectory)
    {
        BindingErrorsCacheFile = Path.Combine(bindingErrorsCacheDirectory ?? Path.GetTempPath(),
            $"{Guid.NewGuid()}_BindingErrors.json");

        _errorListener = new();
        _errorListener.ErrorCatched += OnErrorCatched;
    }

    /// <summary>
    /// Stop listening WPF binding error
    /// </summary>
    public static void Detach()
    {
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
    private static void OnErrorCatched(TraceEventCache eventCache,
                                       string source,
                                       TraceEventType eventType,
                                       string message)
    {
        if (eventType == TraceEventType.Error)
        {
            BindingErrorsCacheSemaphore.Wait();
            var exception = new BindingException(eventCache, source, message);
            var shouldBeThrown = false;

            if (!BindingErrorsCache.Any(x => x.Equals(exception)))
            {
                BindingErrorsCache.Add(exception);
                string json = JsonConvert.SerializeObject(BindingErrorsCache, Formatting.Indented, DefaultSettings);
                File.WriteAllText(BindingErrorsCacheFile, json);
                shouldBeThrown = true;
            }

            BindingErrorsCacheSemaphore.Release();

            if (shouldBeThrown)
                throw exception;
        }
    }

    private static HashSet<BindingException> GetCachedBindingErrors()
    {
        var result = new HashSet<BindingException>();
        string dir = Path.GetDirectoryName(BindingErrorsCacheFile);

        if (dir is not null)
        {
            Directory.CreateDirectory(dir);

            if (File.Exists(BindingErrorsCacheFile))
            {
                string json = File.ReadAllText(BindingErrorsCacheFile);
                HashSet<BindingException> obj = JsonConvert.DeserializeObject<HashSet<BindingException>>(json);

                if (obj is not null)
                    result = obj;
            }
        }

        return result;
    }
}