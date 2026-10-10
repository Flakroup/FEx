using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantAttributeParenthesesAnalyzerTests
{
    private static Task VerifyAsync(string source, params DiagnosticResult[] expected) =>
        AnalyzerTestHelper.VerifyAsync<RedundantAttributeParenthesesAnalyzer>(source, expected: expected);

    [Fact]
    public Task Reports_empty_parentheses_on_declarations() =>
        VerifyAsync("""
            using System;
            [assembly: A{|FEX0009:()|}]
            [module: B{|FEX0009:()|}]
            class A : Attribute { }
            class B : Attribute { }
            class G<T> : Attribute { }
            namespace N { class Q : Attribute { } }
            [A{|FEX0009:()|}] class C1 { }
            [A{|FEX0009:( )|}] class C2 { }
            [A{|FEX0009:(/* c */)|}] class C3 { }
            [A{|FEX0009:()|}, B{|FEX0009:()|}] class C4 { }
            [A{|FEX0009:()|}][B{|FEX0009:()|}] class C5 { }
            [G<int>{|FEX0009:()|}] class C6 { }
            [N.Q{|FEX0009:()|}] class C7 { }
            [A
            {|FEX0009:()|}] class C8 { }
            [A{|FEX0009:()|}] struct S { }
            [A{|FEX0009:()|}] interface I { }
            [A{|FEX0009:()|}] record R;
            [A{|FEX0009:()|}] delegate void D();
            [A{|FEX0009:()|}] enum E { [A{|FEX0009:()|}] M }
            class C9<[A{|FEX0009:()|}] T>
            {
                [field: A{|FEX0009:()|}] int P { get; set; }
                [A{|FEX0009:()|}] event Action Ev;
                [return: A{|FEX0009:()|}] int M([A{|FEX0009:()|}] int p) => p;
                int Q { [A{|FEX0009:()|}] get; }
                void L()
                {
                    [A{|FEX0009:()|}] void Local() { }
                    Action a = [A{|FEX0009:()|}] () => { };
                    Local();
                }
            }
            """);

    [Fact]
    public Task Ignores_attributes_with_arguments_or_without_parentheses() =>
        VerifyAsync("""
            using System;
            class N : Attribute { public string? Name { get; set; } }
            [Obsolete("x")] class C1 { }
            [N(Name = "x")] class C2 { }
            [Serializable] class C3 { }
            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)] class C4 : Attribute { }
            """);

    [Fact]
    public Task Ignores_an_attribute_type_that_does_not_resolve() =>
        VerifyAsync(
            """
            [Unknown()] class C { }
            """,
            DiagnosticResult.CompilerError("CS0246").WithSpan(1, 2, 1, 9).WithArguments("Unknown"),
            DiagnosticResult.CompilerError("CS0246").WithSpan(1, 2, 1, 9).WithArguments("UnknownAttribute"));
}
