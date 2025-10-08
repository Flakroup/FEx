using FEx.Agnostics.Abstractions.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.IO;

namespace FEx.Logging.Sinks;

internal sealed class PlatformSink : ILogEventSink
{
    private readonly ITextFormatter _formatter;
    private readonly IPlatformLogger _platformLogger;

    public PlatformSink(ITextFormatter formatter, IPlatformLogger platformLogger)
    {
        _formatter = formatter.Guard(nameof(formatter));
        _platformLogger = platformLogger.Guard(nameof(formatter));
    }

    public void Emit(LogEvent logEvent)
    {
        using var output = new StringWriter();
        _formatter.Format(logEvent, output);
        var message = output.ToString();
        _platformLogger.Log(message, logEvent);
    }
}