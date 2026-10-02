using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Encryption;
using FEx.Encryption.Exceptions;
using FEx.SecureStorage.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.SecureStorage.Tests;

/// <summary>
/// The factory's job is to hand back the strongest implementation the current OS offers, and to fall back
/// rather than fail when it offers none - unless the host refused that - and never to fall back silently.
/// The fallback branch is driven through the internal overload with the OS probe stubbed out, so it runs on
/// every OS the suite does.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class SecureStorageModuleTests : IDisposable
{
    private readonly DirectoryInfo _storage =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "fex-module-tests-" + Guid.NewGuid().ToString("N")));

    private readonly IFExLogger _previousLogger = FExStaticLogger.Instance;
    private readonly bool _previousAllow = SecureStorageModule.AllowObfuscatedFallback;
    private readonly FExStringCipher? _previousCipher = SecureStorageModule.FallbackCipher;
    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();

    public SecureStorageModuleTests()
    {
        FExStaticLogger.Configure(() => _logger);
    }

    public void Dispose()
    {
        try
        {
            if (_storage.Exists)
                _storage.Delete(true);
        }
        finally
        {
            SecureStorageModule.AllowObfuscatedFallback = _previousAllow;
            SecureStorageModule.FallbackCipher = _previousCipher;
            FExStaticLogger.Configure(() => _previousLogger);
        }
    }

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

    [Fact]
    public void OsKeystoreAvailable_IsReturnedWithoutAWarning()
    {
        var keystore = Substitute.For<ISecureStorageService>();

        SecureStorageModule.CreateSecureStorageService(() => keystore, () => _storage, true).ShouldBeSameAs(keystore);

        _logger.DidNotReceiveWithAnyArgs().Warning(default(string)!);
    }

    [Fact]
    public void NoOsKeystore_FallsBackToTheObfuscatedFileStorage_AndSaysSoAtWarningLevel()
    {
        var service = SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, true);

        service.ShouldBeOfType<ObfuscatedFileStorageService>();
        _logger.ReceivedWithAnyArgs(1).Warning(default(string)!);
        var message = (string)_logger.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IFExLogger.Warning))
            .GetArguments()[0]!;
        message.ShouldContain(nameof(ObfuscatedFileStorageService));
        message.ShouldContain("not confidential");
        message.ShouldNotContain($"{Environment.UserName}@{Environment.MachineName}", Case.Sensitive,
            "the key's input stays out of the log");
    }

    [Fact]
    public void NoOsKeystore_WithTheFallbackRefused_Throws_AndTouchesNoStorage()
    {
        SecureStorageModule.AllowObfuscatedFallback = false;
        var storageRequested = false;

        Should.Throw<PlatformNotSupportedException>(() => SecureStorageModule.CreateSecureStorageService(
            () => null,
            () =>
            {
                storageRequested = true;
                return _storage;
            },
            true));

        storageRequested.ShouldBeFalse();
    }

    [Fact]
    public void NoOsKeystore_WithAFallbackCipher_EncryptsUnderThatCipher()
    {
        var appCipher = new FExStringCipher("an application secret", FExStringCipher.MinIterations);
        SecureStorageModule.FallbackCipher = appCipher;

        var service = SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, true);
        service.Set("token", "hunter2");

        var cipherStorage = new DirectoryInfo(Path.Combine(_storage.FullName, SecureStorageModule.FallbackCipherDirectory));
        new ObfuscatedFileStorageService(appCipher, cipherStorage).Get<string>("token").ShouldBe("hunter2");
        Should.Throw<FExDecryptionException>(() => new ObfuscatedFileStorageService(cipherStorage).Get<string>("token"));
        _logger.ReceivedWithAnyArgs(1).Warning(default(string)!);
    }

    [Fact]
    public void FallbackCipher_KeepsItsFilesApartFromTheMachineKeyedOnes()
    {
        SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, true).Set("token", "machine-keyed");

        SecureStorageModule.FallbackCipher = new FExStringCipher("an application secret", FExStringCipher.MinIterations);
        var withCipher = SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, true);
        withCipher.Set("token", "app-keyed");

        SecureStorageModule.FallbackCipher = null;
        var machineKeyed = SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, true);

        machineKeyed.Get<string>("token").ShouldBe("machine-keyed", "switching the cipher on must not overwrite what was stored without it");
        withCipher.Get<string>("token").ShouldBe("app-keyed");
        File.Exists(Path.Combine(_storage.FullName, SecureStorageModule.FallbackCipherDirectory, "token.sfex")).ShouldBeTrue();
    }

    [Fact]
    public void WithoutTheKeystoreProbeCompiledIn_BlamesTheBuild_NotTheMachine()
    {
        var service = SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, false);
        SecureStorageModule.AllowObfuscatedFallback = false;
        var refusal = Should.Throw<PlatformNotSupportedException>(
            () => SecureStorageModule.CreateSecureStorageService(() => null, () => _storage, false));

        service.ShouldBeOfType<ObfuscatedFileStorageService>();
        var warning = (string)_logger.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IFExLogger.Warning))
            .GetArguments()[0]!;

        foreach (var message in new[] { warning, refusal.Message })
        {
            message.ShouldContain("below net5.0");
            message.ShouldNotContain("No OS keystore is available");
        }
    }
}
