using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantVerbatimPrefixAnalyzerTests
{
    private static Task VerifyAsync(string source, params DiagnosticResult[] expected) =>
        AnalyzerTestHelper.VerifyAsync<RedundantVerbatimPrefixAnalyzer>(source, expected: expected);

    [Fact]
    public Task Reports_a_prefix_on_a_local_and_on_its_use() =>
        VerifyAsync("""
            class C { int M() { int {|FEX0006:@|}value = 1; return {|FEX0006:@|}value; } }
            """);

    [Fact]
    public Task Reports_a_prefix_on_contextual_keywords() =>
        VerifyAsync("""
            class C
            {
                void M(int {|FEX0006:@|}async, int {|FEX0006:@|}partial, int {|FEX0006:@|}dynamic, int {|FEX0006:@|}when, int {|FEX0006:@|}from,
                       int {|FEX0006:@|}nameof, int {|FEX0006:@|}yield, int {|FEX0006:@|}var, int {|FEX0006:@|}global, int {|FEX0006:@|}select,
                       int {|FEX0006:@|}where, int {|FEX0006:@|}let, int {|FEX0006:@|}orderby, int {|FEX0006:@|}ascending, int {|FEX0006:@|}required,
                       int {|FEX0006:@|}args, int {|FEX0006:@|}value, int {|FEX0006:@|}alias, int {|FEX0006:@|}record) { }
            }
            """);

    [Fact]
    public Task Reports_a_prefix_on_ordinary_declarations() =>
        VerifyAsync("""
            namespace {|FEX0006:@|}NsX { }
            enum E { {|FEX0006:@|}A }
            class {|FEX0006:@|}var { }
            class G<{|FEX0006:@|}T>
            {
                int {|FEX0006:@|}_x;
                int {|FEX0006:@|}é;
                void M()
                {
                    {|FEX0006:@|}Label: _x = 1;
                    (int, int) t = ({|FEX0006:@|}a: 1, 2);
                    object o = new { {|FEX0006:@|}P = 1 };
                    {|FEX0006:@|}var v = null;
                    global::System.{|FEX0006:@|}String s = null;
                    System.{|FEX0006:@|}Int32 i = 0;
                }
            }
            """);

    [Fact]
    public Task Keeps_a_prefix_that_makes_a_reserved_keyword_a_name() =>
        VerifyAsync("""
            class C
            {
                int M(int @class, int @this)
                {
                    int @int = 1;
                    int @namespace = @int + @class;
                    return @namespace;
                }
            }
            """);

    [Fact]
    public Task Keeps_a_prefix_on_a_type_named_like_a_type_declaring_contextual_keyword() =>
        VerifyAsync("""
            class @record { }
            interface @scoped { }
            struct @file { }
            enum @extension { }
            class {|FEX0006:@|}required { }
            class {|FEX0006:@|}args { }
            class {|FEX0006:@|}async { }
            class {|FEX0006:@|}record2 { }
            """);

    [Fact]
    public Task Keeps_a_prefix_on_a_delegate_named_like_a_type_declaring_contextual_keyword() =>
        VerifyAsync("""
            delegate void @file();
            delegate void @record();
            delegate void {|FEX0006:@|}required();
            """);

    [Fact]
    public Task Keeps_a_field_prefix_inside_property_accessors_and_expression_bodies() =>
        VerifyAsync("""
            using System;
            class C
            {
                int P { get { int @field = 1; return @field; } set { int @field = value; } }
                int Q => Helper(@field => 1);
                Func<int> R => () => { int @field = 1; return @field; };
                static int Helper(Func<int, int> f) => f(0);
            }
            """);

    [Fact]
    public Task Reports_a_field_prefix_outside_property_accessors() =>
        VerifyAsync("""
            class C
            {
                static int field;
                static int P { get; } = {|FEX0006:@|}field;
                int this[int i] { get { int {|FEX0006:@|}field = i; return {|FEX0006:@|}field; } }
                event System.Action E { add { int {|FEX0006:@|}field = 1; } remove { } }
                C() { int {|FEX0006:@|}field = 1; }
                void M() { int {|FEX0006:@|}field = 1; }
            }
            """);

    [Fact]
    public Task Keeps_an_await_prefix_inside_async_bodies() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task M() { int @await = 1; @await++; await Task.Yield(); }
                async Task N() { Action a = () => { int @await = 2; }; await Task.Yield(); }
                async Task O() { async Task Local() { int @await = 3; await Task.Yield(); } await Local(); }
                void S() { Func<Task> f = async () => { int @await = 1; await Task.Yield(); }; }
            }
            """);

    [Fact]
    public Task Keeps_an_await_prefix_on_a_member_access_inside_an_async_body() =>
        VerifyAsync("""
            using System.Threading.Tasks;
            class C
            {
                int {|FEX0006:@|}await;
                async Task M() { this.@await = 1; await Task.Yield(); }
            }
            """);

    [Fact]
    public Task Reports_an_await_prefix_outside_async_bodies() =>
        VerifyAsync("""
            using System.Threading.Tasks;
            class C
            {
                void S() { int {|FEX0006:@|}await = 1; }
                async Task P(int {|FEX0006:@|}await) { await Task.Yield(); }
                async Task M()
                {
                    void Sync() { int {|FEX0006:@|}await = 1; }
                    Sync();
                    await Task.Yield();
                }
            }
            """);

    [Fact]
    public Task Reports_an_attribute_prefix_only_when_no_attribute_suffixed_type_exists() =>
        VerifyAsync("""
            using System;
            class Foo1 : Attribute { }
            class Foo3 : Attribute { }
            class Foo3Attribute : Attribute { }
            [{|FEX0006:@|}Foo1] class A { }
            [@Foo3] class B { }
            [Q.{|FEX0006:@|}Foo4] class D { }
            namespace Q { class Foo4 : Attribute { } }
            """);

    [Fact]
    public Task Reports_a_prefix_on_a_name_that_merely_matches_an_attribute_type() =>
        VerifyAsync("""
            using System;
            class FooAttribute : Attribute { }
            class C
            {
                int Foo;
                void M() { {|FEX0006:@|}Foo = 1; }
            }
            """);

    [Fact]
    public Task Keeps_an_alias_qualified_attribute_prefix() =>
        VerifyAsync("""
            using System;
            class Foo : Attribute { }
            class FooAttribute : Attribute { }
            [global::@Foo] class A { }
            """);

    [Fact]
    public Task Reports_an_await_prefix_on_an_async_lambda_parameter() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                Func<int, Task> F = async (int {|FEX0006:@|}await) => { await Task.Yield(); };
            }
            """);

    [Fact]
    public Task Reports_an_attribute_prefix_on_a_name_that_already_carries_the_suffix() =>
        VerifyAsync("""
            using System;
            class Foo2Attribute : Attribute { }
            [{|FEX0006:@|}Foo2Attribute] class A { }
            [System.{|FEX0006:@|}ObsoleteAttribute] class B { }
            """);

    [Fact]
    public Task Keeps_an_attribute_prefix_that_selects_the_exact_type_over_the_suffixed_one() =>
        VerifyAsync(
            """
            using System;
            class Foo2Attribute : Attribute { }
            [@Foo2] class A { }
            [System.@Obsolete] class B { }
            """,
            DiagnosticResult.CompilerError("CS0246").WithSpan(3, 2, 3, 7).WithArguments("Foo2"),
            DiagnosticResult.CompilerError("CS0234").WithSpan(4, 9, 4, 18).WithArguments("Obsolete", "System"));

    [Fact]
    public Task Ignores_unprefixed_identifiers() =>
        VerifyAsync("""
            class C { int M(int value) { int field = value; return field; } }
            """);

    [Fact]
    public Task Keeps_an_await_prefix_in_top_level_statements() =>
        AnalyzerTestHelper.VerifyAsync<RedundantVerbatimPrefixAnalyzer>("""
            using System.Threading.Tasks;
            int @await = 1;
            await Task.Yield();
            """, OutputKind.ConsoleApplication);

}
