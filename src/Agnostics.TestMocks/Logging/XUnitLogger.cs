using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

/// <summary>An <see cref="ILogger"/> that writes timestamped log entries to xUnit test output.</summary>
public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    /// <summary>Initializes a logger that writes to the given test output under a category name.</summary>
    /// <param name="output">The xUnit output helper that receives the log lines.</param>
    /// <param name="categoryName">The category included in every written line.</param>
    public XUnitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    /// <summary>Returns a no-op scope, since this logger does not track scopes.</summary>
    /// <typeparam name="TState">The type of the scope state.</typeparam>
    /// <param name="state">The scope state, which is ignored.</param>
    /// <returns>A disposable that does nothing when disposed.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

    /// <summary>Reports that every log level is enabled.</summary>
    /// <param name="logLevel">The log level being checked.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>Writes the formatted entry, prefixed with the current time, level and category, to the test output.</summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">The severity of the entry.</param>
    /// <param name="eventId">The event identifier, which is ignored.</param>
    /// <param name="state">The state to format.</param>
    /// <param name="exception">The exception passed to <paramref name="formatter"/>, if any.</param>
    /// <param name="formatter">Produces the message text from <paramref name="state"/> and <paramref name="exception"/>.</param>
    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        _output.WriteLine($"{DateTime.Now:o} [{logLevel}] {_categoryName}: {formatter(state, exception)}");
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        #region IDisposable
        public void Dispose()
        {
        }
        #endregion
    }
}