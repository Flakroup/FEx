using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantStringFormatCallAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantStringFormatCallAnalyzer>(source);

    [Fact]
    public Task Reports_a_literal_without_a_placeholder() =>
        VerifyAsync(""""
            class C
            {
                string Plain() => {|FEX0024:string.Format("abc")|};
                string Capital() => {|FEX0024:System.String.Format("abc")|};
                string Empty() => {|FEX0024:string.Format("")|};
                string Spaces() => {|FEX0024:string.Format("a b  c")|};
                string Percent() => {|FEX0024:string.Format("100%")|};
                string Verbatim() => {|FEX0024:string.Format(@"c:\dir")|};
                string Raw() => {|FEX0024:string.Format("""raw""")|};
            }
            """");

    [Fact]
    public Task Reports_a_literal_whose_braces_are_all_escaped() =>
        VerifyAsync("""
            class C { string M() => {|FEX0024:string.Format("a {{ b }}")|}; }
            """);

    [Fact]
    public Task Reports_a_literal_passed_by_name() =>
        VerifyAsync("""
            class C { string M() => {|FEX0024:string.Format(format: "abc")|}; }
            """);

    [Fact]
    public Task Ignores_a_literal_with_a_placeholder() =>
        VerifyAsync("""
            class C
            {
                string WithArgument(int i) => string.Format("{0}", i);
                string Unescaped() => string.Format("a { b");
                string Closing() => string.Format("a } b");
                string Named() => string.Format("{abc}");
                string Mixed() => string.Format("{{ {0} }}", 1);
            }
            """);

    [Fact]
    public Task Ignores_a_call_with_more_than_the_format() =>
        VerifyAsync("""
            using System.Globalization;
            class C
            {
                string Provider() => string.Format(CultureInfo.InvariantCulture, "abc");
                string Unused(int i) => string.Format("abc", i);
                string Array(object[] args) => string.Format("abc", args);
            }
            """);

    [Fact]
    public Task Ignores_a_format_that_is_not_a_literal() =>
        VerifyAsync("""
            class C
            {
                private const string Format = "abc";
                string Const() => string.Format(Format);
                string Variable(string f) => string.Format(f);
                string Concatenated() => string.Format("a" + "b");
                string Interpolated(int i) => string.Format($"a{i}");
            }
            """);

    [Fact]
    public Task Ignores_a_Format_method_that_is_not_on_string() =>
        VerifyAsync("""
            using System;
            using System.Text;
            class Own { public static string Format(string s) => s; }
            class C
            {
                string Custom() => Own.Format("abc");
                void Line() { Console.WriteLine("abc"); }
                void Builder(StringBuilder sb) { sb.AppendFormat("abc"); }
            }
            """);

    [Fact]
    public Task Ignores_other_methods_of_string_that_take_one_literal() =>
        VerifyAsync("""
            class C
            {
                string Intern() => string.Intern("abc");
                bool Empty() => string.IsNullOrEmpty("abc");
            }
            """);

    [Fact]
    public Task Ignores_a_null_literal() =>
        VerifyAsync("""
            class C
            {
                string Null() => string.Format(null);
            }
            """);
}
