using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class EmptyStatementAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<EmptyStatementAnalyzer>(source);

    [Fact]
    public Task Reports_a_doubled_semicolon() =>
        VerifyAsync("""
            using System;
            class C { void M() { Console.Write(1);{|FEX0011:;|} } }
            """);

    [Fact]
    public Task Reports_every_stray_semicolon_in_a_run() =>
        VerifyAsync("""
            using System;
            class C { void M() { Console.Write(1);{|FEX0011:;|}{|FEX0011:;|} } }
            """);

    [Fact]
    public Task Reports_a_leading_semicolon_and_one_that_is_alone_in_a_block() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M(bool b)
                {
                    {|FEX0011:;|}
                    if (b) { {|FEX0011:;|} }
                    { Console.Write(1); }
                }
            }
            """);

    [Fact]
    public Task Reports_a_semicolon_after_a_block_a_local_function_or_a_return() =>
        VerifyAsync("""
            using System;
            class C
            {
                int M(bool b)
                {
                    if (b) { Console.Write(1); }{|FEX0011:;|}
                    { }{|FEX0011:;|}
                    void L() { }{|FEX0011:;|}
                    L();
                    return 1;{|FEX0011:;|}
                }
            }
            """);

    [Fact]
    public Task Reports_a_semicolon_in_a_switch_section() =>
        VerifyAsync("""
            class C
            {
                void M(int i)
                {
                    switch (i)
                    {
                        case 1: {|FEX0011:;|} break;
                        case 2: break; {|FEX0011:;|}
                        default: {|FEX0011:;|} break;
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_semicolon_in_a_lambda_block() =>
        VerifyAsync("""
            using System;
            class C { Action A = () => { {|FEX0011:;|} }; }
            """);

    [Fact]
    public Task Ignores_an_empty_body_embedded_in_a_statement() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                void M(bool b, IEnumerable<int> xs, object o, System.IDisposable d)
                {
                    if (b) ;
                    if (b) { } else ;
                    while (b) ;
                    foreach (var x in xs) ;
                    for (var i = 0; i < 3; i++) ;
                    do ; while (b);
                    lock (o) ;
                    using (d) ;
                L: ;
                }
            }
            """);
}
