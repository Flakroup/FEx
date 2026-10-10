using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantExplicitNullableCreationAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantExplicitNullableCreationAnalyzer>(source);

    [Fact]
    public Task Reports_a_creation_in_a_typed_declaration_field_and_property() =>
        VerifyAsync("""
            using System;
            class C
            {
                private int? _field = {|FEX0030:new int?(1)|};
                public int? Property { get; } = {|FEX0030:new Nullable<int>(1)|};
                public int? Arrow => {|FEX0030:new int?(1)|};
                void Local() { int? local = {|FEX0030:new int?(1)|}; Nullable<int> other = {|FEX0030:new Nullable<int>(2)|}; }
                void Parenthesized() { int? local = ({|FEX0030:new int?(1)|}); }
            }
            """);

    [Fact]
    public Task Reports_a_creation_of_another_value_type() =>
        VerifyAsync("""
            using System;
            enum Mode { A }
            struct S { }
            class C
            {
                DateTime? Date() => {|FEX0030:new DateTime?(DateTime.Now)|};
                Mode? Mode_() => {|FEX0030:new Mode?(Mode.A)|};
                S? Struct(S s) => {|FEX0030:new S?(s)|};
                long? Long() => {|FEX0030:new long?(1)|};
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_a_return_and_an_assignment() =>
        VerifyAsync("""
            class C
            {
                int? _field;
                int? Return() { return {|FEX0030:new int?(1)|}; }
                void Assign() { _field = {|FEX0030:new int?(1)|}; }
                int? Lambda() { System.Func<int?> f = () => { return {|FEX0030:new int?(1)|}; }; return f(); }
                System.Func<int?> Expression() => () => {|FEX0030:new int?(1)|};
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_a_comparison() =>
        VerifyAsync("""
            class C
            {
                bool Equal(int? value) => value == {|FEX0030:new int?(1)|};
                bool NotEqual(int? value) => {|FEX0030:new int?(1)|} != value;
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_an_array_initializer_and_a_switch_arm() =>
        VerifyAsync("""
            class C
            {
                int?[] Array() => new int?[] { {|FEX0030:new int?(1)|}, null };
                int? Switch(int i) => i switch { 0 => {|FEX0030:new int?(1)|}, _ => null };
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_a_conditional_whose_other_branch_has_a_type() =>
        VerifyAsync("""
            class C
            {
                int? Second(bool b) => b ? {|FEX0030:new int?(1)|} : 2;
                int? First(bool b, int other) => b ? other : {|FEX0030:new int?(1)|};
            }
            """);

    [Fact]
    public Task Reports_a_creation_passed_as_an_argument() =>
        VerifyAsync("""
            class C
            {
                static void Take(int? value) { }
                static void Params(params object?[] values) { }
                void Direct() { Take({|FEX0030:new int?(1)|}); }
                void Named() { Take(value: {|FEX0030:new int?(1)|}); }
                void Boxed() { Params({|FEX0030:new int?(1)|}); }
                void Format() => System.Console.WriteLine("{0}", {|FEX0030:new int?(1)|});
            }
            """);

    [Fact]
    public Task Ignores_a_creation_without_a_value() =>
        VerifyAsync("""
            using System;
            class C
            {
                int? A() => new int?();
                int? B() => new Nullable<int>();
                int? C_() => new int?(default);
                int? Named() => new int?(value: 1);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_fixes_the_type_of_a_var() =>
        VerifyAsync("""
            class C { void M() { var local = new int?(1); } }
            """);

    [Fact]
    public Task Ignores_a_creation_that_is_the_operand_of_a_member_access_or_a_coalesce() =>
        VerifyAsync("""
            class C
            {
                int Member() => new int?(1).Value;
                int Coalesce() => new int?(1) ?? 2;
                string Text() => $"{new int?(1)}";
                string Concat() => "a" + new int?(1);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_in_a_conditional_whose_other_branch_has_no_type() =>
        VerifyAsync("""
            class C
            {
                int? Null(bool b) => b ? new int?(1) : null;
                int? Default(bool b) => b ? null : new int?(1);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_picks_the_overload() =>
        VerifyAsync("""
            class C
            {
                static void Take(int value) { }
                static void Take(int? value) { }
                static void Generic<T>(T value) { }
                void Run() { Take(new int?(1)); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_infers_a_generic_argument() =>
        VerifyAsync("""
            class C
            {
                static T Identity<T>(T value) => value;
                static void Take<T>(T value) { }
                void Run() { Identity(new int?(1)); Take(new int?(1)); }
            }
            """);

    [Fact]
    public Task Reports_a_creation_passed_in_a_null_conditional_call() =>
        VerifyAsync("""
            class C
            {
                void Instance(int? value) { }
                void Run(C? other) { other?.Instance({|FEX0030:new int?(1)|}); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_passed_in_a_null_conditional_call_that_picks_the_overload() =>
        VerifyAsync("""
            class C
            {
                void Instance(int value) { }
                void Instance(int? value) { }
                void Run(C? other) { other?.Instance(new int?(1)); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_passed_in_a_call_chained_after_a_null_conditional() =>
        VerifyAsync("""
            class C
            {
                C Self(int? value) => this;
                void Run(C? other) { other?.Self(new int?(1)).Self(null); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_of_another_type() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                List<int> List() => new List<int>(1);
                object Object() => new object();
                string Text() => new string('a', 1);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_with_more_arguments() =>
        VerifyAsync("""
            class C
            {
                System.DateTime Date() => new System.DateTime(1, 2, 3);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_in_a_return_of_a_lambda_that_is_an_argument() =>
        VerifyAsync("""
            using System;
            class C
            {
                static T Run<T>(Func<T> f) => f();
                int? M() => Run(() => new int?(1));
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_a_local_function_inside_a_lambda_whose_own_type_is_not_fixed() =>
        VerifyAsync("""
            class C
            {
                void M()
                {
                    var f = () =>
                    {
                        int? Local() { return {|FEX0030:new int?(1)|}; }
                        return Local();
                    };
                }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_in_a_tuple_a_compound_assignment_and_a_var_context() =>
        VerifyAsync("""
            class C
            {
                (int?, int) Pair() => (new int?(1), 2);
                void Add(int? n) { n += new int?(1); }
                void Coalesce(int? n) { n ??= new int?(1); }
                void Conditional(bool b, int? other) { var x = b ? new int?(1) : other; }
                void Switch(int i, int? other) { var x = i switch { 0 => new int?(1), _ => other }; }
                void Lambda() { var f = () => { return new int?(1); }; }
            }
            """);
}
