using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using NSubstitute;
using System;

namespace FEx.MSBuildx.Tests;

/// <summary>
/// Observable collections raise their change events through the static dispatcher, which needs a UI main thread that a
/// test host lacks; the scope swaps in a dispatcher that runs the events inline and restores the defaults on dispose.
/// </summary>
internal sealed class InlineDispatcherScope : IDisposable
{
    /// <summary>The xUnit collection of tests that touch the process-global <c>FExCoreStatics</c> factories.</summary>
    public const string CollectionName = "FExCoreStatics dispatcher";

    public InlineDispatcherScope()
    {
        var dispatcher = Substitute.For<IFExDispatcher>();
        dispatcher.When(d => d.SendInContext(Arg.Any<Action>(), Arg.Any<object>(), Arg.Any<bool>(), Arg.Any<uint?>()))
            .Do(call => call.Arg<Action>()());

        FExCoreStatics.Configure(dispatcherFactory: () => dispatcher);
    }

    public void Dispose() => FExCoreStatics.SetDefaults();
}
