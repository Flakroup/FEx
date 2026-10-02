using FEx.Core.Abstractions.Extensions;
using Shouldly;
using System;
using System.Diagnostics;
using Xunit;

namespace FEx.Core.Tests.Extensions;

public sealed class SynchronizationContextExtensionsTests
{
    /// <summary>
    /// The deadlock heuristic fires on a ThreadPool timer thread with no caller to catch it; rethrowing there
    /// terminates the whole process, so it must only log.
    /// </summary>
    [Fact]
    public void DeadlockCallback_DoesNotThrow()
    {
        Should.NotThrow(() => SynchronizationContextExtensions.Callback(new StackTrace()));
    }
}
