using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName) => new XUnitLogger(_output, categoryName);

    #region IDisposable
    public void Dispose()
    {
    }
    #endregion
}