using FEx.Agnostics.Abstractions.Extensions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class GuardExtensionsTests
{
    [Fact]
    public void GuardProperty_NonNull_ReturnsTheValue()
    {
        var value = new object();

        value.GuardProperty().ShouldBeSameAs(value);
    }

    [Fact]
    public void GuardProperty_Null_ThrowsWithTheMessageAndParamName()
    {
        object? value = null;

        var ex = Should.Throw<ArgumentNullException>(() => value.GuardProperty("custom message", "myParam"));

        ex.ParamName.ShouldBe("myParam");
        ex.Message.ShouldContain("custom message");
    }

    [Fact]
    public void GuardProperty_NullWithoutMessage_UsesTheDefaultMessage()
    {
        object? value = null;

        Should.Throw<ArgumentNullException>(() => value.GuardProperty())
              .Message.ShouldContain("Precondition not met.");
    }

    [Fact]
    public void Guard_NonNull_ReturnsTheValue()
    {
        var value = new object();

        value.Guard("value").ShouldBeSameAs(value);
    }

    [Fact]
    public void Guard_Null_ThrowsWithTheParamNameAndDefaultMessage()
    {
        object? value = null;

        var ex = Should.Throw<ArgumentNullException>(() => value.Guard("myParam"));

        ex.ParamName.ShouldBe("myParam");
        ex.Message.ShouldContain("Precondition not met.");
    }

    [Fact]
    public void Guard_NullWithMessage_ThrowsWithTheMessage()
    {
        object? value = null;

        var ex = Should.Throw<ArgumentNullException>(() => value.Guard("myParam", "custom message"));

        ex.ParamName.ShouldBe("myParam");
        ex.Message.ShouldContain("custom message");
    }

    [Fact]
    public void Guard_PredicateHolds_ReturnsTheValue() =>
        5.Guard(x => x > 0, "value").ShouldBe(5);

    [Fact]
    public void Guard_PredicateFails_ThrowsWithTheParamNameAndDefaultMessage()
    {
        var ex = Should.Throw<ArgumentNullException>(() => (-1).Guard(x => x > 0, "myParam"));

        ex.ParamName.ShouldBe("myParam");
        ex.Message.ShouldContain("Precondition not met.");
    }

    [Fact]
    public void Guard_PredicateFailsWithMessage_ThrowsWithTheMessage()
    {
        var ex = Should.Throw<ArgumentNullException>(() => (-1).Guard(x => x > 0, "myParam", "custom message"));

        ex.ParamName.ShouldBe("myParam");
        ex.Message.ShouldContain("custom message");
    }

    [Fact]
    public void Guard_NullPredicate_ThrowsNamingThePredicate()
    {
        Func<int, bool>? predicate = null;

        Should.Throw<ArgumentNullException>(() => 1.Guard(predicate!, "value", null))
              .ParamName.ShouldBe("predicate");
    }

    // The predicate overloads only run the predicate: a null value that satisfies it passes through. They therefore
    // must not carry [ContractAnnotation("value: null => stop")], which would hide the missing null check from R#.
    [Fact]
    public void Guard_NullValueSatisfyingThePredicate_IsReturnedWithoutAnyNullCheck()
    {
        string? value = null;

        value.Guard(_ => true, "value").ShouldBeNull();
        value.Guard(_ => true, "value", "message").ShouldBeNull();
    }
}
