using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantToStringCallForValueTypeAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantToStringCallForValueTypeAnalyzer>(source);

    [Fact]
    public Task Reports_a_value_type_ToString_next_to_a_string_on_either_side() =>
        VerifyAsync("""
            class C
            {
                string Right(int i) => "a" + {|FEX0023:i.ToString()|};
                string Left(int i) => {|FEX0023:i.ToString()|} + "a";
                string Parenthesized(int i) => "a" + ({|FEX0023:i.ToString()|});
                string Three(int i, int j) => "a" + {|FEX0023:i.ToString()|} + {|FEX0023:j.ToString()|};
                string Literal() => "a" + {|FEX0023:5.ToString()|};
            }
            """);

    [Fact]
    public Task Reports_enums_chars_floating_point_and_structs() =>
        VerifyAsync("""
            using System;
            enum Color { Red }
            struct Pair { public override string ToString() => "p"; }
            struct Box<T> { public override string ToString() => "b"; }
            class C
            {
                string Enum(Color c) => "a" + {|FEX0023:c.ToString()|};
                string Char(char c) => "a" + {|FEX0023:c.ToString()|};
                string Double(double d) => "a" + {|FEX0023:d.ToString()|};
                string Bool(bool b) => "a" + {|FEX0023:b.ToString()|};
                string Date(DateTime d) => "a" + {|FEX0023:d.ToString()|};
                string Guid(Guid g) => "a" + {|FEX0023:g.ToString()|};
                string Custom(Pair p) => "a" + {|FEX0023:p.ToString()|};
                string Generic(Box<int> b) => "a" + {|FEX0023:b.ToString()|};
            }
            """);

    [Fact]
    public Task Reports_a_nullable_value_type_including_a_null_conditional_call() =>
        VerifyAsync("""
            class C
            {
                string Plain(int? n) => "a" + {|FEX0023:n.ToString()|};
                string Conditional(int? n) => "a" + n?{|FEX0023:.ToString()|};
                string Value(int? n) => "a" + {|FEX0023:n!.Value.ToString()|};
            }
            """);

    [Fact]
    public Task Reports_a_type_parameter_that_may_be_a_value_type() =>
        VerifyAsync("""
            using System;
            class C
            {
                string Unconstrained<T>(T t) => "a" + {|FEX0023:t!.ToString()|};
                string Interface<T>(T t) where T : IComparable => "a" + {|FEX0023:t.ToString()|};
                string Struct<T>(T t) where T : struct => "a" + {|FEX0023:t.ToString()|};
                string Unmanaged<T>(T t) where T : unmanaged => "a" + {|FEX0023:t.ToString()|};
            }
            """);

    [Fact]
    public Task Ignores_ToString_with_arguments() =>
        VerifyAsync("""
            using System.Globalization;
            class C
            {
                string Format(int i) => "a" + i.ToString("x");
                string Culture(int i) => "a" + i.ToString(CultureInfo.InvariantCulture);
            }
            """);

    [Fact]
    public Task Ignores_ToString_that_is_not_in_a_concatenation() =>
        VerifyAsync("""
            using System;
            using System.Text;
            class C
            {
                string Alone(int i) => i.ToString();
                string Hole(int i) => $"{i.ToString()}";
                string EnumHole(DayOfWeek d) => $"{d.ToString()}";
                string Format(int i) => string.Format("{0}", i.ToString());
                void Append(StringBuilder sb, int i) { sb.Append(i.ToString()); }
                void Write(int i) { Console.WriteLine(i.ToString()); }
                string Compound(string s, int i) { s += i.ToString(); return s; }
                string Concat(int i) => string.Concat(i.ToString(), "x");
            }
            """);

    [Fact]
    public Task Ignores_a_concatenation_whose_other_side_is_not_a_string() =>
        VerifyAsync("""
            class C
            {
                string Object(object o, int i) => o + i.ToString();
                string Int(int i, int j) => i.ToString() + j;
                string Char(int i) => 'c' + i.ToString();
            }
            """);

    [Fact]
    public Task Ignores_ToString_of_a_reference_type_or_a_string() =>
        VerifyAsync("""
            using System;
            class C
            {
                string String(string s) => "a" + s.ToString();
                string Object(object o) => "a" + o.ToString();
                string Class<T>(T t) where T : class => "a" + t.ToString();
                string Derived<T>(T t) where T : Exception => "a" + t.ToString();
            }
            """);

    [Fact]
    public Task Ignores_a_comparison_because_the_operand_types_would_change() =>
        VerifyAsync("""
            class C
            {
                bool Left(int i) => i.ToString() == "a";
                bool Right(int i) => "a" != i.ToString();
            }
            """);
}
