using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Runtime.CompilerServices;
#if !NETSTANDARD
using System.Threading;
#endif

namespace FEx.Agnostics.Abstractions;

/// <summary>
/// Provides static access to core FEx services at the agnostics layer.
/// This class MUST be initialized by higher-level layers (e.g., FEx.Core) before use.
/// </summary>
public sealed class FExAgnosticsStatics
{
#if NETSTANDARD
    private static readonly object _lockObject = new();
#else
    private static readonly Lock _lockObject = new();
#endif
    private static IAsyncHelper _asyncHelper;
    private static bool _hasBeenInitialized;

    /// <summary>
    /// Gets the <see cref="IAsyncHelper" /> instance.
    /// <br />
    /// ⚠️ This property must be initialized by calling <see cref="Configure" /> before first use.
    /// <br />
    /// ⚠️ Typically initialized automatically by FEx.Core during application startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before initialization. Call <see cref="Configure" /> first.
    /// </exception>
    public static IAsyncHelper AsyncHelper
    {
        get
        {
            if (_asyncHelper is null)
                ThrowNotInitializedException();

            return _asyncHelper;
        }
    }

    /// <summary>
    /// Configures the static services. This should be called during application initialization.
    /// </summary>
    /// <param name="asyncHelper">The <see cref="IAsyncHelper" /> implementation to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncHelper" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when already initialized with a different instance.</exception>
    public static void Configure(IAsyncHelper asyncHelper)
    {
        if (asyncHelper is null)
            throw new ArgumentNullException(nameof(asyncHelper));

        lock (_lockObject)
        {
            if (_hasBeenInitialized && !ReferenceEquals(_asyncHelper, asyncHelper))
                throw new InvalidOperationException(
                    $"{nameof(FExAgnosticsStatics)} has already been initialized with a different {nameof(IAsyncHelper)} instance. "
                    + "Cannot reinitialize with a different instance.");

            _asyncHelper = asyncHelper;
            _hasBeenInitialized = true;
        }
    }

    /// <summary>
    /// Resets the configuration. Used primarily for testing.
    /// </summary>
    internal static void Reset()
    {
        lock (_lockObject)
        {
            _asyncHelper = null;
            _hasBeenInitialized = false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNotInitializedException()
    {
        throw new InvalidOperationException(
            $"{nameof(FExAgnosticsStatics)}.{nameof(AsyncHelper)} has not been initialized. "
            + $"Call {nameof(Configure)} during application startup, or ensure FEx.Core is properly initialized via FExServiceProvider.Initialize().");
    }
}