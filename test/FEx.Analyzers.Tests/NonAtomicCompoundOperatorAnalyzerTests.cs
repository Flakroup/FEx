using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class NonAtomicCompoundOperatorAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<NonAtomicCompoundOperatorAnalyzer>(source);

    [Fact]
    public Task Reports_postfix_increment() =>
        VerifyAsync("class C { private volatile int _v; void M() { {|FEX0001:_v++|}; } }");

    [Fact]
    public Task Reports_prefix_decrement() =>
        VerifyAsync("class C { private volatile int _v; void M() { {|FEX0001:--_v|}; } }");

    [Fact]
    public Task Reports_compound_assignment() =>
        VerifyAsync("class C { private volatile int _v; void M() { {|FEX0001:_v |= 4|}; } }");

    [Fact]
    public Task Reports_a_static_field_and_a_field_reached_through_another_instance() =>
        VerifyAsync("""
            class C
            {
                private static volatile int s_v;
                private volatile bool _flag;
                void M(C other) { {|FEX0001:s_v += 2|}; {|FEX0001:other._flag &= true|}; }
            }
            """);

    [Fact]
    public Task Ignores_a_field_that_is_not_volatile() =>
        VerifyAsync("class C { private int _v; void M() { _v++; _v += 1; } }");

    [Fact]
    public Task Ignores_reads_and_plain_assignments_of_a_volatile_field() =>
        VerifyAsync("class C { private volatile int _v; int M() { _v = 1; var copy = _v; return copy + _v; } }");

    [Fact]
    public Task Ignores_interlocked_calls() =>
        VerifyAsync("""
            using System.Threading;
            class C { private volatile int _v; void M() { Interlocked.Increment(ref _v); Interlocked.Add(ref _v, 2); } }
            """);

    [Fact]
    public Task Ignores_a_compound_assignment_to_a_local_and_a_property() =>
        VerifyAsync("class C { private volatile int _v; int P { get; set; } void M() { var local = _v; local++; P += 1; } }");

    [Fact]
    public Task Reports_null_coalescing_assignment_to_a_volatile_field() =>
        VerifyAsync("class C { private volatile string? _v; void M() { {|FEX0001:_v ??= \"x\"|}; } }");

    [Fact]
    public Task Ignores_null_coalescing_assignment_to_a_field_that_is_not_volatile() =>
        VerifyAsync("class C { private string? _v; void M() { _v ??= \"x\"; } }");
}
