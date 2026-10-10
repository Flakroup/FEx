using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantQueryOrderByAscendingKeywordAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantQueryOrderByAscendingKeywordAnalyzer>(source);

    [Fact]
    public Task Reports_ascending_on_a_single_ordering() =>
        VerifyAsync("""
            using System.Linq;
            class C { object M(int[] xs) => from x in xs orderby x {|FEX0015:ascending|} select x; }
            """);

    [Fact]
    public Task Reports_ascending_in_a_mixed_and_multiple_orderings() =>
        VerifyAsync("""
            using System.Linq;
            class C
            {
                object A(int[] xs) => from x in xs orderby x {|FEX0015:ascending|}, x % 2 descending select x;
                object B(int[] xs) => from x in xs orderby x {|FEX0015:ascending|}, x % 3 {|FEX0015:ascending|}, x select x;
                object D(int[] xs) => from x in xs
                                      where x > 0
                                      orderby
                                          x
                                          {|FEX0015:ascending|}
                                      select x;
                object E(int[][] xs) => from a in xs orderby (from x in a orderby x {|FEX0015:ascending|} select x).Count() {|FEX0015:ascending|} select a;
                object F(int[] xs) => from x in xs orderby x * x + 1 {|FEX0015:ascending|} select x;
            }
            """);

    [Fact]
    public Task Ignores_descending_and_the_implicit_direction() =>
        VerifyAsync("""
            using System.Linq;
            class C
            {
                object A(int[] xs) => from x in xs orderby x descending select x;
                object B(int[] xs) => from x in xs orderby x select x;
                object D(int[] xs) => from x in xs orderby x, x % 2 descending select x;
            }
            """);

    [Fact]
    public Task Ignores_method_syntax() =>
        VerifyAsync("""
            using System.Linq;
            class C { object M(int[] xs) => xs.OrderBy(x => x).ThenByDescending(x => x); }
            """);
}
