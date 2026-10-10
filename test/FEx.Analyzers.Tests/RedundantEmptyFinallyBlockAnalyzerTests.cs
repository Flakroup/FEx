using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantEmptyFinallyBlockAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantEmptyFinallyBlockAnalyzer>(source);

    [Fact]
    public Task Reports_an_empty_finally_after_catch_clauses() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A() { try { } catch { } {|FEX0016:finally { }|} }
                void B() { try { } catch (ArgumentException) { } catch (Exception) { } {|FEX0016:finally { }|} }
                void D(bool b) { try { } catch (Exception) when (b) { } {|FEX0016:finally { }|} }
                void E() { try { } catch { throw; } {|FEX0016:finally { }|} }
            }
            """);

    [Fact]
    public Task Reports_an_empty_finally_without_catch() =>
        VerifyAsync("""
            class C { void A() { try { } {|FEX0016:finally { }|} } }
            """);

    [Fact]
    public Task Reports_an_empty_finally_in_a_lambda_and_in_nested_try() =>
        VerifyAsync("""
            using System;
            class C
            {
                Action A = () => { try { } {|FEX0016:finally { }|} };
                void B() { try { try { } {|FEX0016:finally { }|} } catch { } }
                void D()
                {
                    try { }
                    catch { }
                    {|FEX0016:finally
                    {
                    }|}
                }
            }
            """);

    [Fact]
    public Task Reports_a_finally_holding_only_a_comment_or_disabled_text() =>
        VerifyAsync("""
            class C
            {
                void A() { try { } {|FEX0016:finally { /* nothing */ }|} }
                void B()
                {
                    try { }
                    {|FEX0016:finally
                    {
            #if NEVER
                        System.Console.Write(1);
            #endif
                    }|}
                }
            }
            """);

    [Fact]
    public Task Ignores_a_finally_that_holds_a_statement() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A() { try { } finally { ; } }
                void B() { try { } finally { { } } }
                void D() { try { } finally { Console.Write(1); } }
                void E() { try { } catch { } }
            }
            """);
}
