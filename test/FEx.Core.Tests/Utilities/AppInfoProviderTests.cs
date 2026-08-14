using FEx.Core.Utilities;
using Shouldly;
using Xunit;

namespace FEx.Core.Tests.Utilities;

/// <summary>
/// A single-file published app reports Assembly.Location as an EMPTY string, not null, so the resolver must
/// treat empty exactly like absent and fall back to the process main module - otherwise new FileInfo("") throws
/// while the app info is being built, before any window appears. These pin the empty-vs-null branch that a real
/// single-file run would otherwise be the only way to reach.
/// </summary>
public sealed class AppInfoProviderTests
{
    [Fact]
    public void ResolveEntryAssemblyLocation_UsesTheAssemblyLocation_WhenItIsPresent()
    {
        var location = AppInfoProvider.ResolveEntryAssemblyLocation(@"C:\app\Automaton.dll", @"C:\app\Automaton.exe");

        location!.FullName.ShouldBe(@"C:\app\Automaton.dll");
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_FallsBackToTheMainModule_WhenTheLocationIsEmpty()
    {
        // The single-file case: Assembly.Location is "" and the exe path is the only location there is.
        var location = AppInfoProvider.ResolveEntryAssemblyLocation("", @"C:\app\Automaton.exe");

        location!.FullName.ShouldBe(@"C:\app\Automaton.exe");
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_FallsBackToTheMainModule_WhenTheLocationIsNull()
    {
        var location = AppInfoProvider.ResolveEntryAssemblyLocation(null, @"C:\app\Automaton.exe");

        location!.FullName.ShouldBe(@"C:\app\Automaton.exe");
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_IsNull_WhenNeitherLocationIsAvailable()
    {
        AppInfoProvider.ResolveEntryAssemblyLocation("", "").ShouldBeNull();
        AppInfoProvider.ResolveEntryAssemblyLocation(null, null).ShouldBeNull();
    }
}
