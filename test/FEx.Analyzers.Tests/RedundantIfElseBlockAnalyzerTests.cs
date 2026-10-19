using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantIfElseBlockAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantIfElseBlockAnalyzer>(source);

    [Fact]
    public Task Reports_an_else_after_a_branch_that_exits_the_method() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            class C
            {
                int A(bool b) { if (b) { return 1; } {|FEX0020:else|} { Console.Write(1); } return 0; }
                int B(bool b) { if (b) return 1; {|FEX0020:else|} Console.Write(1); return 0; }
                void D(bool b) { if (b) throw new Exception(); {|FEX0020:else|} Console.Write(1); }
                IEnumerable<int> E(bool b) { if (b) yield break; {|FEX0020:else|} yield return 1; }
                void F(bool b) { while (true) { if (b) continue; {|FEX0020:else|} Console.Write(1); } }
                void G(bool b) { while (true) { if (b) break; {|FEX0020:else|} Console.Write(1); } }
                void H(bool b) { if (b) goto L; {|FEX0020:else|} Console.Write(1); L: Console.Write(2); }
                void I(bool b) { if (b) { while (true) { } } {|FEX0020:else|} Console.Write(1); }
            }
            """);

    [Fact]
    public Task Reports_an_empty_else() =>
        VerifyAsync("""
            class C
            {
                int A(bool b) { if (b) return 1; {|FEX0020:else|} { } return 0; }
            }
            """);

    [Fact]
    public Task Reports_every_else_of_a_chain() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    if (i == 1) return;
                    {|FEX0020:else|} if (i == 2) return;
                    {|FEX0020:else|} Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_an_else_when_a_nested_if_ends_both_branches() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(bool a, bool b)
                {
                    if (a)
                    {
                        if (b) return 1;
                        {|FEX0020:else|} return 2;
                    }
                    {|FEX0020:else|} { Console.Write(1); }
                    return 0;
                }
            }
            """);

    [Fact]
    public Task Reports_an_else_after_a_switch_whose_sections_all_exit() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(bool b, int i)
                {
                    if (b)
                    {
                        switch (i)
                        {
                            case 1: return 1;
                            default: return 2;
                        }
                    }
                    {|FEX0020:else|} { Console.Write(1); }
                    return 0;
                }
            }
            """);

    [Fact]
    public Task Keeps_an_if_without_an_else() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(bool b) { if (b) return 1; Console.Write(1); return 0; }
            }
            """);

    [Fact]
    public Task Keeps_an_else_after_a_branch_that_can_complete() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(bool b, bool c) { if (b) { if (c) return 1; } else { Console.Write(1); } return 0; }
                int B(bool b, int[] xs) { if (b) { foreach (var x in xs) { return x; } } else { Console.Write(1); } return 0; }
                int D(bool b, int i) { if (b) { switch (i) { case 1: return 1; } } else { Console.Write(1); } return 0; }
                void E(bool b) { if (b) Console.Write(1); else Console.Write(2); }
                void F(bool b) { if (b) { } else { Console.Write(1); } }
            }
            """);

    [Fact]
    public Task Keeps_an_else_after_a_call_that_the_compiler_cannot_see_ending() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Fail() => throw new Exception();
                int A(bool b) { if (b) { Environment.Exit(1); } else { Console.Write(1); } return 0; }
                int B(bool b) { if (b) { Fail(); } else { Console.Write(1); } return 0; }
            }
            """);

    [Fact]
    public Task Reports_an_else_in_a_switch_section_or_behind_a_label() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i, bool b) { switch (i) { case 1: if (b) break; {|FEX0020:else|} Console.Write(1); break; } }
                void B(bool b) { L: if (b) return; {|FEX0020:else|} Console.Write(1); }
            }
            """);

    [Fact]
    public Task Reports_an_else_in_top_level_statements() =>
        AnalyzerTestHelper.VerifyAsync<RedundantIfElseBlockAnalyzer>("""
            using System;
            if (args.Length > 0) return; {|FEX0020:else|} Console.Write(1);
            """, OutputKind.ConsoleApplication);

    [Fact]
    public Task Keeps_an_else_when_the_if_is_not_in_a_statement_list() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(Func<bool> c, bool b) { while (c()) if (b) break; else Console.Write(1); }
                void B(int[] xs) { foreach (var x in xs) if (x > 0) continue; else Console.Write(x); }
                void D(bool b) { for (var i = 0; i < 3; i++) if (b) break; else Console.Write(i); }
                void E(bool b) { do if (b) break; else Console.Write(1); while (b); }
                void F(IDisposable d, bool b) { using (d) if (b) return; else Console.Write(1); Console.Write(2); }
                void G(object o, bool b) { lock (o) if (b) return; else Console.Write(1); Console.Write(2); }
                void H(bool a, bool b) { if (a) if (b) return; else Console.Write(1); Console.Write(2); }
                void I(bool a, bool b) { if (a) Console.Write(0); else if (b) throw new Exception(); else Console.Write(1); }
            }
            """);

    [Fact]
    public Task Keeps_the_else_of_an_else_if_whose_earlier_branch_can_complete() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(int i)
                {
                    if (i == 1) { Console.Write(1); }
                    else if (i == 2) { return 2; }
                    else { Console.Write(3); }
                    return 0;
                }

                int B(int i)
                {
                    if (i == 1) return 1;
                    {|FEX0020:else|} if (i == 2) { Console.Write(2); }
                    else if (i == 3) return 3;
                    else Console.Write(4);
                    return 0;
                }
            }
            """);
}
