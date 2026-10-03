using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using Xunit;
using FExLoggerExtensions = FEx.Logging.Abstractions.Extensions.LoggerExtensions;

namespace FEx.Logging.Tests;

/// <summary>
/// Pins the <see cref="ILogger" /> helpers: the exception must reach the log entry (it used to be passed as a format
/// argument and silently dropped) and the message must go through a constant template.
/// </summary>
public sealed class LoggerExtensionsTests
{
    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void Log_AttachesTheExceptionAndMessage_AtTheRequestedLevel(LogLevel level)
    {
        var logger = new CapturingLogger();
        var exception = new InvalidOperationException("boom");

        FExLoggerExtensions.Log(logger, level, "something failed {not a placeholder}", exception);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(level);
        entry.Exception.ShouldBeSameAs(exception);
        entry.Message.ShouldBe("something failed {not a placeholder}");
        entry.Template.ShouldBe("{Message}");
    }

    [Fact]
    public void Log_LogsWithoutAnException_WhenNoneIsGiven()
    {
        var logger = new CapturingLogger();

        FExLoggerExtensions.Log(logger, LogLevel.Warning, "plain message");

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Exception.ShouldBeNull();
        entry.Message.ShouldBe("plain message");
    }

    [Fact]
    public void Log_LogsNothing_ForLevelNone()
    {
        var logger = new CapturingLogger();

        FExLoggerExtensions.Log(logger, LogLevel.None, "ignored");

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void LogError_AttachesTheExceptionAndLogsItsText()
    {
        var logger = new CapturingLogger();
        var exception = new InvalidOperationException("boom");

        FExLoggerExtensions.LogError(logger, exception);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeSameAs(exception);
        entry.Message.ShouldBe(exception.ToString());
        entry.Template.ShouldBe("{Exception}");
    }
}
