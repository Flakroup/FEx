#if NET5_0_OR_GREATER
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace FEx.SecureStorage;

/// <summary>
/// macOS-only secure storage backed by the Security framework Keychain Services
/// (generic password items). Items are tagged with the configured service name
/// (default <c>com.flakroup.fex.securestorage</c>) and the supplied key as the
/// account name. Values are JSON-serialized before being stored as UTF-8 bytes.
/// </summary>
[SupportedOSPlatform("macos")]
public class MacOsKeychainSecureStorageService : ISecureStorageService
{
    private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
    private const string DefaultServiceName = "com.flakroup.fex.securestorage";

    private const int errSecSuccess = 0;
    private const int errSecItemNotFound = -25300;
    private const int errSecDuplicateItem = -25299;

    private readonly string _serviceName;

    public MacOsKeychainSecureStorageService()
        : this(DefaultServiceName)
    {
    }

    public MacOsKeychainSecureStorageService(string serviceName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsKeychainSecureStorageService requires macOS.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("serviceName must be a non-empty string.", nameof(serviceName));

        _serviceName = serviceName;
    }

    public T Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("key must be a non-empty string.", nameof(key));

        var serviceBytes = Encoding.UTF8.GetBytes(_serviceName);
        var accountBytes = Encoding.UTF8.GetBytes(key);

        var status = SecKeychainFindGenericPassword(
            keychainOrArray: IntPtr.Zero,
            serviceNameLength: (uint)serviceBytes.Length,
            serviceName: serviceBytes,
            accountNameLength: (uint)accountBytes.Length,
            accountName: accountBytes,
            passwordLength: out var passwordLength,
            passwordData: out var passwordPtr,
            itemRef: IntPtr.Zero);

        if (status == errSecItemNotFound)
            throw new FileNotFoundException($"Keychain item not found for key '{key}' in service '{_serviceName}'.");

        ThrowIfError(status, "SecKeychainFindGenericPassword");

        try
        {
            var bytes = new byte[passwordLength];
            Marshal.Copy(passwordPtr, bytes, 0, (int)passwordLength);
            var json = Encoding.UTF8.GetString(bytes);

            return json.FromJson<T>();
        }
        finally
        {
            if (passwordPtr != IntPtr.Zero)
                SecKeychainItemFreeContent(IntPtr.Zero, passwordPtr);
        }
    }

    public void Set(string key, object content)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("key must be a non-empty string.", nameof(key));

        var serviceBytes = Encoding.UTF8.GetBytes(_serviceName);
        var accountBytes = Encoding.UTF8.GetBytes(key);
        var passwordBytes = Encoding.UTF8.GetBytes(content.ToJson());

        var status = SecKeychainAddGenericPassword(
            keychain: IntPtr.Zero,
            serviceNameLength: (uint)serviceBytes.Length,
            serviceName: serviceBytes,
            accountNameLength: (uint)accountBytes.Length,
            accountName: accountBytes,
            passwordLength: (uint)passwordBytes.Length,
            passwordData: passwordBytes,
            itemRef: out var itemRef);

        if (status == errSecDuplicateItem)
        {
            // Find the existing item and overwrite its data.
            var findStatus = SecKeychainFindGenericPasswordWithRef(
                keychainOrArray: IntPtr.Zero,
                serviceNameLength: (uint)serviceBytes.Length,
                serviceName: serviceBytes,
                accountNameLength: (uint)accountBytes.Length,
                accountName: accountBytes,
                passwordLength: out _,
                passwordData: out var existingPasswordPtr,
                itemRef: out var existingItemRef);

            ThrowIfError(findStatus, "SecKeychainFindGenericPassword (for update)");

            try
            {
                var modifyStatus = SecKeychainItemModifyAttributesAndData(
                    itemRef: existingItemRef,
                    attrList: IntPtr.Zero,
                    length: (uint)passwordBytes.Length,
                    data: passwordBytes);

                ThrowIfError(modifyStatus, "SecKeychainItemModifyAttributesAndData");
            }
            finally
            {
                if (existingPasswordPtr != IntPtr.Zero)
                    SecKeychainItemFreeContent(IntPtr.Zero, existingPasswordPtr);

                if (existingItemRef != IntPtr.Zero)
                    CFRelease(existingItemRef);
            }

            return;
        }

        ThrowIfError(status, "SecKeychainAddGenericPassword");

        if (itemRef != IntPtr.Zero)
            CFRelease(itemRef);
    }

    private static void ThrowIfError(int status, string operation)
    {
        if (status != errSecSuccess)
            throw new InvalidOperationException($"{operation} failed with OSStatus {status}.");
    }

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainFindGenericPassword(
        IntPtr keychainOrArray,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        IntPtr itemRef);

    [DllImport(SecurityFramework, EntryPoint = "SecKeychainFindGenericPassword")]
    private static extern int SecKeychainFindGenericPasswordWithRef(
        IntPtr keychainOrArray,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainAddGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        uint passwordLength,
        byte[] passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainItemModifyAttributesAndData(
        IntPtr itemRef,
        IntPtr attrList,
        uint length,
        byte[] data);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}
#endif
