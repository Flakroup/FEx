using System.Threading.Tasks;
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
}
