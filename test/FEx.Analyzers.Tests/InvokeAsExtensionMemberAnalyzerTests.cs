using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class InvokeAsExtensionMemberAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<InvokeAsExtensionMemberAnalyzer>(source);

    [Fact]
    public Task Reports_a_linq_call_made_through_Enumerable() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            public class C
            {
                public int First(List<int> l) => Enumerable.{|FEX0056:First|}(l);
                public int Count(List<int> l) => Enumerable.{|FEX0056:Count|}(l);
                public bool Any(List<int> l) => Enumerable.{|FEX0056:Any|}(l, x => x > 1);
            }
            """);

    [Fact]
    public Task Reports_an_extension_method_of_the_user() =>
        VerifyAsync("""
            public static class Ext
            {
                public static int Twice(this int x) => x * 2;
                public static int Add(this int x, int y) => x + y;
            }
            public class C
            {
                public int Twice(int i) => Ext.{|FEX0056:Twice|}(i);
                public int Add(int i) => Ext.{|FEX0056:Add|}(i, 2);
                public int Literal() => Ext.{|FEX0056:Twice|}(5);
            }
            """);

    [Fact]
    public Task Reports_both_calls_of_a_nested_pair() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            public class C
            {
                public int M(List<int> l) => Enumerable.{|FEX0056:Count|}(Enumerable.{|FEX0056:Where|}(l, x => x > 0));
            }
            """);

    [Fact]
    public Task Reports_a_call_with_explicit_type_arguments_and_a_named_receiver() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            public class C
            {
                public IEnumerable<int> Select(List<int> l) => Enumerable.{|FEX0056:Select<int, int>|}(l, x => x);
                public int Named(List<int> l) => Enumerable.{|FEX0056:Count|}(source: l);
            }
            """);

    [Fact]
    public Task Reports_a_call_through_a_qualified_name_and_an_alias() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            using EnumerableAlias = System.Linq.Enumerable;
            public class C
            {
                public int Qualified(List<int> l) => System.Linq.Enumerable.{|FEX0056:Count|}(l);
                public int Alias(List<int> l) => EnumerableAlias.{|FEX0056:First|}(l);
            }
            """);

    [Fact]
    public Task Reports_a_receiver_that_needs_parentheses_and_one_that_does_not() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            public class C
            {
                public int Binary(List<int>? a, List<int> b) => Enumerable.{|FEX0056:Count|}(a ?? b);
                public int Ternary(bool c, List<int> a, List<int> b) => Enumerable.{|FEX0056:Count|}(c ? a : b);
                public int Cast(object o) => Enumerable.{|FEX0056:Count|}((List<int>)o);
                public async Task<int> Await(Task<List<int>> t) => Enumerable.{|FEX0056:Count|}(await t);
                public int Member(C other) => Enumerable.{|FEX0056:Count|}(other.Items);
                public int Array(int[] a) => Enumerable.{|FEX0056:Count|}(a);
                public List<int> Items = new();
            }
            """);

    [Fact]
    public Task Reports_a_string_as_the_receiver() =>
        VerifyAsync("""
            public static class Ext
            {
                public static int Length(this string s) => s.Length;
                public static int Anything(this object o) => 0;
            }
            public class C
            {
                public int Variable(string s) => Ext.{|FEX0056:Length|}(s);
                public int Literal() => Ext.{|FEX0056:Length|}("abc");
                public int Concatenation(string s) => Ext.{|FEX0056:Length|}(s + "a");
                public int Widened() => Ext.{|FEX0056:Anything|}("abc");
            }
            """);

    [Fact]
    public Task Reports_a_ref_receiver_and_a_value_one_boxed() =>
        VerifyAsync("""
            public struct Counter { public int Value; }
            public static class Ext
            {
                public static void Inc(this ref Counter c) { c.Value++; }
                public static int Boxed(this object o) => 0;
            }
            public class C
            {
                public void Ref(Counter c) { Ext.{|FEX0056:Inc|}(ref c); }
                public int Box(int i) => Ext.{|FEX0056:Boxed|}(i);
            }
            """);

    [Fact]
    public Task Reports_a_call_with_optional_and_params_arguments() =>
        VerifyAsync("""
            public static class Ext
            {
                public static int Optional(this int x, int y = 3) => x + y;
                public static int Many(this int x, params int[] rest) => x + rest.Length;
            }
            public class C
            {
                public int Optional(int i) => Ext.{|FEX0056:Optional|}(i);
                public int Many(int i) => Ext.{|FEX0056:Many|}(i, 1, 2);
            }
            """);

    [Fact]
    public Task Reports_a_call_inside_a_lambda_and_an_expression_tree() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;
            public class C
            {
                public Expression<Func<List<int>, int>> Tree() => l => Enumerable.{|FEX0056:Count|}(l);
                public Func<List<int>, int> Lambda() => l => Enumerable.{|FEX0056:Count|}(l);
            }
            """);

    [Fact]
    public Task Ignores_a_call_already_in_instance_form() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            public class C
            {
                public int M(List<int> l) => l.Count() + l.First();
            }
            """);

    [Fact]
    public Task Ignores_a_static_method_that_is_no_extension() =>
        VerifyAsync("""
            public static class Util
            {
                public static int Twice(int x) => x * 2;
            }
            public class C
            {
                public int M(int i) => Util.Twice(i);
            }
            """);

    [Fact]
    public Task Ignores_a_call_an_instance_member_would_take() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            public class Holder { public int Foo(int s) => 0; }
            public static class Ext { public static int Foo(this Holder h, int i) => 1; }
            public class C
            {
                public bool Contains(List<int> l) => Enumerable.Contains(l, 1);
                public int Foo(Holder h) => Ext.Foo(h, 1);
            }
            """);

    [Fact]
    public Task Ignores_a_receiver_the_instance_form_cannot_take() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public struct Wrapper { public static implicit operator List<int>(Wrapper w) => new(); }
            public static class Ext { public static int Size(this List<int> l) => l.Count; }
            public class C
            {
                public int Null() => Ext.Size(null!);
                public int Conversion(Wrapper w) => Ext.Size(w);
            }
            """);

    [Fact]
    public Task Ignores_an_interpolated_string_as_the_receiver() =>
        VerifyAsync("""
            public static class Ext { public static int Length(this string s) => s.Length; }
            public class C
            {
                public int M(int i) => Ext.Length($"a{i}");
            }
            """);

    [Fact]
    public Task Ignores_a_dynamic_receiver() =>
        VerifyAsync("""
            using System.Linq;
            public class C
            {
                public int M(dynamic d) => Enumerable.Count(d);
            }
            """);

    [Fact]
    public Task Ignores_a_call_inside_the_class_that_declares_the_extension() =>
        VerifyAsync("""
            public static class Ext
            {
                public static int Twice(this int x) => x * 2;
                public static int M(int i) => Ext.Twice(i);
                public static class Nested { public static int N(int i) => Ext.Twice(i); }
            }
            """);

    [Fact]
    public Task Ignores_a_call_when_the_extension_class_is_not_imported() =>
        VerifyAsync("""
            public class C
            {
                public int M(string s) => System.Linq.Enumerable.Count(s);
            }
            """);

    [Fact]
    public Task Reports_named_arguments_written_in_order_and_a_generic_extension() =>
        VerifyAsync("""
            public static class Ext
            {
                public static int Sub(this int a, int b) => a - b;
                public static T Same<T>(this T value) => value;
            }
            public class C
            {
                public int InOrder(int i) => Ext.{|FEX0056:Sub|}(a: i, b: 1);
                public int Mixed(int i) => Ext.{|FEX0056:Sub|}(i, b: 1);
                public int Generic(string s) => Ext.{|FEX0056:Same|}(s).Length;
            }
            """);

    [Fact]
    public Task Ignores_named_arguments_written_out_of_order() =>
        VerifyAsync("""
            public static class Ext { public static int Sub(this int a, int b) => a - b; }
            public class C
            {
                private static int F() => 1;
                private static int G() => 2;
                public int Plain(int i) => Ext.Sub(b: 1, a: i);
                public int Calls() => Ext.Sub(b: F(), a: G());
                public int OneCall(int i) => Ext.Sub(b: F(), a: i);
            }
            """);

    [Fact]
    public Task Reports_a_receiver_whose_last_operand_the_extension_does_not_take() =>
        VerifyAsync("""
            public static class Ext { public static int Shout(this string s) => s.Length; }
            public class C
            {
                public int M(int i) => Ext.{|FEX0056:Shout|}("a" + i);
            }
            """);

    [Fact]
    public Task Ignores_a_call_another_extension_of_a_nearer_namespace_would_take() =>
        VerifyAsync("""
            public static class Outer { public static int Twice(this int x) => x * 2; }
            namespace Near
            {
                public static class Closer { public static int Twice(this int x) => x * 3; }
                public class C { public int M(int i) => Outer.Twice(i); }
            }
            """);

    [Fact]
    public Task Ignores_a_parenthesized_receiver_argument() =>
        VerifyAsync("""
            public static class Ext { public static int Twice(this int x) => x * 2; public static int Shout(this string s) => s.Length; }
            public class C
            {
                public int M(int i) => Ext.Twice((i));
                public int N(int i) => Ext.Shout((($"a{i}")));
            }
            """);

    [Fact]
    public Task Ignores_later_arguments_written_out_of_order_behind_the_receiver() =>
        VerifyAsync("""
            public static class Ext { public static int Mix(this int a, int b, int c) => a + b + c; }
            public class C
            {
                private static int F() => 1;
                private static int G() => 2;
                public int M(int i) => Ext.Mix(i, c: F(), b: G());
            }
            """);
}
