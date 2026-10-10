using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

public class BaseMemberHasParamsAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<BaseMemberHasParamsAnalyzer>(source);

    [Fact]
    public Task Reports_an_override_that_drops_params() =>
        VerifyAsync("""
            class Base { public virtual void M(params int[] a) { } }
            class Derived : Base { public override void M(int[] {|FEX0005:a|}) { } }
            """);

    [Fact]
    public Task Reports_the_last_parameter_of_an_override_with_several() =>
        VerifyAsync("""
            class Base { public virtual int M(string s, params object[] rest) => 0; }
            class Derived : Base { public override int M(string s, object[] {|FEX0005:rest|}) => 1; }
            """);

    [Fact]
    public Task Reports_a_generic_override_through_a_constructed_base() =>
        VerifyAsync("""
            class Base<T> { public virtual void M(params T[] a) { } }
            class Derived : Base<int> { public override void M(int[] {|FEX0005:a|}) { } }
            """);

    [Fact]
    public Task Ignores_an_override_that_keeps_params() =>
        VerifyAsync("""
            class Base { public virtual void M(params int[] a) { } }
            class Derived : Base { public override void M(params int[] a) { } }
            """);

    [Fact]
    public Task Ignores_a_base_without_params() =>
        VerifyAsync("""
            class Base { public virtual void M(int[] a) { } }
            class Derived : Base { public override void M(int[] a) { } }
            """);

    [Fact]
    public Task Ignores_a_method_that_overrides_nothing() =>
        VerifyAsync("""
            class Base { public void M(params int[] a) { } }
            class Derived : Base { public new void M(int[] a) { } }
            """);

    [Fact]
    public Task Ignores_an_override_without_parameters() =>
        VerifyAsync("""
            class Base { public virtual void M() { } }
            class Derived : Base { public override void M() { } }
            """);

    [Fact]
    public Task Reports_an_indexer_override_that_drops_params() =>
        VerifyAsync("""
            class Base { public virtual int this[params int[] i] { get => 0; set { } } }
            class Derived : Base { public override int this[int[] {|FEX0005:i|}] { get => 1; set { } } }
            """);

    [Fact]
    public Task Ignores_an_indexer_override_that_keeps_params_and_a_property_override() =>
        VerifyAsync("""
            class Base { public virtual int this[params int[] i] { get => 0; set { } } public virtual int P => 0; }
            class Derived : Base { public override int this[params int[] i] { get => 1; set { } } public override int P => 1; }
            """);

    // ReSharper 2026.2.3.1 stays silent on an interface implementation.
    [Fact]
    public Task Ignores_an_interface_implementation_that_drops_params() =>
        VerifyAsync("""
            interface I { void M(params int[] a); }
            class Impl : I { public void M(int[] a) { } }
            """);

    [Fact]
    public Task Names_the_override_without_the_params_it_does_not_declare() =>
        AnalyzerTestHelper.VerifyAsync<BaseMemberHasParamsAnalyzer>(
            """
            class Base { public virtual void M(params int[] a) { } }
            class Derived : Base { public override void M(int[] {|#0:a|}) { } }
            """,
            expected: [new DiagnosticResult("FEX0005", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Derived.M(int[])", "Base.M(params int[])")]);
}
