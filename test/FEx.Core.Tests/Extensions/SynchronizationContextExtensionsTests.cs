using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Extensions;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace FEx.Core.Tests.Extensions;

// FExStaticLogger.ErrorLogged is a process-wide event; keep these tests off each other's toes.
[Collection(nameof(SynchronizationContextExtensionsTests))]
public sealed class SynchronizationContextExtensionsTests
{
    /// <summary>
    /// The deadlock heuristic fires on a ThreadPool timer thread with no caller to catch it; rethrowing there
    /// terminates the whole process, so it must only log.
    /// </summary>
    [Fact]
    public void DeadlockCallback_DoesNotThrow_AndLogsAnError()
    {
        var logged = new ConcurrentQueue<FExErrorEventArgs>();
        void OnLogged(object? _, FExErrorEventArgs e) => logged.Enqueue(e);

        FExStaticLogger.ErrorLogged += OnLogged;

        try
        {
            Should.NotThrow(() => SynchronizationContextExtensions.Callback(new StackTrace()));
        }
        finally
        {
            FExStaticLogger.ErrorLogged -= OnLogged;
        }

        // The deadlock report itself - or, on a runtime where AttachedException cannot be built at all
        // (its stack-trace reflection fails), the fallback report. Never silence.
        logged.ShouldNotBeEmpty();
        logged.ShouldContain(e => CanBuildAttachedException()
            ? e.Message!.StartsWith("Deadlock assumed")
            : e.Message == "Failed to report an assumed deadlock.");
    }

    private static bool CanBuildAttachedException()
    {
        try
        {
            _ = new AttachedException("probe", new StackTrace());

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [Fact]
    public void DeadlockCallback_ReportsFailure_WhenStateIsInvalid()
    {
        var logged = new ConcurrentQueue<FExErrorEventArgs>();
        void OnLogged(object? _, FExErrorEventArgs e) => logged.Enqueue(e);

        FExStaticLogger.ErrorLogged += OnLogged;

        try
        {
            Should.NotThrow(() => SynchronizationContextExtensions.Callback("not a stack trace"));
        }
        finally
        {
            FExStaticLogger.ErrorLogged -= OnLogged;
        }

        logged.ShouldContain(e => e.Message == "Failed to report an assumed deadlock.");
    }

    [Fact]
    public void DeadlockCallback_DoesNotThrow_WhenAnErrorLoggedSubscriberThrows()
    {
        static void Throwing(object? _, FExErrorEventArgs e) => throw new InvalidOperationException("subscriber");

        FExStaticLogger.ErrorLogged += Throwing;

        try
        {
            Should.NotThrow(() => SynchronizationContextExtensions.Callback(new StackTrace()));
            Should.NotThrow(() => SynchronizationContextExtensions.Callback("not a stack trace"));
        }
        finally
        {
            FExStaticLogger.ErrorLogged -= Throwing;
        }
    }
}
