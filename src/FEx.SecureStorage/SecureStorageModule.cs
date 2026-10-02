using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Encryption;
using FEx.SecureStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;
using System.IO;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace FEx.SecureStorage;

[Register(typeof(SecureStorageModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class SecureStorageModule : InitializeModule<ISecureStorageContainer, IServiceCollection>
{
    /// <summary>
    /// Whether <see cref="CreateSecureStorageService()" /> may fall back to <see cref="ObfuscatedFileStorageService" />
    /// when no OS keystore is available. Defaults to <c>true</c>; set it to <c>false</c> before the container
    /// resolves <see cref="ISecureStorageService" /> to make the factory throw
    /// <see cref="PlatformNotSupportedException" /> instead of downgrading to obfuscation.
    /// </summary>
    public static bool AllowObfuscatedFallback { get; set; } = true;

    /// <summary>
    /// Cipher the file fallback uses instead of its machine-bound key. Build it from a secret only the
    /// application knows to get actual confidentiality from the fallback; leave it <c>null</c> and the fallback
    /// only obfuscates. Values written under one key cannot be read under the other.
    /// </summary>
    public static FExStringCipher? FallbackCipher { get; set; }

    /// <summary>
    /// Picks the most secure <see cref="ISecureStorageService" /> available on the
    /// current OS. Order: Windows DPAPI &gt; macOS Keychain &gt; Linux libsecret &gt;
    /// cross-platform file fallback. Falling back is logged at warning level and can be refused with
    /// <see cref="AllowObfuscatedFallback" />.
    /// </summary>
    /// <remarks>
    /// The last resort is <see cref="ObfuscatedFileStorageService" />, and it earns its name: with no OS
    /// keystore to lean on and no <see cref="FallbackCipher" /> it can only key itself off public machine and
    /// user identifiers, so it obscures values rather than keeping them secret. On the targets below
    /// <c>net5.0</c> the OS keystores are compiled out entirely and that fallback is the only implementation
    /// this factory can return.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">
    /// No OS keystore is available and <see cref="AllowObfuscatedFallback" /> is <c>false</c>.
    /// </exception>
    [Factory(Scope.SingleInstance)]
    public static ISecureStorageService CreateSecureStorageService() =>
        CreateSecureStorageService(CreateOsKeystoreService, ObfuscatedFileStorageService.GetDefaultStorage);

    /// <summary>The selection itself, with the OS probe and the fallback directory supplied - reachable from a test on any OS.</summary>
    internal static ISecureStorageService CreateSecureStorageService(Func<ISecureStorageService?> osKeystore,
                                                                     Func<DirectoryInfo> fallbackStorage)
    {
        var service = osKeystore();

        if (service is not null)
            return service;

        if (!AllowObfuscatedFallback)
            throw new PlatformNotSupportedException(
                $"No OS keystore is available and {nameof(SecureStorageModule)}.{nameof(AllowObfuscatedFallback)} is false.");

        var cipher = FallbackCipher;

        if (cipher is null)
        {
            FExStaticLogger.Warning(
                $"No OS keystore is available; secure storage falls back to {nameof(ObfuscatedFileStorageService)} keyed on public machine and user identifiers - stored values are obfuscated, not confidential. Set {nameof(SecureStorageModule)}.{nameof(FallbackCipher)} to encrypt them with an application secret, or {nameof(AllowObfuscatedFallback)} to false to refuse the fallback.");

            return new ObfuscatedFileStorageService(fallbackStorage());
        }

        FExStaticLogger.Warning(
            $"No OS keystore is available; secure storage falls back to {nameof(ObfuscatedFileStorageService)} encrypted with the application-supplied {nameof(FallbackCipher)}.");

        return new ObfuscatedFileStorageService(cipher, fallbackStorage());
    }

    protected override void RegisterServices(ISecureStorageContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddSingletonServiceUsingContainer(container);
    }

    private static ISecureStorageService? CreateOsKeystoreService()
    {
#if NET5_0_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsDpapiSecureStorageService();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsKeychainSecureStorageService();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            try
            {
                return new LinuxLibsecretSecureStorageService();
            }
            catch (PlatformNotSupportedException)
            {
                // libsecret-1.so.0 not installed - fall through to file fallback.
            }
#endif
        return null;
    }
}
