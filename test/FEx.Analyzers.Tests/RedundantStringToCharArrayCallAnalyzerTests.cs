using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantStringToCharArrayCallAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantStringToCharArrayCallAnalyzer>(source);

    [Fact]
    public Task Reports_a_foreach_over_ToCharArray() =>
        VerifyAsync("""
            using System;
            class C { void M(string s) { foreach (char c in {|FEX0026:s.ToCharArray()|}) Console.Write(c); } }
            """);

    [Fact]
    public Task Reports_a_foreach_over_ToCharArray_of_an_expression_or_a_literal() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Concatenated(string a, string b) { foreach (char c in {|FEX0026:(a + b).ToCharArray()|}) Console.Write(c); }
                void Literal() { foreach (char c in {|FEX0026:"abc".ToCharArray()|}) Console.Write(c); }
                void Block(string s) { foreach (char c in {|FEX0026:s.ToCharArray()|}) { Console.Write(c); } }
            }
            """);

    [Fact]
    public Task Ignores_a_loop_variable_declared_with_var() =>
        VerifyAsync("""
            using System;
            class C { void M(string s) { foreach (var c in s.ToCharArray()) Console.Write(c); } }
            """);

    [Fact]
    public Task Ignores_a_loop_variable_that_converts_the_element() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Int(string s) { foreach (int c in s.ToCharArray()) Console.Write(c); }
                void Object(string s) { foreach (object c in s.ToCharArray()) Console.Write(c); }
            }
            """);

    [Fact]
    public Task Ignores_the_range_overload() =>
        VerifyAsync("""
            using System;
            class C { void M(string s) { foreach (char c in s.ToCharArray(1, 2)) Console.Write(c); } }
            """);

    [Fact]
    public Task Ignores_a_foreach_over_something_else() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Variable(char[] chars) { foreach (char c in chars) Console.Write(c); }
                void String(string s) { foreach (char c in s) Console.Write(c); }
                void Other(Own o) { foreach (char c in o.ToCharArray()) Console.Write(c); }
                void Reversed(string s) { foreach (char c in new string(s.ToCharArray()).Trim()) Console.Write(c); }
            }
            class Own { public char[] ToCharArray() => new char[0]; }
            """);

    [Fact]
    public Task Ignores_ToCharArray_outside_a_foreach() =>
        VerifyAsync("""
            class C
            {
                int Length(string s) => s.ToCharArray().Length;
                char[] Copy(string s) => s.ToCharArray();
            }
            """);
}
