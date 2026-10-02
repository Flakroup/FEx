using FEx.Agnostics.Abstractions.Logging;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

// Console.Error is process-wide, and FExStaticLogger shares its logger with other tests.
[Collection(StaticStateCollection.Name)]
public sealed class FExDebugLoggerTests
{
    [Theory]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void WarningAndAbove_ReachStandardError(string level)
    {
        var captured = CaptureStandardError(logger =>
        {
            switch (level)
            {
                case "Warning": logger.Warning("the-message"); break;
                case "Error": logger.Error("the-message"); break;
                default: logger.Critical("the-message"); break;
            }
        });

        captured.ShouldContain("the-message");
        captured.ShouldContain($"[{level}]");
    }

    [Fact]
    public void ExceptionOverload_WritesExceptionToStandardError()
    {
        var captured = CaptureStandardError(logger => logger.Error(new InvalidOperationException("kaboom"), "failed"));

        captured.ShouldContain("failed");
        captured.ShouldContain("kaboom");
    }

    [Fact]
    public void BelowWarning_DoesNotWriteToStandardError()
    {
        var captured = CaptureStandardError(logger =>
        {
            logger.Trace("t");
            logger.Debug("d");
            logger.Information("i");
        });

        captured.ShouldBeEmpty();
    }

    [Fact]
    public void StaticLoggerDefault_WarningReachesStandardError()
    {
        var original = FExStaticLogger.Instance;
        var originalError = Console.Error;
        using var writer = new StringWriter();

        try
        {
            FExStaticLogger.Configure(() => new FExDebugLogger());
            Console.SetError(writer);

            FExStaticLogger.Warning("static-warning");
        }
        finally
        {
            Console.SetError(originalError);
            FExStaticLogger.Configure(() => original);
        }

        writer.ToString().ShouldContain("static-warning");
    }

    private static string CaptureStandardError(Action<FExDebugLogger> act)
    {
        var originalError = Console.Error;
        using var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            act(new FExDebugLogger());
        }
        finally
        {
            Console.SetError(originalError);
        }

        return writer.ToString();
    }
}
