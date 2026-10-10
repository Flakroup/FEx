using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RemoveRedundantOrStatementFalseAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RemoveRedundantOrStatementFalseAnalyzer>(source);

    [Fact]
    public Task Reports_or_assign_false_on_a_local_a_field_and_a_property() =>
        VerifyAsync("""
            class C
            {
                private bool _field;
                private static bool _static;
                public bool Property { get; set; }
                void Local() { var a = true; {|FEX0033:a |= false;|} }
                void Field() { {|FEX0033:_field |= false;|} }
                void This() { {|FEX0033:this._field |= false;|} }
                void Static() { {|FEX0033:_static |= false;|} }
                void Prop() { {|FEX0033:Property |= false;|} }
                void Other(C other) { {|FEX0033:other._field |= false;|} }
            }
            """);

    [Fact]
    public Task Reports_or_assign_false_on_an_element_a_parameter_and_in_a_lambda() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Element(bool[] array) { {|FEX0033:array[0] |= false;|} }
                void Reference(ref bool flag) { {|FEX0033:flag |= false;|} }
                Action Lambda(bool a) => () => { {|FEX0033:a |= false;|} };
            }
            """);

    [Fact]
    public Task Reports_a_statement_followed_by_a_comment() =>
        VerifyAsync("""
            class C
            {
                void M(bool a)
                {
                    {|FEX0033:a |= false;|} // keep
                }
            }
            """);

    [Fact]
    public Task Ignores_or_assign_of_something_other_than_the_false_literal() =>
        VerifyAsync("""
            class C
            {
                private const bool Never = false;
                void Run(bool a, bool b)
                {
                    a |= true;
                    a |= b;
                    a |= (false);
                    a |= Never;
                    a |= 1 == 2;
                }
            }
            """);

    [Fact]
    public Task Ignores_other_operators() =>
        VerifyAsync("""
            class C
            {
                void Run(bool a)
                {
                    a = a | false;
                    a &= true;
                    a &= false;
                    a ^= false;
                    a = false;
                }
            }
            """);

    [Fact]
    public Task Ignores_or_assign_on_a_value_that_is_not_a_plain_bool() =>
        VerifyAsync("""
            using System;
            [Flags] enum Options { None = 0, A = 1 }
            class C
            {
                void Run(int i, bool? maybe, Options options, uint u)
                {
                    i |= 0;
                    maybe |= false;
                    options |= Options.None;
                    u |= 0u;
                }
            }
            """);

    [Fact]
    public Task Ignores_or_assign_false_that_is_not_a_statement() =>
        VerifyAsync("""
            using System;
            class C
            {
                bool Value(bool a) => a |= false;
                bool Assigned(bool a, bool b) { b = a |= false; return b; }
                Func<bool> Lambda(bool a) => () => a |= false;
                void Loop(bool a) { for (; ; a |= false) { } }
            }
            """);
}
