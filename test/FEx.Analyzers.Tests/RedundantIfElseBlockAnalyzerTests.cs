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
}
