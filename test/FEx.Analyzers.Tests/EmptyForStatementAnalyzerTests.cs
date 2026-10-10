using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class EmptyForStatementAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<EmptyForStatementAnalyzer>(source);

    [Fact]
    public Task Reports_a_header_that_only_reads_and_counts() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                int _f;
                int P => 3;
                static int S => 3;
                void M1(string s) { {|FEX0012:for (var i = 0; i < s.Length; i++) ;|} }
                void M2(List<int> l) { {|FEX0012:for (var i = 0; i < l.Count; i++) ;|} }
                void M3(int[] a) { {|FEX0012:for (var i = 0; a[i] < 3; i++) ;|} }
                void M4() { {|FEX0012:for (var i = 0; i < _f; i++) ;|} }
                void M5() { {|FEX0012:for (var i = 0; i < P; i++) ;|} }
                void M6() { {|FEX0012:for (var i = 0; i < S; i++) ;|} }
                void M7() { {|FEX0012:for (var i = 0; i < sizeof(int); i++) ;|} }
                void M8() { {|FEX0012:for (var i = 0; true; i++) ;|} }
                void M9() { {|FEX0012:for (var i = 0; i < 3; ) ;|} }
                void M10() { {|FEX0012:for (var i = 0; i < 3; i++) { }|} }
                void M11() { {|FEX0012:for (var i = 0; i < 3; i++) { ; /* c */ ; }|} }
            }
            """);

    [Fact]
    public Task Reports_a_header_that_advances_only_its_own_counters() =>
        VerifyAsync("""
            class C
            {
                void M1() { {|FEX0012:for (var i = 0; i < 3; i += 2) ;|} }
                void M2() { {|FEX0012:for (var i = 0; i < 3; i = i + 1) ;|} }
                void M3() { {|FEX0012:for (var i = 0; i < 3; ++i) ;|} }
                void M4() { {|FEX0012:for (var i = 1; i < 100; i *= 2) ;|} }
                void M5() { {|FEX0012:for (var i = 0; (i = i + 1) < 3; ) ;|} }
                void M6() { {|FEX0012:for (int i = 0, j = 5; i < j; i++, j--) ;|} }
                void M7() { {|FEX0012:for (var s = ""; s.Length < 3; s += "a") ;|} }
                void M8(string t) { {|FEX0012:for (var n = t.Length; n > 0; n--) ;|} }
            }
            """);

    [Fact]
    public Task Reports_conditions_built_from_operators() =>
        VerifyAsync("""
            class C
            {
                int _f;
                void M1(string s) { {|FEX0012:for (var i = 0; (s?.Length ?? 0) < 3; i++) ;|} }
                void M2() { {|FEX0012:for (var i = 0; i < (i > 0 ? 3 : 4); i++) ;|} }
                void M3() { {|FEX0012:for (var i = 0; checked(i + 1) < 3; i++) ;|} }
                void M4() { {|FEX0012:for (var i = 0; (long)i < 3 && -i < 5 && !(i > 9); i++) ;|} }
                void M5() { {|FEX0012:for (; _f < 3;) ;|} }
            }
            """);

    [Fact]
    public Task Reports_a_pure_annotated_call_and_string_calls() =>
        VerifyAsync("""
            using System.Diagnostics.Contracts;
            class C
            {
                [Pure] static int G() => 1;
                void M1() { {|FEX0012:for (var i = 0; i < G(); i++) ;|} }
                void M2(string s) { {|FEX0012:for (var i = 0; s.Contains("a"); i++) ;|} }
            }
            """);

    [Fact]
    public Task Reports_nested_and_labeled_loops() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M1()
                {
                    for (var i = 0; i < 3; i++)
                        {|FEX0012:for (var j = 0; j < 3; j++) ;|}
                }

                void M2(bool b)
                {
                    Func<int> f = () => { {|FEX0012:for (var i = 0; i < 3; i++) ;|} return 1; };
                    L: {|FEX0012:for (var i = 0; i < 3; i++) ;|}
                    if (b) {|FEX0012:for (var i = 0; i < 3; i++) ;|}
                }
            }
            """);

    [Fact]
    public Task Ignores_a_for_without_a_condition() =>
        VerifyAsync("""
            class C
            {
                void M1() { for (;;) ; }
                void M2() { for (var i = 0; ; i++) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_for_with_a_body() =>
        VerifyAsync("""
            class C
            {
                void M() { for (var i = 0; i < 3; i++) { System.Console.Write(i); } }
            }
            """);

    [Fact]
    public Task Ignores_a_header_that_calls_a_method() =>
        VerifyAsync("""
            using System;
            class C
            {
                static int F() => 1;
                int Inst() => 1;
                static void Bump(ref int i) { i++; }
                void M1() { for (var i = 0; i < F(); i++) ; }
                void M2() { for (var i = 0; i < 3; i++, F()) ; }
                void M3() { for (int i = 0, j = F(); i < 3; i++) ; }
                void M4() { for (var i = 0; i < Inst(); i++) ; }
                void M5(Func<int, bool> f) { for (var i = 0; f(i); i++) ; }
                void M6() { for (var i = 0; i < 3; ) { Bump(ref i); } for (var i = 0; i < 3; Bump(ref i)) ; }
                void M7() { for (var i = 0; nameof(i).Length > i; i++) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_header_that_creates_an_object() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class C
            {
                void M1() { for (var i = 0; new List<int>().Count > i; i++) ; }
                void M2() { for (var i = 0; new object() != null; i++) ; }
                void M3() { for (List<int> l = new(); l.Count < 3; ) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_header_with_a_lambda() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M1() { for (var i = 0; (Func<bool>)(() => true) != null; i++) ; }
                void M2() { for (var i = 0; (Func<bool>)(delegate { return true; }) != null; i++) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_header_that_awaits_or_throws() =>
        VerifyAsync("""
            using System.Threading.Tasks;
            class C
            {
                async Task M1(Task<int> t) { for (var i = 0; i < await t; i++) ; }
                void M2(bool b) { for (var i = 0; i < (b ? 1 : throw null!); i++) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_header_that_writes_anything_but_its_own_counters() =>
        VerifyAsync("""
            class Holder { public int X; }
            class C
            {
                int _f;
                void M1() { int k; for (k = 0; k < 3; k++) ; }
                void M2() { int k = 0; for (var i = 0; i < 3; k++) ; }
                void M3() { for (var i = 0; i < 3; _f++) ; }
                void M4(int p) { for (p = 0; p < 3; ) ; }
                void M5(int p) { for (var i = 0; i < 3; i++, p++) ; }
                void M6(int[] a) { for (var i = 0; i < 3; a[0]++) ; }
                void M7(int[] a) { for (var i = 0; i < 3; a[i] = 1) ; }
                void M8() { for (var o = new Holder(); o.X < 3; o.X++) ; }
                void M9(int p) { for (var i = 0; i < 3; p = 1) ; }
                void M10(int p) { for (var i = 0; i < 3; p += 1) ; }
                void M11(int p) { for (var i = 0; i < 3; --p) ; }
                void M12(int p) { for (var i = 0; i < 3; p--) ; }
                void M13(int p) { for (var i = 0; i < 3; ++p) ; }
            }
            """);

    [Fact]
    public Task Ignores_a_ref_local() =>
        VerifyAsync("""
            class C
            {
                void M(int[] a) { for (ref int r = ref a[0]; r < 3; ) ; }
            }
            """);
}
