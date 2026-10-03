using Microsoft.Win32;
using Shouldly;
using System;
using System.Runtime.Versioning;
using Xunit;

// The tests deliberately hand keys to the wrappers under test and assert on them after disposal.
#pragma warning disable IDISP001, IDISP017

namespace FEx.Platforms.Tests;

/// <summary>
/// Pins the real registry wiring: disposing the wrappers must release the OS handles, which is the leak behind
/// issue #82. Runs against a throw-away key under HKCU on Windows and is skipped elsewhere.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RegistryUninstallWrapperTests
{
    private const string TestRoot = @"Software\FExPlatformsTests";

    [Fact]
    public void Disposing_The_Root_Releases_The_Uninstall_Key()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows registry only");

        var name = Guid.NewGuid().ToString("N");
        var hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var key = hive.CreateSubKey($@"{TestRoot}\{name}");

        try
        {
            using var root = new RegistryUninstallRoot(hive, key);
        }
        finally
        {
            using var cleanup = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            cleanup.DeleteSubKeyTree(TestRoot, false);
        }

        Should.Throw<ObjectDisposedException>(() => key.GetSubKeyNames());
        // The base hive is a system key: .NET never closes it on Dispose, so only the subkey handle can be asserted.
    }

    [Fact]
    public void Disposing_The_Entry_Releases_The_Key()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows registry only");

        using var hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        var key = hive.CreateSubKey($@"{TestRoot}\{Guid.NewGuid():N}");

        try
        {
            var entry = new RegistryUninstallEntry(key);

            entry.Dispose();
        }
        finally
        {
            hive.DeleteSubKeyTree(TestRoot, false);
        }

        Should.Throw<ObjectDisposedException>(() => key.GetValue("DisplayName"));
    }
}
