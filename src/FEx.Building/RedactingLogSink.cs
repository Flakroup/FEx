using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Building;

/// <summary>
/// Passes every log event through a <see cref="SecretRedactor" /> before handing it to the logger that actually
/// writes it. Redacts the message template's literal text as well as every property value - a line like
/// <c>Log.Information(text)</c> carries its content in the template, not in a property.
/// </summary>
/// <remarks>
/// Owns the logger it wraps: it replaces that logger as <c>Log.Logger</c>, so <c>Log.CloseAndFlush()</c> only
/// reaches the wrapped one through this sink's <see cref="Dispose" /> - without it the tail of the log is lost.
/// </remarks>
public sealed class RedactingLogSink : ILogEventSink, IDisposable
{
    private static readonly MessageTemplateParser Parser = new();

    private readonly ILogger _inner;
    private readonly SecretRedactor _redactor;

    public RedactingLogSink(ILogger inner, SecretRedactor redactor)
    {
        _inner = inner;
        _redactor = redactor;
    }

    /// <summary>
    /// Wraps <paramref name="inner" /> so every event written to the result is redacted first. The wrapper
    /// passes every level through; <paramref name="inner" /> keeps its own minimum level and sinks.
    /// </summary>
    public static Logger Wrap(ILogger inner, SecretRedactor redactor) =>
        new LoggerConfiguration().MinimumLevel.Verbose()
#pragma warning disable IDISP004 // The logger built here owns the sink and disposes it with itself
            .WriteTo.Sink(new RedactingLogSink(inner, redactor))
#pragma warning restore IDISP004
            .CreateLogger();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent) => _inner.Write(Redact(logEvent));

    /// <summary>Flushes and disposes the wrapped logger.</summary>
#pragma warning disable IDISP007 // Ownership of the wrapped logger is transferred by Wrap - see remarks
    public void Dispose() => (_inner as IDisposable)?.Dispose();
#pragma warning restore IDISP007

    /// <summary>The same event with its template text and every property value redacted.</summary>
    public LogEvent Redact(LogEvent logEvent)
    {
        var text = logEvent.MessageTemplate.Text;
        var redactedText = _redactor.Redact(text);

        var template = text == redactedText
            ? logEvent.MessageTemplate
            : Parser.Parse(redactedText);

        return new(logEvent.Timestamp,
            logEvent.Level,
            logEvent.Exception,
            template,
            logEvent.Properties.Select(p => new LogEventProperty(p.Key, Redact(p.Value))));
    }

    private LogEventPropertyValue Redact(LogEventPropertyValue value) =>
        value switch
        {
            ScalarValue { Value: null } => value,
            ScalarValue { Value: string s } => new ScalarValue(_redactor.Redact(s)),

            // Numbers, flags, dates cannot carry a secret; anything else (Uri, AbsolutePath, a settings object)
            // is rendered by its ToString(), so that is what gets checked.
            ScalarValue { Value: IConvertible } => value,
            ScalarValue scalar => RedactRendered(scalar),
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(Redact)),
            StructureValue structure => new StructureValue(
                structure.Properties.Select(p => new LogEventProperty(p.Name, Redact(p.Value))),
                structure.TypeTag),
            DictionaryValue dictionary => new DictionaryValue(
                dictionary.Elements.Select(e => new KeyValuePair<ScalarValue, LogEventPropertyValue>(e.Key, Redact(e.Value)))),
            _ => value
        };

    private LogEventPropertyValue RedactRendered(ScalarValue scalar)
    {
        var rendered = scalar.Value!.ToString() ?? string.Empty;
        var redacted = _redactor.Redact(rendered);

        return rendered == redacted ? scalar : new ScalarValue(redacted);
    }
}
