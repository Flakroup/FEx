using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantJumpStatementAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantJumpStatementAnalyzer>(source);

    [Fact]
    public Task Reports_a_return_at_the_end_of_a_member() =>
        VerifyAsync("""
            using System;
            class C
            {
                int _f;
                C() { Console.Write(1); {|FEX0019:return;|} }
                static C() { Console.Write(1); {|FEX0019:return;|} }
                ~C() { Console.Write(1); {|FEX0019:return;|} }
                void M() { Console.Write(1); {|FEX0019:return;|} }
                int P { set { _f = value; {|FEX0019:return;|} } }
                void L() { void Local() { Console.Write(1); {|FEX0019:return;|} } Local(); }
                Action A = () => { Console.Write(1); {|FEX0019:return;|} };
                Action B = delegate { Console.Write(1); {|FEX0019:return;|} };
            }
            """);

    [Fact]
    public Task Reports_a_return_the_last_statement_reaches_through_enclosing_constructs() =>
        VerifyAsync("""
            using System;
            class C
            {
                int _f;
                void A(bool c) { if (c) { _f++; {|FEX0019:return;|} } else { _f--; } }
                void A2(bool c) { if (c) { _f++; } else { _f--; {|FEX0019:return;|} } }
                void A3(bool c) { if (c) { _f++; } else {|FEX0019:return;|} }
                void A4(bool c) { if (c) {|FEX0019:return;|} }
                void B() { End: {|FEX0019:return;|} }
                void D() { checked { {|FEX0019:return;|} } }
                unsafe void U() { unsafe { {|FEX0019:return;|} } }
                unsafe void E(int[] a) { fixed (int* p = a) { {|FEX0019:return;|} } }
                void F(IDisposable d) { using (d) { Console.Write(1); {|FEX0019:return;|} } }
                void G(IDisposable d) { using var _ = d; Console.Write(1); {|FEX0019:return;|} }
                void H(object o) { lock (o) { Console.Write(1); {|FEX0019:return;|} } }
                void I(bool c) { try { Console.Write(1); {|FEX0019:return;|} } catch { } finally { Console.Write(2); } }
                void J(bool c) { try { } catch { Console.Write(1); {|FEX0019:return;|} } }
                void K(int[] xs) { foreach (var x in xs) Console.Write(x); {|FEX0019:return;|} }
                void L(bool b) { Action a = () => { }; {|FEX0019:return;|} }
                Action M = () => { if (Console.Read() > 0) { {|FEX0019:return;|} } };
            }
            """);

    [Fact]
    public Task Reports_a_yield_break_at_the_end_of_an_iterator() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> A() { yield return 1; {|FEX0019:yield break;|} }
                IEnumerable<int> B() { try { yield return 1; {|FEX0019:yield break;|} } finally { System.Console.Write(2); } }
            }
            """);

    [Fact]
    public Task Reports_a_continue_at_the_end_of_a_loop_body() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            class C
            {
                void A(int[] xs) { foreach (var x in xs) { Console.Write(x); {|FEX0019:continue;|} } }
                void B(bool b) { while (b) { Console.Write(1); {|FEX0019:continue;|} } }
                void D(bool b) { do { Console.Write(1); {|FEX0019:continue;|} } while (b); }
                void E() { for (var i = 0; i < 3; i++) { l: Console.Write(i); {|FEX0019:continue;|} } }
                void F(int[] xs, bool b) { foreach (var x in xs) { try { Console.Write(x); {|FEX0019:continue;|} } catch { } } }
                void G(int[] xs, object o) { foreach (var x in xs) { lock (o) { Console.Write(x); {|FEX0019:continue;|} } } }
                void H(int[] xs, bool b) { foreach (var x in xs) { if (b) { Console.Write(x); {|FEX0019:continue;|} } } }
            }
            """);

    [Fact]
    public Task Reports_a_goto_to_the_label_that_follows() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(bool b) { if (b) { Console.Write(1); {|FEX0019:goto L;|} } L: Console.Write(2); }
                void B() { {|FEX0019:goto L;|} L: Console.Write(2); }
                void D(bool b) { if (b) {|FEX0019:goto L;|} L: ; }
                Action C2 = () => { {|FEX0019:goto L;|} L: Console.Write(2); };
            }
            """);

    [Fact]
    public Task Keeps_a_goto_whose_label_is_not_next() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A() { goto L; Console.Write(1); L: Console.Write(2); }
                void B(int n) { L: Console.Write(1); if (n-- > 0) goto L; }
                void E() { goto L; M: Console.Write(1); L: Console.Write(2); }
            }
            """);

    [Fact]
    public Task Keeps_a_jump_that_has_statements_after_it() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(bool b) { if (b) return; Console.Write(1); }
                void B(int[] xs) { foreach (var x in xs) { if (x > 0) continue; Console.Write(x); } }
                void D(bool b) { if (b) { return; } else { Console.Write(1); } Console.Write(2); }
            }
            """);

    [Fact]
    public Task Keeps_a_jump_that_leaves_a_loop_or_a_switch() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(bool b, int[] xs) { if (b) { foreach (var x in xs) { Console.Write(x); return; } } }
                void B(bool b) { do { Console.Write(1); return; } while (b); }
                void D(int i) { switch (i) { case 1: Console.Write(1); return; } }
                void E(int i, int[] xs) { foreach (var x in xs) { switch (i) { case 1: Console.Write(1); continue; } } }
                void F(bool b) { while (b) { Console.Write(1); return; } }
                void G(bool b) { for (;;) { Console.Write(1); return; } }
            }
            """);

    [Fact]
    public Task Keeps_a_break_and_goto_case_and_goto_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int[] xs) { foreach (var x in xs) { Console.Write(x); break; } }
                void B(bool b) { while (b) { while (b) { Console.Write(1); break; } } }
                void D(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); goto case 2;
                        case 2: Console.Write(2); goto default;
                        default: break;
                    }
                }
            }
            """);

    [Fact]
    public Task Keeps_a_jump_followed_by_unreachable_code_in_a_switch_section() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        case 1: return; Console.Write(1); break;
                        case 2: Console.Write(2); break;
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_goto_to_the_label_that_follows_in_a_switch_section() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        case 1: {|FEX0019:goto L;|} L: Console.Write(1); break;
                    }
                }
            }
            """);

    [Fact]
    public Task Keeps_a_return_with_a_value() =>
        VerifyAsync("""
            class C { int A() { return 1; } }
            """);

    [Fact]
    public Task Keeps_a_continue_that_a_loop_nested_in_the_body_ends() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int[] xs) { foreach (var x in xs) { Console.Write(x); continue; Console.Write(1); } }
            }
            """);

    [Fact]
    public Task Keeps_a_jump_in_top_level_statements() =>
        AnalyzerTestHelper.VerifyAsync<RedundantJumpStatementAnalyzer>("""
            if (args.Length > 0)
            {
                return;
            }
            """, OutputKind.ConsoleApplication);

}
