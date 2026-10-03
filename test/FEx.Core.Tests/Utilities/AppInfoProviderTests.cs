using FEx.Core.Utilities;
using Shouldly;
using System.IO;
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
    // Rooted on the current platform: a literal C:\app is a relative path on Linux.
    private static readonly string AppDir = Path.Combine(Path.GetTempPath(), "app");
    private static readonly string Dll = Path.Combine(AppDir, "Automaton.dll");
    private static readonly string Exe = Path.Combine(AppDir, "Automaton.exe");

    [Fact]
    public void ResolveEntryAssemblyLocation_UsesTheAssemblyLocation_WhenItIsPresent()
    {
        var location = AppInfoProvider.ResolveEntryAssemblyLocation(Dll, Exe);

        location!.FullName.ShouldBe(Dll);
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_FallsBackToTheMainModule_WhenTheLocationIsEmpty()
    {
        // The single-file case: Assembly.Location is "" and the exe path is the only location there is.
        var location = AppInfoProvider.ResolveEntryAssemblyLocation("", Exe);

        location!.FullName.ShouldBe(Exe);
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_FallsBackToTheMainModule_WhenTheLocationIsNull()
    {
        var location = AppInfoProvider.ResolveEntryAssemblyLocation(null, Exe);

        location!.FullName.ShouldBe(Exe);
    }

    [Fact]
    public void ResolveEntryAssemblyLocation_IsNull_WhenNeitherLocationIsAvailable()
    {
        AppInfoProvider.ResolveEntryAssemblyLocation("", "").ShouldBeNull();
        AppInfoProvider.ResolveEntryAssemblyLocation(null, null).ShouldBeNull();
    }
}
