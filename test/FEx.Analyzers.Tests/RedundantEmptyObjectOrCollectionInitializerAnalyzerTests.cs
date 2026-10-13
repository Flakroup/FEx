using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantEmptyObjectOrCollectionInitializerAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantEmptyObjectOrCollectionInitializerAnalyzer>(source);

    [Fact]
    public Task Reports_an_empty_initializer_after_a_type_name() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item {|FEX0055:{ }|};
            }
            """);

    [Fact]
    public Task Reports_an_empty_initializer_after_parentheses_and_arguments() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Empty() => new Item() {|FEX0055:{ }|};
                public List<int> Arguments() => new List<int>(3) {|FEX0055:{ }|};
                public List<int> Collection() => new List<int> {|FEX0055:{ }|};
                public Dictionary<int, int> Dictionary() => new Dictionary<int, int> {|FEX0055:{ }|};
            }
            """);

    [Fact]
    public Task Reports_a_target_typed_creation_and_a_struct() =>
        VerifyAsync("""
            public struct Point { public int X; }
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Typed() => new() {|FEX0055:{ }|};
                public Point Struct() => new Point {|FEX0055:{ }|};
            }
            """);

    [Fact]
    public Task Reports_an_empty_initializer_nested_in_an_object_initializer() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public class Item { public List<int> L { get; } = new(); }
            public class C
            {
                public Item M() => new Item { L = {|FEX0055:{ }|} };
            }
            """);

    [Fact]
    public Task Reports_an_empty_initializer_on_several_lines() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item
                {|FEX0055:{
                }|};
            }
            """);

    [Fact]
    public Task Ignores_an_initializer_that_assigns_something() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Object() => new Item { P = 1 };
                public List<int> Collection() => new List<int> { 1 };
            }
            """);

    [Fact]
    public Task Ignores_an_initializer_that_holds_a_comment() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Inline() => new Item { /* kept */ };
                public Item Line() => new Item {
                    // kept
                };
            }
            """);

    [Fact]
    public Task Ignores_an_empty_array_initializer_and_a_with_expression() =>
        VerifyAsync("""
            public record Data(int A);
            public class C
            {
                public int[] Array() => new int[] { };
                public Data With(Data data) => data with { };
            }
            """);

    [Fact]
    public Task Ignores_a_creation_without_an_initializer() =>
        VerifyAsync("""
            public class Item { }
            public class C
            {
                public Item M() => new Item();
            }
            """);
}
