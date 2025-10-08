using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XUnitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => NoopDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception exception,
                            Func<TState, Exception, string> formatter)
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