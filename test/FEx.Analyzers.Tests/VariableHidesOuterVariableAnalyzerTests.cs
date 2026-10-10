using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FEx.Analyzers.Tests;

public class VariableHidesOuterVariableAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<VariableHidesOuterVariableAnalyzer>(source);

    [Fact]
    public Task Reports_a_lambda_parameter_named_like_an_outer_local() =>
        VerifyAsync("""
            using System;
            class C { void M() { int x = 1; Func<int, int> f = {|FEX0003:x|} => x; } }
            """);

    [Fact]
    public Task Reports_a_parenthesized_lambda_parameter_named_like_an_outer_parameter() =>
        VerifyAsync("""
            using System;
            class C { void M(int x) { Func<int, int, int> f = (a, {|FEX0003:x|}) => a + x; } }
            """);

    [Fact]
    public Task Reports_an_anonymous_method_local_named_like_an_outer_local() =>
        VerifyAsync("""
            using System;
            class C { void M() { int x = 1; Action a = delegate { int {|FEX0003:x|} = 2; }; } }
            """);

    [Fact]
    public Task Reports_a_local_function_parameter_and_local() =>
        VerifyAsync("""
            class C
            {
                void M(int p)
                {
                    int x = 1;
                    int Local(int {|FEX0003:p|}) { int {|FEX0003:x|} = 2; return p + x; }
                }
            }
            """);

    [Fact]
    public Task Reports_foreach_catch_pattern_and_out_variables() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M(object o, int[] items)
                {
                    int a = 0, b = 0, c = 0, d = 0;
                    Action act = () =>
                    {
                        foreach (var {|FEX0003:a|} in items) { }
                        try { } catch (Exception {|FEX0003:b|}) { }
                        if (o is int {|FEX0003:c|}) { }
                        int.TryParse("1", out var {|FEX0003:d|});
                    };
                }
            }
            """);

    [Fact]
    public Task Reports_a_name_from_an_enclosing_lambda() =>
        VerifyAsync("""
            using System;
            class C { void M() { Action<int> outer = x => { Action<int> inner = {|FEX0003:x|} => { }; }; } }
            """);

    [Fact]
    public Task Reports_through_a_static_function_that_does_not_declare_the_variable() =>
        VerifyAsync("""
            using System;
            class C { void M() { int x = 1; Action a = () => { Action<int> b = {|FEX0003:x|} => { }; }; } }
            """);

    [Fact]
    public Task Ignores_a_static_lambda_or_local_function() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    int x = 1;
                    Func<int, int> f = static x => x;
                    Func<int, int> g = static delegate (int x) { return x; };
                    static int Local(int x) => x;
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_declared_after_the_function() =>
        VerifyAsync("class C { void M() { System.Func<int, int> f = x => x; int x = 1; } }");

    [Fact]
    public Task Ignores_names_in_sibling_scopes_and_fields() =>
        VerifyAsync("""
            using System;
            class C
            {
                int x;
                void M()
                {
                    { int y = 1; }
                    Func<int, int> f = y => y;
                    Func<int, int> g = x => x;
                }
            }
            """);

    [Fact]
    public Task Ignores_discards_and_nameless_declarations() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M(object o)
                {
                    int _ = 1;
                    Func<int, int, int> f = (_, __) => 1;
                    Action a = () => { if (o is int) { } };
                }
            }
            """);

    [Fact]
    public Task Ignores_locals_of_a_nested_function_when_checking_the_outer_one() =>
        VerifyAsync("""
            using System;
            class C { void M() { Action a = () => { int inner = 1; }; Action b = () => { int inner = 2; }; } }
            """);

    [Fact]
    public Task Reports_through_a_static_function_that_declares_the_variable() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = static () =>
                    {
                        int fresh = 1;
                        Action<int> b = {|FEX0003:fresh|} => { };
                    };
                }
            }
            """);

    [Fact]
    public Task Ignores_a_function_that_only_reads_a_variable_of_a_static_one() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = static () =>
                    {
                        int fresh = 1;
                        Action b = () => Console.Write(fresh);
                    };
                }
            }
            """);

    // The tests below pin the verdicts of ReSharper 2026.2.3.1 (VariableHidesOuterVariable at ERROR), probed one
    // shape per method.

    [Fact]
    public Task Reports_every_shape_ReSharper_reports() =>
        VerifyAsync("""
            using System;
            class Probes
            {
                static T F<T>(Func<T> f) => f();

                void LambdaParameterVsEarlierLocal() { int x = 1; Func<int, int> f = {|FEX0003:x|} => x; }
                void LocalFunctionParameterVsEarlierLocal() { int x = 1; int Local(int {|FEX0003:x|}) => x; }
                void LambdaLocalVsEarlierLocal() { int x = 1; Func<int> f = () => { int {|FEX0003:x|} = 2; return x; }; }
                void NestedLambdaParameters() { Func<int, Func<int, int>> g = y => {|FEX0003:y|} => y; }
                void LambdaParameterVsMethodParameter(int p) { Func<int, int> f = {|FEX0003:p|} => p; }
                void AnonymousMethodLocal() { int x = 1; Action a = delegate { int {|FEX0003:x|} = 2; }; }
                void ForeachVariableInLambda() { int x = 1; Action a = () => { foreach (var {|FEX0003:x|} in new[] { 1 }) { } }; }
                void SiblingDeclaratorOfTheSameStatement() { int a = 1, b = F(() => { var {|FEX0003:a|} = 2; return a; }); }
                void EarlierStatementOfAnInitializedVariable() { int x = 1; var y = F(() => { var {|FEX0003:x|} = 2; return x; }); }
                void ForVariable() { for (int i = 0; i < 1; i++) { Func<int> f = () => { int {|FEX0003:i|} = 0; return i; }; } }
                void PatternVariable(object o) { if (o is int n) { Func<int, int> f = {|FEX0003:n|} => n; } }
                void CatchVariable() { try { } catch (Exception ex) { Func<int, int> f = {|FEX0003:ex|} => ex; } }
                void ForeachVariable() { foreach (var item in new[] { 1 }) { Func<int, int> f = {|FEX0003:item|} => item; } }
                void EnclosingLambdaLocal() { Action a = () => { int x = 1; Func<int, int> f = {|FEX0003:x|} => x; }; }
                void LambdaInsideLambdaOfOuterLocal() { int x = 1; Action a = () => { Func<int, int> f = {|FEX0003:x|} => x; }; }
                void OutVariableOfAnEarlierStatement() { int.TryParse("1", out var n); Func<int, int> f = {|FEX0003:n|} => n; }
                void OutVariableOfTheSameInitializer() { var ok = int.TryParse("1", out var n) && F(() => { var {|FEX0003:n|} = 2; return n; }) > 0; }
                void ParameterVsLambdaLocal(int r) { var i = F(() => { int {|FEX0003:r|} = 1; return r; }); }
                void RefStructParameter(Span<int> s) { Func<int, int> g = {|FEX0003:s|} => s; }
                void DeconstructedNameOfAnEarlierStatement() { var (a, b) = F(() => (1, 2)); Func<int, int> f = {|FEX0003:a|} => a; }
            }
            """);

    [Fact]
    public Task Ignores_every_shape_ReSharper_ignores() =>
        VerifyAsync("""
            using System;
            class Probes
            {
                static T F<T>(Func<T> f) => f();
                static T G<T>(Func<int, T> f) => f(0);

                int fld;

                void OutParameter(out int r) { r = 0; var i = F(() => { int r = 1; return r; }); }
                void RefParameter(ref int r) { var i = F(() => { int r = 1; return r; }); }
                void InParameter(in int r) { var i = F(() => { int r = 1; return r; }); }
                void LocalFunctionParameterVsOutParameter(out int r) { r = 0; int Local(int r) => r; }
                void RefLocal(int p) { ref int rl = ref p; var i = F(() => { int rl = 1; return rl; }); }
                void RefReadonlyLocal(int p) { ref readonly int rl = ref p; Func<int, int> g = rl => rl; }
                void RefStructLocal() { Span<int> s = default; Func<int, int> g = s => s; }
                void LocalOfADeconstructionInItsOwnInitializer() { var (a, b) = F(() => { var b = 1; return (1, b); }); }
                void FirstNameOfADeconstructionInItsOwnInitializer() { var (a, b) = F(() => { var a = 1; return (a, 1); }); }
                void ParameterNamedLikeADeconstructedName() { var (a, b) = G(a => (1, 2)); }
                void LambdaDeeperInsideItsOwnDeconstruction() { var (a, b) = F(() => { Func<int, int> f = b => b; return (1, 2); }); }
                void LocalOfADeclaratorInItsOwnInitializer() { var x = F(() => { var x = 1; return x; }); }
                void LocalFunctionParameterInItsOwnInitializer() { var x = F(() => { int Inner(int x) => x; return Inner(1); }); }
                void NestedLambdaInItsOwnInitializer() { var x = F(() => F(() => { var x = 1; return x; })); }
                void LocalOfALaterDeclarator() { int a = F(() => { var b = 1; return b; }), b = 2; }
                void LocalDeclaredAfterTheLambda() { Func<int, int> f = x => x; int x = 1; }
                void LocalOfASiblingScope() { { int x = 1; } Func<int, int> f = x => x; }
                void StaticLambda() { int x = 1; Func<int, int> f = static x => x; }
                void Field() { Func<int, int> f = fld => fld; }
            }
            """);

    // The compiler's own `value` has no declaration to compare with; the analyzer used to crash on it (AD0001).
    [Theory]
    [InlineData("int P { set { Func<int, int> f = value => value; } }")]
    [InlineData("int P { init { Func<int, int> f = value => value; } }")]
    [InlineData("int this[int i] { set { Func<int, int> f = value => value; } }")]
    [InlineData("event Action E { add { Func<int, int> f = value => value; } remove { Func<int, int> g = value => value; } }")]
    public Task Ignores_the_implicit_value_parameter_of_an_accessor(string member) =>
        VerifyAsync($$"""
            using System;
            class C { {{member}} }
            """);

    // Top-level statements declare an implicit `args` that has no location.
    [Theory]
    [InlineData("Func<int, int> f = args => args;")]
    [InlineData("Func<int> f = () => { var args = 1; return args; };")]
    public Task Ignores_the_implicit_args_of_top_level_statements(string statement) =>
        AnalyzerTestHelper.VerifyAsync<VariableHidesOuterVariableAnalyzer>(
            $"""
            using System;
            {statement}
            """,
            OutputKind.ConsoleApplication);

    [Fact]
    public Task Reports_a_name_from_a_primary_constructor_parameter() =>
        VerifyAsync("""
            using System;
            class C(int x) { void M() { Func<int, int> f = {|FEX0003:x|} => x; } }
            """);

    // A partial type's primary constructor parameter lives in another file, where an offset says nothing about order.
    [Fact]
    public Task Reports_a_name_from_a_primary_constructor_parameter_declared_in_another_file() =>
        AnalyzerTestHelper.VerifyAsync<VariableHidesOuterVariableAnalyzer>(
            """
            using System;
            partial class P { void M() { Func<int, int> f = {|FEX0003:x|} => x; } }
            """,
            otherFiles: ["// " + new string('x', 500) + Environment.NewLine + "partial class P(int x) { }"]);

    // A nested type cannot capture the outer type's primary constructor parameter (CS9105).
    [Fact]
    public Task Ignores_a_name_from_the_primary_constructor_of_an_outer_type() =>
        VerifyAsync("""
            using System;
            class Outer(int x) { class Inner { void M() { Func<int, int> f = x => x; } } }
            """);

    [Fact]
    public Task Reports_a_name_from_an_outer_local_that_has_no_initializer() =>
        VerifyAsync("""
            using System;
            class C { void M() { int x; x = 1; Func<int, int> f = {|FEX0003:x|} => x; } }
            """);
}
