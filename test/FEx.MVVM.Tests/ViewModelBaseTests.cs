using FEx.MVVM.Rx.BaseObjects;
using Shouldly;
using Xunit;

namespace FEx.MVVM.Tests;

public class ViewModelBaseTests
{
    private sealed class TestViewModel : ViewModelBase;

    [Fact]
    public void Equality_operators_accept_null_operands()
    {
        TestViewModel? model = new();
        TestViewModel? none = null;

        (model == none).ShouldBeFalse();
        (none == model).ShouldBeFalse();
        (model != none).ShouldBeTrue();
        (none == null).ShouldBeTrue();
    }

    [Fact]
    public void Title_defaults_to_an_empty_string() => new TestViewModel().Title.ShouldBe(string.Empty);
}
