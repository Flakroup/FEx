using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantEmptySwitchSectionAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantEmptySwitchSectionAnalyzer>(source);

    [Fact]
    public Task Reports_a_default_that_only_breaks() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); break;
                        {|FEX0017:default: break;|}
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_default_whose_break_sits_in_comments_or_blocks() =>
        VerifyAsync("""
            class C
            {
                void A(int i) { switch (i) { case 1: break; {|FEX0017:default: /* c */ break;|} } }
                void B(int i) { switch (i) { case 1: break; {|FEX0017:default: { /* c */ break; }|} } }
                void D(int i) { switch (i) { case 1: break; {|FEX0017:default: { { break; } }|} } }
            }
            """);

    [Fact]
    public Task Reports_a_default_in_any_position() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i) { switch (i) { {|FEX0017:default: break;|} case 1: Console.Write(1); break; } }
                void B(object o) { switch (o) { case int n when n > 0: break; case string: break; {|FEX0017:default: break;|} } }
                Action<int> L = i => { switch (i) { case 1: break; {|FEX0017:default: break;|} } };
            }
            """);

    [Fact]
    public Task Reports_a_default_when_a_goto_default_targets_only_a_nested_switch() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i, int j)
                {
                    switch (i)
                    {
                        case 1:
                            switch (j)
                            {
                                case 1: goto default;
                                default: Console.Write(2); break;
                            }
                            break;
                        {|FEX0017:default: break;|}
                    }
                }

                void B(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); goto case 2;
                        case 2: Console.Write(2); break;
                        {|FEX0017:default: break;|}
                    }
                }
            }
            """);

    [Fact]
    public Task Keeps_a_default_that_does_anything_else() =>
        VerifyAsync("""
            using System;
            class C
            {
                int A(int i) { switch (i) { case 1: break; default: return 1; } return 0; }
                void B(int i) { while (true) { switch (i) { case 1: break; default: continue; } break; } }
                void D(int i) { switch (i) { case 1: break; default: ; break; } }
                void E(int i) { switch (i) { case 1: break; default: Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_break_section_that_is_not_the_default() =>
        VerifyAsync("""
            class C
            {
                void A(int i) { switch (i) { case 1: break; case 2: break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_default_that_shares_its_section_with_case_labels() =>
        VerifyAsync("""
            class C
            {
                void A(int i) { switch (i) { case 2: default: break; } }
                void B(int i) { switch (i) { case 1 when i > 0: default: break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_default_that_a_goto_default_targets() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); goto default;
                        default: break;
                    }
                }

                void B(int i, bool b)
                {
                    switch (i)
                    {
                        case 1: { if (b) goto default; Console.Write(1); break; }
                        default: break;
                    }
                }

                void D(int i)
                {
                    switch (i)
                    {
                        case 1: while (true) { goto default; }
                        default: break;
                    }
                }
            }
            """);
}
