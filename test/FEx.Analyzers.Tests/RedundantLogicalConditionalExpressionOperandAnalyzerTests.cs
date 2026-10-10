using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantLogicalConditionalExpressionOperandAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantLogicalConditionalExpressionOperandAnalyzer>(source);

    [Fact]
    public Task Reports_a_true_operand_of_and() =>
        VerifyAsync("""
            class C
            {
                bool Right(bool a) => a && {|FEX0032:true|};
                bool Left(bool a) => {|FEX0032:true|} && a;
            }
            """);

    [Fact]
    public Task Reports_a_false_operand_of_or() =>
        VerifyAsync("""
            class C
            {
                bool Right(bool a) => a || {|FEX0032:false|};
                bool Left(bool a) => {|FEX0032:false|} || a;
            }
            """);

    [Fact]
    public Task Reports_both_operands_when_both_are_neutral() =>
        VerifyAsync("""
            class C
            {
                bool And() => {|FEX0032:true|} && {|FEX0032:true|};
                bool Or() => {|FEX0032:false|} || {|FEX0032:false|};
            }
            """);

    [Fact]
    public Task Reports_a_constant_that_is_written_out_as_an_expression() =>
        VerifyAsync("""
            class C
            {
                bool Parenthesized(bool a) => a && {|FEX0032:(true)|};
                bool Negated(bool a) => a || {|FEX0032:!true|};
                bool Double(bool a) => a && {|FEX0032:!!true|};
                bool Comparison(bool a) => a && {|FEX0032:(1 == 1)|};
                bool Default(bool a) => a || {|FEX0032:default(bool)|};
            }
            """);

    [Fact]
    public Task Reports_in_a_condition_a_chain_and_a_lambda() =>
        VerifyAsync("""
            using System;
            class C
            {
                bool Call() => true;
                void Condition(bool a) { if (a && {|FEX0032:true|}) { } }
                bool Chain(bool a, bool b) => a && {|FEX0032:true|} && b;
                bool Mixed(bool a, bool b) => a || b || {|FEX0032:false|};
                Func<bool, bool> Lambda() => a => a && {|FEX0032:true|};
                bool Invocation() => Call() && {|FEX0032:true|};
                bool Assigned(bool a) { a = a || {|FEX0032:false|}; return a; }
            }
            """);

    [Fact]
    public Task Ignores_the_operand_that_decides_the_result() =>
        VerifyAsync("""
            class C
            {
                bool AndFalse(bool a) => a && false;
                bool OrTrue(bool a) => a || true;
                bool FalseAnd(bool a) => false && a;
                bool TrueOr(bool a) => true || a;
                bool NotTrue(bool a) => a && !true;
                bool NotFalse(bool a) => a || !false;
            }
            """);

    [Fact]
    public Task Ignores_operators_that_do_not_short_circuit() =>
        VerifyAsync("""
            class C
            {
                bool And(bool a) => a & true;
                bool Or(bool a) => a | false;
                bool Xor(bool a) => a ^ true;
            }
            """);

    [Fact]
    public Task Ignores_an_operand_that_is_a_named_constant() =>
        VerifyAsync("""
            class C
            {
                private const bool Enabled = true;
                private const bool Disabled = false;
                bool Identifier(bool a) => a && Enabled;
                bool Member(bool a) => a || C.Disabled;
                bool Parenthesized(bool a) => a && (Enabled);
            }
            """);

    [Fact]
    public Task Ignores_operands_that_are_not_constant() =>
        VerifyAsync("""
            class C
            {
                bool Variables(bool a, bool b) => a && b || a;
                bool Call(bool a) => a && Call(a);
                bool Comparison(int i) => i > 0 && i < 5;
            }
            """);
}
