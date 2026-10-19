using FEx.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Logging.Tests;

/// <summary>
/// Pins the static logger factory fallback. The statics are process-wide, so the tests reset them first and
/// the class is the only one in this assembly that touches them.
/// </summary>
public sealed class FExLoggingStaticsTests
{
    [Fact]
    public void LoggerFactory_NothingConfigured_Throws()
    {
        FExLoggingStatics.SetDefaults();

        Should.Throw<ArgumentNullException>(() => FExLoggingStatics.LoggerFactory);
    }

    [Fact]
    public void LoggerFactory_AfterInitialize_ReturnsTheGivenFactory()
    {
        try
        {
            ILoggerFactory factory = NullLoggerFactory.Instance;

            FExLoggingStatics.Initialize(factory);

            FExLoggingStatics.LoggerFactory.ShouldBeSameAs(factory);
        }
        finally
        {
            FExLoggingStatics.SetDefaults();
        }
    }
}
