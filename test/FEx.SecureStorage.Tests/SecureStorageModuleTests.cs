using FEx.SecureStorage.Abstractions;
using Shouldly;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.SecureStorage.Tests;

/// <summary>
/// The factory's job is to hand back the strongest implementation the current OS offers, and to fall back
/// rather than fail when it offers none.
/// </summary>
/// <remarks>
/// A declared coverage gap sits here: the fallback branch is compiled only below <c>net5.0</c>, the suite
/// runs on <c>net10.0</c>, and the factory is static - so the line that constructs
/// <see cref="ObfuscatedFileStorageService" /> cannot be reached from a test on this target. That
/// constructor is exercised directly in <c>ObfuscatedFileStorageServiceTests</c> instead.
/// </remarks>
public sealed class SecureStorageModuleTests
{
    [Fact]
    public void CreateSecureStorageService_ReturnsAnImplementation() =>
        SecureStorageModule.CreateSecureStorageService().ShouldBeAssignableTo<ISecureStorageService>();

    [Fact]
    public void CreateSecureStorageService_PrefersTheOsKeystore()
    {
        var service = SecureStorageModule.CreateSecureStorageService();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            service.ShouldBeOfType<WindowsDpapiSecureStorageService>();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            service.ShouldBeOfType<MacOsKeychainSecureStorageService>();
        else
            service.ShouldNotBeNull();
    }
}
