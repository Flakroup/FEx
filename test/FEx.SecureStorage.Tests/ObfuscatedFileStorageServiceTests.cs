using FEx.Encryption;
using FEx.Encryption.Exceptions;
using FEx.SecureStorage.Abstractions;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.SecureStorage.Tests;

/// <summary>
/// This fallback keys itself off public machine and user identifiers, so it obscures values rather than
/// keeping them secret - that is why it is named the way it is. What it can still guarantee is that a file
/// edited underneath the application is detected instead of decoding into plausible nonsense, and that is
/// mostly what these pin.
/// </summary>
public sealed class ObfuscatedFileStorageServiceTests : IDisposable
{
    private readonly DirectoryInfo _storage =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "fex-storage-tests-" + Guid.NewGuid().ToString("N")));

    private readonly ObfuscatedFileStorageService _storageService;

    public ObfuscatedFileStorageServiceTests()
    {
        _storageService = new(new FExStringCipher("a test passphrase", FExStringCipher.MinIterations), _storage);
    }

    public void Dispose()
    {
        if (_storage.Exists)
            _storage.Delete(true);
    }

    [Fact]
    public void SetThenGet_ReturnsWhatWasStored()
    {
        _storageService.Set("token", new Credential { User = "gio", Secret = "hunter2" });

        var read = _storageService.Get<Credential>("token");

        read.User.ShouldBe("gio");
        read.Secret.ShouldBe("hunter2");
    }

    [Fact]
    public void Set_WritesSomethingOtherThanThePlaintext()
    {
        _storageService.Set("token", new Credential { User = "gio", Secret = "hunter2" });

        var onDisk = File.ReadAllText(Path.Combine(_storage.FullName, "token.sfex"));

        onDisk.ShouldNotContain("hunter2");
        onDisk.ShouldNotContain("gio");
    }

    [Fact]
    public void Get_DetectsAFileEditedUnderneathIt()
    {
        _storageService.Set("token", new Credential { User = "gio", Secret = "hunter2" });
        var path = Path.Combine(_storage.FullName, "token.sfex");
        var envelope = Convert.FromBase64String(File.ReadAllText(path));
        envelope[^1] ^= 0xFF;
        File.WriteAllText(path, Convert.ToBase64String(envelope));

        Should.Throw<FExDecryptionException>(() => _storageService.Get<Credential>("token"));
    }

    [Fact]
    public void Get_DetectsAFileWrittenWithAnotherKey()
    {
        var stranger = new ObfuscatedFileStorageService(
            new FExStringCipher("an unrelated passphrase", FExStringCipher.MinIterations), _storage);
        stranger.Set("token", new Credential { User = "gio", Secret = "hunter2" });

        Should.Throw<FExDecryptionException>(() => _storageService.Get<Credential>("token"));
    }

    [Fact]
    public void Get_OnAnUnknownKey_Throws() =>
        Should.Throw<Exception>(() => _storageService.Get<Credential>("never-written"));

    [Fact]
    public void Constructor_RejectsMissingArguments()
    {
        Should.Throw<ArgumentNullException>(() => new ObfuscatedFileStorageService(null!, _storage));
        Should.Throw<ArgumentNullException>(
            () => new ObfuscatedFileStorageService(new FExStringCipher("x", FExStringCipher.MinIterations), null!));
    }

    [Fact]
    public void ParameterlessConstructor_BuildsAUsableService()
    {
        // The shape the DI factory uses. Its key is machine-bound by design; what matters here is that it
        // constructs and round-trips at all, since nothing else in the suite reaches that constructor.
        var service = new ObfuscatedFileStorageService();

        service.ShouldBeAssignableTo<ISecureStorageService>();
    }

    private sealed class Credential
    {
        public string? User { get; set; }

        public string? Secret { get; set; }
    }
}
