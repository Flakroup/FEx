using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantStringInterpolationAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantStringInterpolationAnalyzer>(source);

    [Fact]
    public Task Reports_an_interpolated_string_without_a_hole() =>
        VerifyAsync(""""
            class C
            {
                string Plain() => {|FEX0025:$"abc"|};
                string Empty() => {|FEX0025:$""|};
                string Verbatim() => {|FEX0025:$@"a ""b"" c"|};
                string Raw() => {|FEX0025:$"""abc"""|};
                string Escaped() => {|FEX0025:$"{{x}}"|};
                string EscapedAndText() => {|FEX0025:$"a {{ b"|};
                const string Constant = {|FEX0025:$"abc"|};
            }
            """");

    [Fact]
    public Task Reports_it_wherever_the_target_is_a_string_or_an_object() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Take(object o) { }
                void Variable() { var s = {|FEX0025:$"abc"|}; }
                void Argument() { Take({|FEX0025:$"abc"|}); }
                string Concat() => string.Concat({|FEX0025:$"abc"|}, "d");
                void Default(string s = {|FEX0025:$"abc"|}) { }
            }
            """);

    [Fact]
    public Task Ignores_an_interpolated_string_with_a_hole() =>
        VerifyAsync(""""
            class C
            {
                string Hole(int i) => $"a{i}";
                string Aligned(int i) => $"{i,5}";
                string Formatted(int i) => $"{i:x}";
                string Literal() => $"{"x"}";
                string Raw(int i) => $"""a{i}""";
                string Verbatim(int i) => $@"a{i}";
            }
            """");

    [Fact]
    public Task Ignores_a_string_that_has_to_stay_interpolated_for_its_target() =>
        VerifyAsync("""
            using System;
            class C
            {
                FormattableString Formattable() => $"abc";
                IFormattable Formattable2() => $"abc";
                void Take(FormattableString s) { }
                void Argument() { Take($"abc"); }
            }
            """);

    [Fact]
    public Task Reports_a_hole_free_string_next_to_a_plain_operand() =>
        VerifyAsync("""
            class C
            {
                string Right(string s) => s + {|FEX0025:$"a"|};
                string Left(string s) => {|FEX0025:$"a"|} + s;
                string Literal(int i) => "x" + {|FEX0025:$"a"|} + $"b{i}";
                string Before(int i, string s) => s + $"b{i}" + {|FEX0025:$"a"|};
                string Middle(int i, string s) => {|FEX0025:$"a"|} + s + $"b{i}";
                string Grouped(int i, string s) => {|FEX0025:$"a"|} + (s + $"b{i}");
                string Trailing(int i, string s) => {|FEX0025:$"a"|} + ($"b{i}" + s);
                string Mixed(int i) => {|FEX0025:$"a"|} + ($"b{i}" == {|FEX0025:$"c"|});
                bool Compared(int i) => {|FEX0025:$"a"|} == $"b{i}";
            }
            """);

    [Fact]
    public Task Ignores_a_line_of_an_interpolated_concatenation() =>
        VerifyAsync("""
            class C
            {
                string Pair() => $"a" + $"b";
                string Following(int i) => $"a" + $"b{i}";
                string Leading(int i) => $"b{i}" + $"a";
                string Chain(int i) => $"a" + $"b{i}" + $"c";
                string Tail(int i) => $"b{i}" + $"a" + $"c";
                string Grouped(int i) => $"a" + ($"b{i}");
                string Wrapped(int i) => ($"a") + $"b{i}";
                string Nested(int i) => $"a" + ($"b" + $"c{i}");
            }
            """);
}
