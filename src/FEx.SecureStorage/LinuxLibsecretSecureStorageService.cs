#if NET5_0_OR_GREATER
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FEx.SecureStorage;

/// <summary>
/// Linux-only secure storage backed by libsecret (the freedesktop Secret Service API,
/// used by GNOME Keyring and KWallet via secret-service). Items are tagged with the
/// configured service name (default <c>com.flakroup.fex.securestorage</c>) and the
/// supplied key as the <c>key</c> attribute. Requires <c>libsecret-1.so.0</c> to be
/// installed on the target machine - the constructor throws
/// <see cref="PlatformNotSupportedException"/> if the library cannot be loaded.
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxLibsecretSecureStorageService : ISecureStorageService
{
    private const string Libsecret = "libsecret-1.so.0"; // gitleaks:allow
    private const string DefaultServiceName = "com.flakroup.fex.securestorage";

    private readonly string _serviceName;
    private readonly IntPtr _schema;

    public LinuxLibsecretSecureStorageService()
        : this(DefaultServiceName)
    {
    }

    public LinuxLibsecretSecureStorageService(string serviceName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException("LinuxLibsecretSecureStorageService requires Linux.");

        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("serviceName must be a non-empty string.", nameof(serviceName));

        _serviceName = serviceName;

        try
        {
            _schema = BuildSchema(serviceName);
        }
        catch (DllNotFoundException ex)
        {
            throw new PlatformNotSupportedException(
                "libsecret-1.so.0 was not found. Install libsecret on the target Linux machine to use LinuxLibsecretSecureStorageService.",
                ex);
        }
    }

    public T Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("key must be a non-empty string.", nameof(key));

        var passwordPtr = secret_password_lookup_sync(
            schema: _schema,
            cancellable: IntPtr.Zero,
            error: out var errorPtr,
            attribute1: "key",
            value1: key,
            sentinel: IntPtr.Zero);

        ThrowIfGError(errorPtr, "secret_password_lookup_sync");

        if (passwordPtr == IntPtr.Zero)
            throw new System.IO.FileNotFoundException($"libsecret item not found for key '{key}' in service '{_serviceName}'.");

        try
        {
            var json = Marshal.PtrToStringAnsi(passwordPtr);
            return json is null
                ? throw new InvalidOperationException("libsecret returned a null UTF-8 password.")
                : json.FromJson<T>();
        }
        finally
        {
            secret_password_free(passwordPtr);
        }
    }

    public void Set(string key, object content)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("key must be a non-empty string.", nameof(key));

        var json = content.ToJson();

        var stored = secret_password_store_sync(
            schema: _schema,
            collection: null, // SECRET_COLLECTION_DEFAULT
            label: $"{_serviceName}:{key}",
            password: json,
            cancellable: IntPtr.Zero,
            error: out var errorPtr,
            attribute1: "key",
            value1: key,
            sentinel: IntPtr.Zero);

        ThrowIfGError(errorPtr, "secret_password_store_sync");

        if (!stored)
            throw new InvalidOperationException("secret_password_store_sync returned false without setting a GError.");
    }

    private static IntPtr BuildSchema(string serviceName)
    {
        // libsecret schemas are usually statically defined in C; for managed code
        // we use secret_schema_new which allocates a heap schema we own.
        // Attribute: "key" -> SECRET_SCHEMA_ATTRIBUTE_STRING (0).
        return secret_schema_new(
            name: serviceName,
            flags: 0, // SECRET_SCHEMA_NONE
            attribute1: "key",
            type1: 0,
            sentinel: IntPtr.Zero);
    }

    private static void ThrowIfGError(IntPtr errorPtr, string operation)
    {
        if (errorPtr == IntPtr.Zero)
            return;

        // GError layout: { GQuark domain; gint code; gchar* message; }
        // gchar* message lives at offset sizeof(GQuark) + sizeof(gint) which is 8 on
        // 64-bit Linux glib. Reading via pointer arithmetic.
        var messagePtr = Marshal.ReadIntPtr(errorPtr, IntPtr.Size);
        var message = Marshal.PtrToStringAnsi(messagePtr) ?? "unknown libsecret error";
        g_error_free(errorPtr);
        throw new InvalidOperationException($"{operation} failed: {message}");
    }

    [DllImport(Libsecret, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr secret_schema_new(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int flags,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string attribute1,
        int type1,
        IntPtr sentinel);

    [DllImport(Libsecret, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool secret_password_store_sync(
        IntPtr schema,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string collection,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        IntPtr cancellable,
        out IntPtr error,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string attribute1,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value1,
        IntPtr sentinel);

    [DllImport(Libsecret, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr secret_password_lookup_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string attribute1,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value1,
        IntPtr sentinel);

    [DllImport(Libsecret, CallingConvention = CallingConvention.Cdecl)]
    private static extern void secret_password_free(IntPtr password);

    [DllImport("libglib-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_error_free(IntPtr error);
}
#endif
