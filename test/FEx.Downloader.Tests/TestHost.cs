using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using NSubstitute;
using System;

namespace FEx.Downloader.Tests;

internal static class TestHost
{
    /// <summary>No UI main thread exists in a test host; runs property-change notifications inline.</summary>
    public static void ConfigureInlineDispatcher()
    {
        var dispatcher = Substitute.For<IFExDispatcher>();
        dispatcher.When(d => d.InvokeOnMainThread(Arg.Any<Action>(), Arg.Any<object?>()))
            .Do(call => call.Arg<Action>()());
        FExCoreStatics.Configure(dispatcherFactory: () => dispatcher);
    }
}
