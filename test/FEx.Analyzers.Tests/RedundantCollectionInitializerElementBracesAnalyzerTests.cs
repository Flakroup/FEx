using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantCollectionInitializerElementBracesAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantCollectionInitializerElementBracesAnalyzer>(source);

    [Fact]
    public Task Reports_braces_around_a_single_element() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                List<int> A = new() { {|FEX0014:{|} 1 }, {|FEX0014:{|} 2 } };
                List<int> B = new List<int> { {|FEX0014:{|} 1 }, 2 };
            }
            """);

    [Fact]
    public Task Reports_braces_around_expression_kinds_that_read_as_elements() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                int _x;
                List<int> A() => new() { {|FEX0014:{|} (_x = 1) } };
                List<Func<int>> B = new() { {|FEX0014:{|} () => 1 } };
                List<int> D(bool b) => new() { {|FEX0014:{|} b ? 1 : 2 } };
                List<string?> E = new() { {|FEX0014:{|} null } };
                List<int> F = new() { {|FEX0014:{|} default } };
                List<int> G(List<int> o) => new() { {|FEX0014:{|} o.Count } };
                List<int> H(bool b) => new() { {|FEX0014:{|} b ? 1 : throw null! } };
                async Task<List<int>> I(Task<int> t) => new() { {|FEX0014:{|} await t } };
            }
            """);

    [Fact]
    public Task Keeps_braces_around_a_bare_assignment() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                int _x;
                List<int> A() => new() { { _x = 1 } };
            }
            """);

    [Fact]
    public Task Keeps_braces_around_a_collection_expression() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                List<int[]> A = new() { { [1, 2] } };
            }
            """);

    [Fact]
    public Task Ignores_unbraced_elements_and_multi_argument_elements() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                List<int> A = new() { 1, 2 };
                Dictionary<int, int> B = new() { { 1, 2 }, { 3, 4 } };
                Dictionary<int, int> D = new() { [1] = 2 };
            }
            """);
}
