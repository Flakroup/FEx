using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
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
    public Task Keeps_an_await_prefix_on_a_parameter_of_an_async_function() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                Func<int, Task> F = async (int @await) => { await Task.Yield(); };
                Func<int, Task> G = async delegate (int @await) { await Task.Yield(); };
                Func<int, Task> H = async @await => await Task.Yield();
                void S() { async Task Local(int @await) { await Task.Yield(); } }
            }
            """);

    [Fact]
    public Task Keeps_an_await_prefix_on_the_signature_of_a_sync_function_inside_an_async_body() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task M()
                {
                    void Local(int @await) { }
                    void @await() { }
                    Func<int, int> f = @await => @await;
                    await Task.Yield();
                }
            }
            """);

    [Fact]
    public Task Reports_an_await_prefix_on_the_signature_of_a_local_function_in_a_sync_method() =>
        VerifyAsync("""
            using System.Threading.Tasks;
            class C
            {
                void M() { async Task {|FEX0006:@|}await() { await Task.Yield(); } }
                void N() { void Local(int {|FEX0006:@|}await) { } }
            }
            """);

    [Fact]
    public Task Keeps_an_await_prefix_in_an_async_expression_body() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                static int {|FEX0006:@|}await;
                async Task<int> A() => @await;
                Func<Task<int>> B = async () => @await;
                void S() { async Task<int> Local() => @await; }
            }
            """);

    [Fact]
    public Task Keeps_a_discard_named_prefix_on_a_variable_declaration() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            class C
            {
                void A(object o) { if (o is int @_ && {|FEX0006:@|}_ > 0) { } }
                void B(Dictionary<int, int> d) { d.TryGetValue(1, out var @_); Console.Write({|FEX0006:@|}_); }
                void D((int, int) t) { var (@_, y) = t; Console.Write(y); }
                void E(Dictionary<int, int> d) { d.TryGetValue(1, out int @_); }
            }
            """);

    [Fact]
    public Task Keeps_a_query_keyword_prefix_inside_a_query() =>
        VerifyAsync("""
            using System.Linq;
            class C
            {
                void Q(int[] xs)
                {
                    var {|FEX0006:@|}into = 1; var {|FEX0006:@|}join = 2; var {|FEX0006:@|}ascending = 3; var {|FEX0006:@|}descending = 4;
                    var {|FEX0006:@|}on = 5; var {|FEX0006:@|}equals = 6; var {|FEX0006:@|}orderby = 7; var {|FEX0006:@|}from = 8;
                    var {|FEX0006:@|}select = 9; var {|FEX0006:@|}where = 10; var {|FEX0006:@|}group = 11; var {|FEX0006:@|}by = 12;
                    var {|FEX0006:@|}let = 13;
                    var q1 = from x in xs join z in xs on x equals z + @into select x;
                    var q2 = from x in xs select xs.Where(w => w == @join);
                    var q3 = from x in xs orderby x + @ascending select x;
                    var q4 = from x in xs orderby @descending select x;
                    var q5 = from x in xs group x by @on;
                    var q6 = from x in xs where @equals > 0 select x;
                    var q7 = from x in xs select @orderby;
                    var q8 = from x in xs let a = @from select a;
                    var q9 = from x in xs select @select;
                    var q10 = from x in xs where x > @where select x;
                    var q11 = from x in xs group x by @group;
                    var q12 = from x in xs orderby @by select x;
                    var q13 = from x in xs select @let;
                    var o1 = {|FEX0006:@|}into + {|FEX0006:@|}select;
                }
            }
            """);

    [Fact]
    public Task Keeps_a_when_designation_in_a_case_pattern_and_reports_the_rest() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(object o) { switch (o) { case int @when when {|FEX0006:@|}when > 0: break; } }
                void B(object o) { switch (o) { case int @when: break; } }
                int D(object o) => o switch { int @when when {|FEX0006:@|}when > 0 => 1, _ => 0 };
                void E(object o) { if (o is int {|FEX0006:@|}when) Console.Write({|FEX0006:@|}when); }
                void F(object o) { switch (o) { case int {|FEX0006:@|}x when {|FEX0006:@|}x > 0: break; } }
                int G(object o) => o switch { _ => o is int {|FEX0006:@|}when ? 1 : 0 };
                void H(object o) { switch (o) { case int n when o is int {|FEX0006:@|}when: break; } }
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
