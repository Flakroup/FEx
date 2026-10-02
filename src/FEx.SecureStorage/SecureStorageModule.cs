using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.SecureStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
#if NET5_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
#endif

namespace FEx.SecureStorage;

[Register(typeof(SecureStorageModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class SecureStorageModule : InitializeModule<ISecureStorageContainer, IServiceCollection>
{
    /// <summary>
    /// Picks the most secure <see cref="ISecureStorageService" /> available on the
    /// current OS. Order: Windows DPAPI &gt; macOS Keychain &gt; Linux libsecret &gt;
    /// cross-platform file fallback. If libsecret is missing on Linux the factory
    /// silently falls back to the file implementation.
    /// </summary>
    /// <remarks>
    /// The last resort is <see cref="ObfuscatedFileStorageService" />, and it earns its name: with no OS
    /// keystore to lean on it can only key itself off public machine and user identifiers, so it obscures
    /// values rather than keeping them secret. On the targets below <c>net5.0</c> the branch above is
    /// compiled out entirely and that fallback is the only implementation this factory can return.
    /// </remarks>
    [Factory(Scope.SingleInstance)]
    public static ISecureStorageService CreateSecureStorageService()
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
        return new ObfuscatedFileStorageService();
    }

    protected override void RegisterServices(ISecureStorageContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddSingletonServiceUsingContainer(container);
    }
}