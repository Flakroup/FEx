using FEx.Agnostics.Abstractions.Extensions;
using Shouldly;
using System;
using System.Diagnostics;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class ExceptionExtensionsTests
{
    [Fact]
    public void SetStackTrace_DoesNotThrowTypeInitializationException()
    {
        var exception = new InvalidOperationException("boom");

        Should.NotThrow(() => exception.SetStackTrace(new StackTrace()));
    }

    [Fact]
    public void SetStackTrace_StoresTheSuppliedTraceWithoutTrailingNewline()
    {
        var stack = new StackTrace();

        var exception = new InvalidOperationException("boom").SetStackTrace(stack);

        exception.StackTrace.ShouldBe(stack.ToString().TrimEnd('\r', '\n'));
        exception.StackTrace.ShouldNotEndWith("\n");
    }

    [Fact]
    public void Stack_IncludesInnerExceptionStackTraces()
    {
        var inner = Throw("inner");
        var outer = new InvalidOperationException("outer", inner);

        var stack = outer.Stack();

        stack.ShouldContain(nameof(Throw));
    }

    [Fact]
    public void Stack_WalksWholeChain()
    {
        var innermost = Throw("innermost");
        var middle = new InvalidOperationException("middle", innermost);
        var outer = new InvalidOperationException("outer", middle);

        var stack = outer.Stack();

        stack.ShouldContain(innermost.StackTrace!);
    }

    private static Exception Throw(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
