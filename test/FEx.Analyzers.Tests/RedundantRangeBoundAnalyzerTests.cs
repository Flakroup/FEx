using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantRangeBoundAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantRangeBoundAnalyzer>(source);

    [Fact]
    public Task Reports_a_zero_start() =>
        VerifyAsync("""
            using System;
            class C
            {
                int[] A(int[] a) => a[{|FEX0021:0|}..2];
                int[] B(int[] a) => a[{|FEX0021:0|}..];
                int[] D(int[] a, int i) => a[{|FEX0021:0|}..i];
                int[] E(int[] a) => a[{|FEX0021:0|}..^1];
                Span<int> F(Span<int> s) => s[{|FEX0021:0|}..2];
                string G(string s) => s[{|FEX0021:0|}..2];
                Range H = {|FEX0021:0|}..3;
            }
            """);

    [Fact]
    public Task Reports_a_zero_start_behind_parentheses_or_a_cast() =>
        VerifyAsync("""
            using System;
            class C
            {
                int[] A(int[] a) => a[{|FEX0021:(0)|}..2];
                int[] B(int[] a) => a[{|FEX0021:((0))|}..2];
                int[] D(int[] a) => a[{|FEX0021:(Index)0|}..];
                int[] E(int[] a) => a[{|FEX0021:(Index)(0)|}..2];
            }
            """);

    [Fact]
    public Task Reports_an_end_that_is_the_index_from_end_zero() =>
        VerifyAsync("""
            using System;
            class C
            {
                int[] A(int[] a) => a[1..{|FEX0021:^0|}];
                int[] B(int[] a, int n) => a[n..{|FEX0021:^0|}];
                int[] D(int[] a) => a[..{|FEX0021:^0|}];
                int[] E(int[] a) => a[^3..{|FEX0021:^0|}];
                int[] F(int[] a) => a[^0..{|FEX0021:^0|}];
                int[] G(int[] a) => a[1..{|FEX0021:^(0)|}];
                Range H = 1..{|FEX0021:^0|};
            }
            """);

    [Fact]
    public Task Reports_both_bounds_of_the_full_range() =>
        VerifyAsync("""
            class C
            {
                int[] A(int[] a) => a[{|FEX0021:0|}..{|FEX0021:^0|}];
            }
            """);

    [Fact]
    public Task Reports_a_bound_that_folds_to_the_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                const int Zero = 0;
                int[] A(int[] a) => a[{|FEX0021:Zero|}..];
                int[] B(int[] a) => a[..{|FEX0021:^Zero|}];
                int[] D(int[] a) => a[{|FEX0021:(1 - 1)|}..];
                int[] E(int[] a) => a[1..{|FEX0021:^(1 - 1)|}];
                int[] F(int[] a) => a[{|FEX0021:-0|}..];
                int[] G(int[] a) => a[{|FEX0021:(Index)Zero|}..];
                int[] H(int[] a) { const int z = 0; return a[{|FEX0021:z|}..]; }
            }
            """);

    [Fact]
    public Task Reports_a_start_written_as_a_new_index() =>
        VerifyAsync("""
            using System;
            class C
            {
                const int Zero = 0;
                int[] A(int[] a) => a[{|FEX0021:new Index(0)|}..];
                int[] B(int[] a) => a[{|FEX0021:new Index(Zero, false)|}..];
                int[] D(int[] a) => a[{|FEX0021:new System.Index(0)|}..2];
                int[] E(int[] a) => a[{|FEX0021:(new Index(0))|}..];
            }
            """);

    [Fact]
    public Task Keeps_an_index_that_is_not_the_start_of_the_sequence() =>
        VerifyAsync("""
            using System;
            class C
            {
                int[] A(int[] a) => a[new(0)..];
                int[] B(int[] a) => a[..new Index(0, true)];
                int[] D(int[] a) => a[..new Index(0)];
                int[] E(int[] a) => a[new Index(1)..];
                int[] F(int[] a) => a[new Index(0, true)..];
                int[] G(int[] a, int n) => a[new Index(n)..];
                int[] H(int[] a) => a[Index.FromStart(0)..];
                int[] I(int[] a) { Index i = new(0); return a[i..]; }
            }
            """);

    [Fact]
    public Task Keeps_bounds_that_are_not_defaults() =>
        VerifyAsync("""
            using System;
            class C
            {
                int[] A(int[] a) => a[^0..];
                int[] B(int[] a) => a[1..3];
                int[] D(int[] a) => a[..];
                int[] E(int[] a) => a[1..^1];
                int[] F(int[] a) => a[..2];
                int[] G(int[] a) => a[1..];
                int[] J(int[] a, int n) => a[n..^n];
                int[] M2(int[] a) => a[1..-0];
                int[] M3(int[] a) => a[1..+0];
                Index L = ^0;
                Range M = new Range(0, 1);
                Range N = new Range(Index.Start, Index.End);
            }
            """);
}
