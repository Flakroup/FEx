using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantPropertyPatternClauseAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantPropertyPatternClauseAnalyzer>(source);

    [Fact]
    public Task Reports_empty_braces_behind_a_type() =>
        VerifyAsync("""
            using System.Collections.Generic;
            struct S { }
            class C
            {
                bool Class(object o) => o is string {|FEX0031:{ }|};
                bool Struct(object o) => o is S {|FEX0031:{ }|};
                bool Generic(object o) => o is List<int> {|FEX0031:{ }|};
                bool Primitive(object o) => o is int {|FEX0031:{ }|};
                bool Same(string s) => s is string {|FEX0031:{ }|};
                bool Derived(System.Exception e) => e is System.InvalidOperationException {|FEX0031:{ }|};
            }
            """);

    [Fact]
    public Task Reports_empty_braces_behind_a_type_with_a_designation() =>
        VerifyAsync("""
            struct S { }
            class C
            {
                bool Class(object o) => o is string {|FEX0031:{ }|} s;
                bool Struct(object o) => o is S {|FEX0031:{ }|} s;
            }
            """);

    [Fact]
    public Task Reports_empty_braces_behind_a_positional_clause() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                bool Tuple((int, int) t) => t is (int, int) {|FEX0031:{ }|};
                bool Pair(KeyValuePair<int, int> kv) => kv is (_, _) {|FEX0031:{ }|} k;
            }
            """);

    [Fact]
    public Task Reports_it_in_a_switch_a_negation_and_a_combined_pattern() =>
        VerifyAsync("""
            class C
            {
                int Arm(object o) => o switch { string {|FEX0031:{ }|} => 1, _ => 0 };
                int Case(object o) { switch (o) { case string {|FEX0031:{ }|}: return 1; default: return 0; } }
                bool Not(object o) => o is not string {|FEX0031:{ }|};
                bool Or(object o) => o is string {|FEX0031:{ }|} or int {|FEX0031:{ }|};
                bool And(object o) => o is string {|FEX0031:{ }|} and not null;
            }
            """);

    [Fact]
    public Task Reports_it_in_a_nested_pattern() =>
        VerifyAsync("""
            class C
            {
                public object? O;
                bool M(C c) => c is { O: string {|FEX0031:{ }|} };
            }
            """);

    [Fact]
    public Task Ignores_empty_braces_that_stand_alone() =>
        VerifyAsync("""
            class C
            {
                public object? O;
                bool Plain(object? o) => o is { };
                bool Designated(object? o) => o is { } x;
                bool Nullable(int? o) => o is { };
                bool Nested(C c) => c is { O: { } };
            }
            """);

    [Fact]
    public Task Ignores_a_clause_that_tests_something() =>
        VerifyAsync("""
            class C
            {
                public int Length;
                bool Property(C c) => c is C { Length: 1 };
                bool Both(C c) => c is C { Length: > 0, Length: < 5 };
                bool Positional((int, int) t) => t is (1, 2);
            }
            """);

    [Fact]
    public Task Ignores_a_type_pattern_and_a_constant_pattern() =>
        VerifyAsync("""
            class C
            {
                bool Type(object o) => o is string;
                bool Declaration(object o) => o is string s;
                bool Constant(object o) => o is null;
            }
            """);

    [Fact]
    public Task Ignores_empty_braces_that_make_a_type_test_an_always_true_check() =>
        VerifyAsync("""
            class C
            {
                bool Plain(int i) => i is int { };
                bool Combined(int i) => i is int { } and > 0;
            }
            """);

    [Fact]
    public Task Reports_empty_braces_behind_a_type_that_can_still_fail_or_be_nullable() =>
        VerifyAsync("""
            class C
            {
                bool Boxed(object o) => o is int {|FEX0031:{ }|};
                bool Nullable(int? n) => n is int {|FEX0031:{ }|};
            }
            """);

    [Fact]
    public Task Ignores_empty_braces_where_the_type_pattern_needs_a_newer_language_version() =>
        AnalyzerTestHelper.VerifyAsync<RedundantPropertyPatternClauseAnalyzer>(
            """
            class C
            {
                int Switch(object o) { switch (o) { case string { }: return 1; default: return 2; } }
                bool Test(object o) => o is string {|FEX0031:{ }|};
            }
            """,
            languageVersion: LanguageVersion.CSharp8);
}
