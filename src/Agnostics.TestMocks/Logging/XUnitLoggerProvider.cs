using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

/// <summary>An <see cref="ILoggerProvider"/> that creates loggers writing to xUnit test output.</summary>
public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a provider whose loggers write to the given test output.</summary>
    /// <param name="output">The xUnit output helper that receives the log lines.</param>
    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>Creates an <see cref="XUnitLogger"/> for the given category.</summary>
    /// <param name="categoryName">The category included in every line the logger writes.</param>
    /// <returns>A new logger bound to the provider's test output.</returns>
    public ILogger CreateLogger(string categoryName) => new XUnitLogger(_output, categoryName);

    #region IDisposable
    /// <summary>Does nothing, as the provider owns no resources.</summary>
    public void Dispose()
    {
    }
    #endregion
}