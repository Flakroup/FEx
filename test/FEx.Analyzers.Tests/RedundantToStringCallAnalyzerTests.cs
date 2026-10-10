using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantToStringCallAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantToStringCallAnalyzer>(source);

    [Fact]
    public Task Reports_a_string_ToString_in_a_concatenation() =>
        VerifyAsync("""
            class C { string M(string s) => "a" + {|FEX0022:s.ToString()|}; }
            """);

    [Fact]
    public Task Reports_an_object_ToString_next_to_a_string_on_either_side() =>
        VerifyAsync("""
            class C
            {
                string Right(object o) => "a" + {|FEX0022:o.ToString()|};
                string Left(object o) => {|FEX0022:o.ToString()|} + "a";
                string Parenthesized(object o) => "a" + ({|FEX0022:o.ToString()|});
                string Through(object o, string s) => s + {|FEX0022:o.ToString()|};
            }
            """);

    [Fact]
    public Task Reports_a_user_defined_ToString_in_a_concatenation() =>
        VerifyAsync("""
            class P { public override string ToString() => "p"; }
            class C { string M(P p) => "a" + {|FEX0022:p.ToString()|}; }
            """);

    [Fact]
    public Task Reports_this_ToString_in_a_concatenation() =>
        VerifyAsync("""
            class C { string M() => "a" + {|FEX0022:this.ToString()|}; }
            """);

    [Fact]
    public Task Reports_ToString_in_an_interpolation_hole() =>
        VerifyAsync("""
            class C
            {
                string Plain(object o) => $"{ {|FEX0022:o.ToString()|} }";
                string Formatted(object o) => $"{ {|FEX0022:o.ToString()|}:x}";
                string Aligned(object o) => $"{ {|FEX0022:o.ToString()|},5}";
            }
            """);

    [Fact]
    public Task Reports_ToString_passed_to_string_Format() =>
        VerifyAsync("""
            class C
            {
                string One(object o) => string.Format("{0}", {|FEX0022:o.ToString()|});
                string Two(object o, object p) => string.Format("{0} {1}", o, {|FEX0022:p.ToString()|});
                string WithNumber(object o) => string.Format("{0}{1}", {|FEX0022:o.ToString()|}, 1);
            }
            """);

    [Fact]
    public Task Reports_ToString_passed_to_StringBuilder_Append() =>
        VerifyAsync("""
            using System.Text;
            class C { void M(StringBuilder sb, object o) { sb.Append({|FEX0022:o.ToString()|}); } }
            """);

    [Fact]
    public Task Reports_a_null_conditional_ToString_in_a_concatenation() =>
        VerifyAsync("""
            class C { string M(object o) => "a" + o?{|FEX0022:.ToString()|}; }
            """);

    [Fact]
    public Task Reports_ToString_of_a_string_wherever_it_stands() =>
        VerifyAsync("""
            using System;
            class C
            {
                string Alone(string s) => {|FEX0022:s.ToString()|};
                int Length(string s) => {|FEX0022:s.ToString()|}.Length;
                void Argument(string s) { Console.Write({|FEX0022:s.ToString()|}); }
                string Compound(string s, string t) { s += {|FEX0022:t.ToString()|}; return s; }
            }
            """);

    [Fact]
    public Task Reports_ToString_of_a_type_parameter_constrained_to_a_reference_type() =>
        VerifyAsync("""
            using System;
            class C
            {
                string Class<T>(T t) where T : class => "a" + {|FEX0022:t.ToString()|};
                string Derived<T>(T t) where T : Exception => "a" + {|FEX0022:t.ToString()|};
            }
            """);

    [Fact]
    public Task Reports_ToString_of_interfaces_arrays_and_delegates() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            class C
            {
                string Interface(IEnumerable<int> e) => "a" + {|FEX0022:e.ToString()|};
                string Array(int[] a) => "a" + {|FEX0022:a.ToString()|};
                string Delegate(Action a) => "a" + {|FEX0022:a.ToString()|};
            }
            """);

    [Fact]
    public Task Ignores_ToString_where_nothing_converts_it_anyway() =>
        VerifyAsync("""
            using System;
            using System.Text;
            class C
            {
                string Alone(object o) => o.ToString();
                void Argument(object o) { Console.WriteLine(o.ToString()); }
                string Compound(string s, object o) { s += o.ToString(); return s; }
                void Line(StringBuilder sb, object o) { sb.AppendLine(o.ToString()); }
                string Concat(object o) => string.Concat(o.ToString(), "x");
                string Join(object o) => string.Join(",", o.ToString(), "b");
                string Implicit() => "a" + ToString();
            }
            """);

    [Fact]
    public Task Ignores_ToString_with_arguments() =>
        VerifyAsync("""
            using System;
            class C { string M(IFormattable f) => "a" + f.ToString("x", null); }
            """);

    [Fact]
    public Task Ignores_a_concatenation_whose_other_side_is_not_a_string() =>
        VerifyAsync("""
            class C
            {
                string Object(object o, object p) => o + p.ToString();
                string Int(object o, int i) => o.ToString() + i;
                string Char(object o) => o.ToString() + 'c';
            }
            """);

    [Fact]
    public Task Ignores_ToString_through_base() =>
        VerifyAsync("""
            class C { public override string ToString() => "a" + base.ToString(); }
            """);

    [Fact]
    public Task Ignores_string_Format_with_a_provider_or_a_non_string_format() =>
        VerifyAsync("""
            using System.Globalization;
            class C { string M(object o) => string.Format(CultureInfo.InvariantCulture, "{0}", o.ToString()); }
            """);

    [Fact]
    public Task Ignores_ToString_of_a_value_type_or_a_type_that_may_be_one() =>
        VerifyAsync("""
            using System;
            class C
            {
                string Int(int i) => "a" + i.ToString();
                string Hole(int i) => $"{i.ToString()}";
                string Unconstrained<T>(T t) => "a" + t!.ToString();
                string Interface<T>(T t) where T : IComparable => "a" + t.ToString();
                string Struct<T>(T t) where T : struct => "a" + t.ToString();
            }
            """);

    [Fact]
    public Task Ignores_other_parameterless_methods() =>
        VerifyAsync("""
            using System;
            class C
            {
                string Trim(string s) => "a" + s.Trim();
                string Type(object o) => "a" + o.GetType();
                string Hash(object o) => "a" + o.GetHashCode();
            }
            """);

    [Fact]
    public Task Ignores_a_comparison_because_the_operand_types_would_change() =>
        VerifyAsync("""
            class C
            {
                bool Left(object o) => o.ToString() == "a";
                bool Right(object o) => "a" != o.ToString();
            }
            """);

    [Fact]
    public Task Ignores_ToString_passed_to_methods_that_only_look_like_Format_and_Append() =>
        VerifyAsync("""
            using System;
            using System.Text;
            class Log { public static string Format(string format, string text) => format + text; }
            class Writer { public void Append(string text) { } }
            class C
            {
                string Own(object o) => Log.Format("x", o.ToString());
                void Mine(Writer w, object o) { w.Append(o.ToString()); }
                void Range(StringBuilder sb, object o) { sb.Append(o.ToString(), 0, 1); }
                string Pattern(Uri uri) => string.Format(uri.ToString(), 1);
            }
            """);
}
