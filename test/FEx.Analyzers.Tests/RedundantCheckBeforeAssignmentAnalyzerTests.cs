using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantCheckBeforeAssignmentAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantCheckBeforeAssignmentAnalyzer>(source);

    [Fact]
    public Task Reports_the_check_for_a_field_in_its_various_forms() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                private static int _s;
                private volatile int _v;
                public event System.Action? E;
                void Plain() { {|FEX0034:if (_f != 1) _f = 1;|} }
                void Braces() { {|FEX0034:if (_f != 1) { _f = 1; }|} }
                void Comment() { {|FEX0034:if (_f != 1) { /* c */ _f = 1; }|} }
                void This() { {|FEX0034:if (this._f != 1) this._f = 1;|} }
                void Static() { {|FEX0034:if (_s != 1) _s = 1;|} }
                void Volatile() { {|FEX0034:if (_v != 1) _v = 1;|} }
                void Other(C other) { {|FEX0034:if (other._f != 1) other._f = 1;|} }
                void Event() { {|FEX0034:if (E != null) E = null;|} }
            }
            """);

    [Fact]
    public Task Reports_the_check_for_a_local_a_parameter_and_a_reference() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                void Local() { var x = 0; {|FEX0034:if (x != 1) x = 1;|} }
                void Out(out int x) { x = 0; {|FEX0034:if (x != 1) x = 1;|} }
                void Ref() { ref int r = ref _f; {|FEX0034:if (r != 1) r = 1;|} }
            }
            """);

    [Fact]
    public Task Reports_the_check_for_other_types() =>
        VerifyAsync("""
            enum Mode { A, B }
            struct S { public int X; }
            class C
            {
                private string? _s;
                private double _d;
                private Mode _m;
                private object? _o;
                private long _l;
                void Text() { {|FEX0034:if (_s != "a") _s = "a";|} }
                void Null() { {|FEX0034:if (_s != null) _s = null;|} }
                void Double() { {|FEX0034:if (_d != 1.5) _d = 1.5;|} }
                void Enum() { {|FEX0034:if (_m != Mode.B) _m = Mode.B;|} }
                void Object(object? o) { {|FEX0034:if (_o != o) _o = o;|} }
                void Widening(int x) { {|FEX0034:if (_l != x) _l = x;|} }
                void StructField() { var s = new S(); {|FEX0034:if (s.X != 1) s.X = 1;|} }
            }
            """);

    [Fact]
    public Task Reports_the_check_for_an_indexer() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                void Array(int[] a) { {|FEX0034:if (a[0] != 1) a[0] = 1;|} }
                void List(List<int> l) { {|FEX0034:if (l[0] != 1) l[0] = 1;|} }
            }
            """);

    [Fact]
    public Task Reports_the_check_with_the_operands_in_either_order() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                int Value() => 1;
                void Reversed() { {|FEX0034:if (1 != _f) _f = 1;|} }
                void Call() { {|FEX0034:if (_f != Value()) _f = Value();|} }
                void Variable(int x) { {|FEX0034:if (_f != x) _f = x;|} }
                void Reversed2(int x) { {|FEX0034:if (x != _f) _f = x;|} }
            }
            """);

    [Fact]
    public Task Reports_a_check_whose_value_is_a_property() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                public int P { get; set; }
                void M() { {|FEX0034:if (_f != P) _f = P;|} }
            }
            """);

    [Fact]
    public Task Ignores_the_check_for_a_property_because_a_setter_may_do_more() =>
        VerifyAsync("""
            class C
            {
                public int P { get; set; }
                public static int S { get; set; }
                void Plain() { if (P != 1) P = 1; }
                void This() { if (this.P != 1) this.P = 1; }
                void Static() { if (S != 1) S = 1; }
                void Other(C other) { if (other.P != 1) other.P = 1; }
            }
            """);

    [Fact]
    public Task Ignores_a_body_that_does_more_than_the_assignment() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                private int _g;
                void Two() { if (_f != 1) { _f = 1; _g = 2; } }
                void Call() { if (_f != 1) { _f = 1; M(); } }
                void M() { }
                void Empty() { if (_f != 1) { } }
                void Other() { if (_f != 1) M(); }
            }
            """);

    [Fact]
    public Task Ignores_an_assignment_of_another_value_or_to_another_variable() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                private int _g;
                void Value() { if (_f != 1) _f = 2; }
                void Variable() { if (_f != 1) _g = 1; }
                void Other() { if (_f != _g) _g = 1; }
                void Crossed(int x) { if (x != _g) _f = x; }
            }
            """);

    [Fact]
    public Task Ignores_an_if_that_has_an_else() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                void Else() { if (_f != 1) _f = 1; else _f = 2; }
                void ElseIf(int x) { if (_f != x) _f = x; else if (x > 1) _f = 2; }
            }
            """);

    [Fact]
    public Task Ignores_a_condition_that_is_not_a_plain_inequality() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                void Equal() { if (_f == 1) _f = 1; }
                void Greater() { if (_f > 1) _f = 1; }
                void And(bool b) { if (_f != 1 && b) _f = 1; }
                void Not() { if (!(_f == 1)) _f = 1; }
                void Equals_() { if (!_f.Equals(1)) _f = 1; }
                void Flag(bool b) { if (b) _f = 1; }
            }
            """);

    [Fact]
    public Task Ignores_a_compound_assignment() =>
        VerifyAsync("""
            class C
            {
                private int _f;
                void Add() { if (_f != 1) _f += 1; }
                void Or() { if (_f != 1) _f |= 1; }
            }
            """);
}
