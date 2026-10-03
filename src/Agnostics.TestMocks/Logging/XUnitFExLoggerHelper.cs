using FEx.Agnostics.Abstractions.Interfaces;
using NSubstitute;
using System;
using System.Linq;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

/// <summary>Creates substitute <see cref="IFExLogger"/> instances that write their output to xUnit test output.</summary>
public static class XUnitFExLoggerHelper
{
    /// <summary>Creates a substitute <see cref="IFExLogger"/> that forwards error, information and debug messages to the test output.</summary>
    /// <param name="output">The xUnit output helper that receives the log lines.</param>
    /// <returns>A mocked logger bound to <paramref name="output"/>.</returns>
    public static IFExLogger CreateMockLogger(ITestOutputHelper output)
    {
        var logger = Substitute.For<IFExLogger>();

        logger.When(static x => x.Error(Arg.Any<Exception>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                var ex = callInfo.Arg<Exception>();
                var message = callInfo.Args().OfType<string>().ElementAtOrDefault(0);
                output.WriteLine($"Log: {message}, Exception: {ex?.Message}");
            });

        logger.When(static x => x.Information(Arg.Any<string>()))
            .Do(callInfo =>
            {
                var message = callInfo.Arg<string>();
                output.WriteLine($"LogInfo: {message}");
            });

        logger.When(static x => x.Debug(Arg.Any<string>()))
            .Do(callInfo =>
            {
                var message = callInfo.Arg<string>();
                output.WriteLine($"RawLog: {message}");
            });

        return logger;
    }
}