using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantEmptyObjectCreationArgumentListAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantEmptyObjectCreationArgumentListAnalyzer>(source);

    [Fact]
    public Task Reports_the_parentheses_before_an_object_initializer() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item{|FEX0054:()|} { P = 1 };
            }
            """);

    [Fact]
    public Task Reports_the_parentheses_before_a_collection_or_dictionary_initializer() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public class C
            {
                public List<int> List() => new List<int>{|FEX0054:()|} { 1, 2 };
                public Dictionary<int, int> Dictionary() => new Dictionary<int, int>{|FEX0054:()|} { [1] = 2 };
            }
            """);

    [Fact]
    public Task Reports_a_struct_and_a_nested_initializer() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public struct Point { public int X; }
            public class Item { public List<int> L { get; } = new(); }
            public class C
            {
                public Point Struct() => new Point{|FEX0054:()|} { X = 1 };
                public Item Nested() => new Item{|FEX0054:()|} { L = { 1 } };
            }
            """);

    [Fact]
    public Task Reports_the_parentheses_whatever_the_initializer_holds() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item{|FEX0054:()|} { };
            }
            """);

    [Fact]
    public Task Ignores_a_creation_without_an_initializer() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item();
            }
            """);

    [Fact]
    public Task Ignores_an_initializer_without_parentheses() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item M() => new Item { P = 1 };
            }
            """);

    [Fact]
    public Task Ignores_arguments() =>
        VerifyAsync("""
            using System.Collections.Generic;
            public class C
            {
                public List<int> M() => new List<int>(3) { 1 };
            }
            """);

    [Fact]
    public Task Ignores_a_target_typed_creation_and_an_anonymous_type() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Typed() => new() { P = 1 };
                public object Anonymous() => new { P = 1 };
            }
            """);

    [Fact]
    public Task Ignores_parentheses_that_hold_a_comment() =>
        VerifyAsync("""
            public class Item { public int P { get; set; } }
            public class C
            {
                public Item Before() => new Item(/* kept */) { P = 1 };
                public Item After() => new Item(
                    // kept
                    ) { P = 1 };
            }
            """);
}
