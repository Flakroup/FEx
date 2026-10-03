using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Logging.Tests;

/// <summary>
/// Records what reaches an <see cref="ILogger" /> so tests can assert the level, attached exception, rendered message
/// and message template of each entry.
/// </summary>
internal sealed class CapturingLogger : ILogger
{
    public List<Entry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var template = (state as IEnumerable<KeyValuePair<string, object?>>)?
            .FirstOrDefault(x => x.Key == "{OriginalFormat}").Value as string;

        Entries.Add(new Entry(logLevel, exception, formatter(state, exception), template));
    }

    internal sealed record Entry(LogLevel Level, Exception? Exception, string Message, string? Template);
}
