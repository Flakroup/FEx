using FEx.Agnostics.Abstractions.Interfaces;
using NSubstitute;
using System;
using System.Linq;
using Xunit.Abstractions;

namespace FEx.Agnostics.TestMocks.Logging;

public static class XUnitFExLoggerHelper
{
    public static IFExLogger CreateMockLogger(ITestOutputHelper output)
    {
        IFExLogger logger = Substitute.For<IFExLogger>();

        logger.When(static x => x.Error(Arg.Any<Exception>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                Exception ex = callInfo.Arg<Exception>();
                string message = callInfo.Args().OfType<string>().ElementAtOrDefault(0);
                output.WriteLine($"Log: {message}, Exception: {ex?.Message}");
            });

        logger.When(static x => x.Information(Arg.Any<string>()))
            .Do(callInfo =>
            {
                string message = callInfo.Arg<string>();
                output.WriteLine($"LogInfo: {message}");
            });

        logger.When(static x => x.Debug(Arg.Any<string>()))
            .Do(callInfo =>
            {
                string message = callInfo.Arg<string>();
                output.WriteLine($"RawLog: {message}");
            });

        return logger;
    }
}