using FEx.Core.Abstractions.Helpers;
using Shouldly;
using System;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class ReflectionHelperTests
{
    [Fact]
    public void GetPropertyValue_PrivatePropertyDeclaredOnBaseClass_IsFoundFromDerivedInstance() =>
        new Derived().GetPropertyValue("HiddenProperty").ShouldBe("base-property");

    [Fact]
    public void SetPropertyValue_PrivatePropertyDeclaredOnBaseClass_IsFoundFromDerivedInstance()
    {
        var derived = new Derived();

        derived.SetPropertyValue("HiddenProperty", "changed");

        derived.GetPropertyValue("HiddenProperty").ShouldBe("changed");
    }

    [Fact]
    public void GetFieldValue_PrivateFieldDeclaredOnBaseClass_IsFoundFromDerivedInstance() =>
        new Derived().GetFieldValue("_hiddenField").ShouldBe(42);

    [Fact]
    public void GetPropertyValue_MissingProperty_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new Derived().GetPropertyValue("Missing"))
            .ParamName.ShouldBe("propertyName");

    [Fact]
    public void SetPropertyValue_MissingProperty_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new Derived().SetPropertyValue("Missing", "x"))
            .ParamName.ShouldBe("propertyName");

    [Fact]
    public void GetFieldValue_MissingField_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new Derived().GetFieldValue("Missing"))
            .ParamName.ShouldBe("fieldName");

    private class Base(int hiddenField)
    {
        private readonly int _hiddenField = hiddenField;

        private string HiddenProperty { get; set; } = "base-property";

        // Reads the private members so the compiler does not flag them as unused.
        public override string ToString() => $"{_hiddenField}{HiddenProperty}";
    }

    private sealed class Derived() : Base(42);
}
