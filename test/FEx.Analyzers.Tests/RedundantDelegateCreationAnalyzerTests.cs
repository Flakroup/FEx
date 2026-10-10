using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantDelegateCreationAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantDelegateCreationAnalyzer>(source);

    [Fact]
    public Task Reports_a_method_group_subscribed_to_or_unsubscribed_from_an_event() =>
        VerifyAsync("""
            using System;
            class C
            {
                public event EventHandler? E;
                private void H(object? s, EventArgs a) { }
                void Subscribe() { E += {|FEX0028:new EventHandler(H)|}; }
                void Unsubscribe() { E -= {|FEX0028:new EventHandler(H)|}; }
            }
            """);

    [Fact]
    public Task Reports_a_creation_in_a_typed_declaration_assignment_return_and_initializer() =>
        VerifyAsync("""
            using System;
            class C
            {
                private Action _field = {|FEX0028:new Action(Run)|};
                static void Run() { }
                Action Return() => {|FEX0028:new Action(Run)|};
                void Local() { Action a = {|FEX0028:new Action(Run)|}; }
                void Assign() { Action a; a = {|FEX0028:new Action(Run)|}; }
                void Parenthesized() { Action a = ({|FEX0028:new Action(Run)|}); }
                Action Coalesce(Action? a) => a ?? {|FEX0028:new Action(Run)|};
            }
            """);

    [Fact]
    public Task Reports_a_lambda_an_anonymous_method_and_a_delegate_variable() =>
        VerifyAsync("""
            using System;
            class C
            {
                Action Lambda() => {|FEX0028:new Action(() => { })|};
                Action Anonymous() => {|FEX0028:new Action(delegate { })|};
                Action Variable(Action a) => {|FEX0028:new Action(a)|};
                Func<int, int> Func() => {|FEX0028:new Func<int, int>(x => x)|};
            }
            """);

    [Fact]
    public Task Reports_a_creation_passed_as_an_argument() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                static void Run() { }
                static void Take(Action a) { }
                void Direct() { Take({|FEX0028:new Action(Run)|}); }
                void Named() { Take(a: {|FEX0028:new Action(Run)|}); }
                Task Task_() => Task.Run({|FEX0028:new Action(Run)|});
                static void Take(Action<int> a) { }
            }
            """);

    [Fact]
    public Task Reports_a_creation_whose_delegate_type_differs_from_the_target_by_variance() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Handle(object o) { }
                Action<string> M() => {|FEX0028:new Action<string>(Handle)|};
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_names_the_type_of_a_var() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                void M() { var a = new Action(Run); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_is_the_operand_of_a_member_access() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                void Invoke() { new Action(Run).Invoke(); }
                void Conditional() { new Action(Run)?.Invoke(); }
                string Parenthesized() => (new Action(Run)).ToString()!;
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_needs_the_type_to_infer_one() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                Action? Conditional(bool b) => b ? new Action(Run) : null;
                Delegate Delegate_() => new Action(Run);
                object Object() => new Action(Run);
                Delegate Combine(Action a) => Delegate.Combine(new Action(Run), a)!;
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_converts_between_delegate_types() =>
        VerifyAsync("""
            using System;
            delegate void First();
            delegate void Second();
            class C { Second M(First f) => new Second(f); }
            """);

    [Fact]
    public Task Ignores_a_creation_that_picks_the_overload() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void M() { }
                static void M(int x) { }
                static void Take(Action a) { }
                static void Take(Action<int> a) { }
                void Run() { Take(new Action(M)); }
            }
            """);

    [Fact]
    public Task Ignores_creations_that_are_not_delegates() =>
        VerifyAsync("""
            using System;
            class Plain { public Plain(int i) { } }
            class C
            {
                Plain One() => new Plain(1);
                Plain Two(Plain p) => new Plain(p == null ? 1 : 2);
                object Three() => new object();
            }
            """);

    [Fact]
    public Task Ignores_a_creation_that_is_an_operand_of_a_binary_operator() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                Action Combine() => new Action(Run) + new Action(Run);
                bool Compare(Action a) => a == new Action(Run);
            }
            """);

    [Fact]
    public Task Reports_a_creation_that_is_a_tuple_component() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                (Action, int) Pair() => ({|FEX0028:new Action(Run)|}, 1);
            }
            """);

    [Fact]
    public Task Ignores_a_creation_passed_to_a_call_that_does_not_resolve() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Run() { }
                void M() { {|CS0103:Missing|}(new Action(Run)); }
            }
            """);

    [Fact]
    public Task Ignores_a_creation_of_a_type_that_converts_to_a_delegate() =>
        VerifyAsync("""
            using System;
            class Wrapper
            {
                public Wrapper(Action action) { }
                public static implicit operator Action(Wrapper wrapper) => () => { };
            }
            class C
            {
                static void Run() { }
                Action M() => new Wrapper(Run);
            }
            """);
}
