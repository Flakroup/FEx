using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class EmptyNamespaceAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<EmptyNamespaceAnalyzer>(source);

    [Fact]
    public Task Reports_an_empty_block_scoped_namespace() =>
        VerifyAsync("""
            namespace {|FEX0013:E1|} { }
            namespace {|FEX0013:A.B.C|}
            {
            }
            """);

    [Fact]
    public Task Reports_an_empty_file_scoped_namespace() =>
        VerifyAsync("""
            namespace {|FEX0013:FS1|};
            """);

    [Fact]
    public Task Reports_a_namespace_holding_only_usings() =>
        VerifyAsync("""
            namespace {|FEX0013:U1|} { using System; }
            namespace {|FEX0013:U2|} { using Alias = System.String; }
            namespace {|FEX0013:U3|} { using static System.Math; }
            """);

    [Fact]
    public Task Reports_a_namespace_holding_only_trivia() =>
        VerifyAsync("""
            namespace {|FEX0013:T1|} { /* nothing */ }
            namespace {|FEX0013:T2|}
            {
            #region R
            #endregion
            }
            namespace {|FEX0013:T3|}
            {
            #if NEVER
                class X { }
            #endif
            }
            """);

    [Fact]
    public Task Reports_only_the_inner_namespace_of_a_nested_pair() =>
        VerifyAsync("""
            namespace N5 { namespace {|FEX0013:N5a|} { } }
            """);

    [Fact]
    public Task Ignores_a_namespace_that_declares_something() =>
        VerifyAsync("""
            namespace N1 { class C { } }
            namespace N2 { delegate void D(); }
            namespace N3 { enum E { A } }
            namespace N4 { interface I { } }
            namespace N6 { struct S { } }
            namespace N7 { record R; }
            """);
}
