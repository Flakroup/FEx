using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantLambdaSignatureParenthesesAnalyzerTests
{
    private static Task VerifyAsync(string source, LanguageVersion? languageVersion = null, params DiagnosticResult[] expected) =>
        AnalyzerTestHelper.VerifyAsync<RedundantLambdaSignatureParenthesesAnalyzer>(source, languageVersion: languageVersion, expected: expected);

    [Fact]
    public Task Reports_a_single_untyped_parameter_in_parentheses() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Func<int, int> a = {|FEX0008:(|}x) => x;
                    Func<int, int> b = {|FEX0008:(|}_) => 0;
                    Func<int, int> c = {|FEX0008:(|}x) => { return x; };
                    Func<int, int> d = {|FEX0008:(|} /* c */ x ) => x;
                }
            }
            """);

    [Fact]
    public Task Reports_with_modifiers_in_front_of_the_parentheses() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                void M()
                {
                    Func<int, int> a = static {|FEX0008:(|}x) => x;
                    Func<int, Task> b = async {|FEX0008:(|}x) => { await Task.Yield(); };
                    Func<int, Task> c = async static {|FEX0008:(|}x) => { await Task.Yield(); };
                }
            }
            """);

    [Fact]
    public Task Reports_nested_lambdas_and_expression_trees() =>
        VerifyAsync("""
            using System;
            using System.Linq.Expressions;
            class C
            {
                void M()
                {
                    Func<int, Func<int, int>> n = {|FEX0008:(|}x) => {|FEX0008:(|}y) => x + y;
                    Expression<Func<int, int>> e = {|FEX0008:(|}x) => x;
                }
            }
            """);

    [Fact]
    public Task Keeps_parentheses_around_a_parameter_with_an_attribute() =>
        VerifyAsync("""
            using System;
            class MyAttribute : Attribute { }
            class C
            {
                void M() { Func<int, int> a = ([My] x) => x; }
            }
            """);

    [Fact]
    public Task Keeps_parentheses_on_a_lambda_with_an_attribute() =>
        VerifyAsync("""
            using System;
            class MyAttribute : Attribute { }
            class C
            {
                void M()
                {
                    Func<int, int> a = [My] (x) => x;
                    Func<int, int> b = [My] static (x) => x;
                }
            }
            """);

    [Fact]
    public Task Keeps_parentheses_on_a_lambda_with_a_return_type() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M() { Func<int, int> a = int (x) => x; }
            }
            """);

    [Fact]
    public Task Keeps_parentheses_around_a_typed_parameter() =>
        VerifyAsync("""
            using System;
            class C
            {
                delegate void R(ref int a);
                void M()
                {
                    Func<int, int> a = (int x) => x;
                    R r = (ref int x) => { };
                }
            }
            """);

    [Fact]
    public Task Keeps_parentheses_around_a_parameter_with_a_modifier() =>
        VerifyAsync(
            """
            class C
            {
                delegate void R(ref int a);
                delegate void O(out int a);
                void M()
                {
                    R r = (ref x) => { };
                    O o = (out x) => { x = 1; };
                }
            }
            """,
            LanguageVersion.Preview);

    [Fact]
    public Task Keeps_parentheses_around_a_parameter_with_a_default_value() =>
        VerifyAsync(
            """
            using System;
            class C
            {
                void M() { Action<int> a = (x = 1) => { }; }
            }
            """,
            LanguageVersion.Preview,
            DiagnosticResult.CompilerError("CS9098").WithSpan(4, 33, 4, 34).WithArguments("x"));

    [Fact]
    public Task Ignores_other_parameter_counts_and_the_bare_form() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Func<int, int, int> two = (x, y) => x + y;
                    Func<int> none = () => 1;
                    Func<int, int> bare = x => x;
                }
            }
            """);
}
