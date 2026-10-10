using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class LoopVariableNeverChangedAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<LoopVariableNeverChangedAnalyzer>(source);

    [Fact]
    public Task Reports_a_while_loop_on_a_local_the_body_never_writes() =>
        VerifyAsync("""
            using System;
            class C { void M() { bool c = true; while ({|FEX0002:c|}) { Console.Write(1); } } }
            """);

    [Fact]
    public Task Reports_a_do_while_loop_on_a_parameter() =>
        VerifyAsync("""
            using System;
            class C { void M(bool b) { do { Console.Write(1); } while ({|FEX0002:b|}); } }
            """);

    [Fact]
    public Task Reports_a_for_loop_whose_incrementors_leave_the_variable_alone() =>
        VerifyAsync("""
            using System;
            class C { void M() { for (int i = 0; {|FEX0002:i|} < 3;) { Console.Write(i); } } }
            """);

    [Fact]
    public Task Reports_every_variable_of_a_condition_nothing_writes_once() =>
        VerifyAsync("""
            class C { void M(int a, int b) { while ({|FEX0002:a|} < {|FEX0002:b|} && a != 0) { } } }
            """);

    [Fact]
    public Task Reports_a_negated_comparison_with_a_conversion_and_a_conditional() =>
        VerifyAsync("""
            class C { void M(byte x, bool flag) { while (!(({|FEX0002:x|} > 1 ? {|FEX0002:flag|} : false) == (flag && true))) { } } }
            """);

    [Fact]
    public Task Reports_when_a_nested_loop_writes_only_something_else() =>
        VerifyAsync("""
            class C { void M(bool outer) { for (int i = 0; i < 2; i++) { while ({|FEX0002:outer|}) { i = 5; } } } }
            """);

    [Fact]
    public Task Ignores_a_variable_assigned_in_the_body() =>
        VerifyAsync("class C { void M() { bool c = true; while (c) { c = false; } } }");

    [Fact]
    public Task Ignores_compound_assignment_and_increment_in_the_body() =>
        VerifyAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    while (a < 9) { a += 1; }
                    while (b < 9) { --b; }
                    do { } while (c++ < 9);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_written_by_the_for_incrementor() =>
        VerifyAsync("class C { void M() { for (int i = 0; i < 3; i++) { } } }");

    [Fact]
    public Task Ignores_one_changing_variable_next_to_a_fixed_one() =>
        VerifyAsync("class C { void M(int n) { for (int i = 0; i < n; i++) { } int j = 0; while (j < n) { j++; } } }");

    [Fact]
    public Task Ignores_a_variable_passed_by_ref_or_out() =>
        VerifyAsync("""
            class C
            {
                static void Bump(ref int x) { x++; }
                static void Init(out bool x) { x = false; }
                void M()
                {
                    int a = 0;
                    while (a < 3) { Bump(ref a); }
                    bool b = true;
                    while (b) { Init(out b); }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_written_through_a_ref_local_or_an_address() =>
        VerifyAsync("""
            unsafe class C
            {
                void M()
                {
                    int a = 0;
                    ref int alias = ref a;
                    while (a < 3) { alias++; }
                    int b = 0;
                    int* p = &b;
                    while (b < 3) { *p += 1; }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_written_by_a_deconstruction() =>
        VerifyAsync("class C { void M() { int a = 0, b = 0; while (a < 3) { (a, b) = (a + 1, b); } while (b < 3) { (a, (b, _)) = (0, (b + 1, 0)); } } }");

    [Fact]
    public Task Ignores_a_variable_written_inside_a_lambda_in_the_loop() =>
        VerifyAsync("""
            using System;
            class C { void M() { bool c = true; while (c) { Action stop = () => c = false; stop(); } } }
            """);

    [Fact]
    public Task Ignores_a_variable_written_by_a_closure_declared_outside_the_loop() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    bool done = false;
                    Action stop = () => done = true;
                    while (!done) { stop(); }
                    bool other = false;
                    void Finish() => other = true;
                    while (!other) { Finish(); }
                }
            }
            """);

    [Fact]
    public Task Reports_a_variable_written_only_outside_the_loop_by_the_lambda_that_declares_it() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = () =>
                    {
                        bool c = true;
                        while ({|FEX0002:c|}) { }
                        c = false;
                    };
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_a_lambda_nested_in_its_declaring_lambda_writes() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action a = () =>
                    {
                        bool c = true;
                        Action nested = () => { c = false; };
                        while (c) { nested(); }
                    };
                }
            }
            """);

    [Fact]
    public Task Ignores_fields_properties_and_calls_in_the_condition() =>
        VerifyAsync("""
            class C
            {
                private bool _f;
                bool P => true;
                bool Next() => false;
                void M(string s)
                {
                    while (_f) { }
                    while (P) { }
                    while (Next()) { }
                    while (s.Length > 0) { }
                    while (s is null) { }
                }
            }
            """);

    [Fact]
    public Task Ignores_an_assignment_inside_the_condition() =>
        VerifyAsync("class C { bool Next() => false; void M() { bool c; while ((c = Next()) && c) { } } }");

    [Fact]
    public Task Ignores_constant_conditions_and_loops_without_one() =>
        VerifyAsync("class C { void M() { const bool Yes = true; while (true) { break; } while (Yes) { break; } for (;;) { break; } } }");

    [Fact]
    public Task Ignores_ref_parameters_and_ref_locals_in_the_condition() =>
        VerifyAsync("class C { void M(ref int x, int y) { ref int r = ref y; while (x < 3) { break; } while (r < 3) { break; } } }");

    [Fact]
    public Task Ignores_foreach_loops() =>
        VerifyAsync("class C { void M(int[] items, bool b) { foreach (int item in items) { if (b) { } } } }");

    [Fact]
    public Task Ignores_a_for_loop_that_writes_the_variable_in_its_body() =>
        VerifyAsync("class C { void M() { for (int i = 0; i < 3;) { i++; } } }");

    [Fact]
    public Task Reports_a_variable_written_only_before_the_loop() =>
        VerifyAsync("class C { void M() { bool c = true; c = false; while ({|FEX0002:c|}) { } } }");

    [Fact]
    public Task Ignores_a_variable_aliased_by_a_ref_local_or_pointer_inside_the_loop() =>
        VerifyAsync("""
            unsafe class C
            {
                void M()
                {
                    int a = 0;
                    while (a < 3) { ref int alias = ref a; alias++; }
                    int b = 0;
                    while (b < 3) { int* p = &b; *p += 1; }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_primary_constructor_parameter_another_member_writes() =>
        VerifyAsync("""
            class C(bool stop)
            {
                void Run() { while (stop) { } }
                void Stop() { stop = true; }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_whose_address_a_ref_in_or_out_argument_took_outside_the_loop() =>
        VerifyAsync("""
            using System;
            using System.Runtime.InteropServices;
            class C
            {
                static Span<int> Wrap(ref int x) => MemoryMarshal.CreateSpan(ref x, 1);
                static void Take(in int x) { }
                static void Init(out int x) { x = 0; }
                void M()
                {
                    int a = 0;
                    var span = Wrap(ref a);
                    while (a < 3) { span[0]++; }
                    int b = 0;
                    Take(in b);
                    while (b < 3) { }
                    int c;
                    Init(out c);
                    while (c < 3) { }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_variable_passed_by_ref_to_a_dynamic_call() =>
        VerifyAsync("""
            class C
            {
                void Bump(ref int x) { x++; }
                void M()
                {
                    dynamic self = this;
                    int x = 0;
                    while (x < 3) { self.Bump(ref x); }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_condition_decided_by_a_user_defined_operator() =>
        VerifyAsync("""
            struct S
            {
                public int V;
                public static bool operator <(S a, S b) => a.V < b.V;
                public static bool operator >(S a, S b) => a.V > b.V;
                public void Bump() { V++; }
            }
            class C { void M() { S a = default, b = default; b.V = 3; while (a < b) { a.Bump(); } } }
            """);

    [Fact]
    public Task Ignores_a_condition_decided_by_a_user_defined_conversion() =>
        VerifyAsync("""
            struct W
            {
                public int V;
                public static implicit operator int(W w) => w.V;
                public void Bump() { V++; }
            }
            class C { void M() { W w = default; while (w < 3) { w.Bump(); } } }
            """);

    [Fact]
    public Task Reports_a_condition_on_strings_and_enums_with_the_language_operators() =>
        VerifyAsync("""
            using System;
            class C { void M(string s, DayOfWeek d) { while ({|FEX0002:s|} == "a" && {|FEX0002:d|} != DayOfWeek.Monday) { } } }
            """);

    // ReSharper 2026.2.3.1 reports a captured variable although the enclosing method writes it outside the loop.
    [Fact]
    public Task Reports_a_variable_the_enclosing_method_writes_when_the_loop_is_in_a_lambda_or_local_function() =>
        VerifyAsync("""
            using System;
            using System.Threading.Tasks;
            class C
            {
                void M()
                {
                    bool stop = false;
                    Task.Run(() => { while ({|FEX0002:stop|}) { Console.Write(1); } });
                    stop = true;
                    bool done = false;
                    void Spin() { while ({|FEX0002:done|}) { } }
                    Spin();
                    done = true;
                }
            }
            """);

    [Fact]
    public Task Reports_a_variable_that_is_only_assigned_to_something_else() =>
        VerifyAsync("class C { void M() { bool c = true, d = false; while ({|FEX0002:c|}) { d = c; } } }");

    [Fact]
    public Task Reports_a_very_long_condition_without_overflowing_the_stack() =>
        VerifyAsync("class C { void M(bool a) { while ({|FEX0002:a|} && " + string.Join(" && ", Enumerable.Repeat("a", 5000)) + ") { } } }");
}
