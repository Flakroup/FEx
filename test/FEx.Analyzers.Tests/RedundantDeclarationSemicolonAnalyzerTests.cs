using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantDeclarationSemicolonAnalyzerTests
{
    private static Task VerifyAsync(string source, params DiagnosticResult[] expected) =>
        AnalyzerTestHelper.VerifyAsync<RedundantDeclarationSemicolonAnalyzer>(source, expected: expected);

    [Fact]
    public Task Reports_a_semicolon_after_a_type_with_members() =>
        VerifyAsync("""
            class C { int x; }{|FEX0010:;|}
            struct S { int x; }{|FEX0010:;|}
            interface I { void M(); }{|FEX0010:;|}
            record R { int x; }{|FEX0010:;|}
            record struct RS { int x; }{|FEX0010:;|}
            enum E { A }{|FEX0010:;|}
            enum E2 { A, B }{|FEX0010:;|}
            record P(int X) { int y; }{|FEX0010:;|}
            class Outer { class Inner { int x; }{|FEX0010:;|} }
            """);

    [Fact]
    public Task Reports_a_semicolon_after_a_type_body_holding_more_than_whitespace() =>
        VerifyAsync("""
            class A { /* c */ }{|FEX0010:;|}
            class B
            {
                // c
            }{|FEX0010:;|}
            class C
            {
            #if NEVER
                int x;
            #endif
            }{|FEX0010:;|}
            """);

    [Fact]
    public Task Reports_the_semicolon_wherever_it_sits_after_the_brace() =>
        VerifyAsync("""
            class A { int x; }
            {|FEX0010:;|}
            class B { int x; }   {|FEX0010:;|}
            """);

    [Fact]
    public Task Reports_only_the_first_of_two_semicolons() =>
        VerifyAsync(
            """
            class A { int x; }{|FEX0010:;|};
            """,
            DiagnosticResult.CompilerError("CS8803").WithSpan(1, 20, 1, 21),
            DiagnosticResult.CompilerError("CS8805").WithSpan(1, 20, 1, 21),
            DiagnosticResult.CompilerError("CS8937").WithSpan(1, 20, 1, 21));

    [Fact]
    public Task Reports_a_semicolon_after_a_block_scoped_namespace_even_when_empty() =>
        VerifyAsync("""
            namespace E1 { }{|FEX0010:;|}
            namespace N1 { class X { } }{|FEX0010:;|}
            namespace N2 { namespace N3 { class Y { } }{|FEX0010:;|} }
            """);

    [Fact]
    public Task Ignores_a_semicolon_after_an_empty_type_body() =>
        VerifyAsync("""
            class A { };
            class B
            {
            };
            struct S { };
            enum E { };
            interface I<T> { };
            class P(int x) { };
            record R(int X) { };
            record struct RS(int X) { };
            """);

    [Fact]
    public Task Ignores_declarations_that_end_without_a_body() =>
        VerifyAsync("""
            record R;
            delegate void D();
            class C { int x; void M() { } }
            """);

    [Fact]
    public Task Ignores_a_file_scoped_namespace() =>
        VerifyAsync("""
            namespace N;
            class C { int x; }
            """);

    [Fact]
    public Task Ignores_types_and_namespaces_without_a_semicolon() =>
        VerifyAsync("""
            namespace N { class C { int x; } enum E { A } }
            """);
}
