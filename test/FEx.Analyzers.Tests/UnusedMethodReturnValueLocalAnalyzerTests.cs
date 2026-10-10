using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class UnusedMethodReturnValueLocalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<UnusedMethodReturnValueLocalAnalyzer>(source);

    [Fact]
    public Task Ignores_E03_private_nested_class_implicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface E03I { int R(); }
            public class E03 { private class In : E03I { public int R() => 1; public void M() { R(); } } public E03I G() => new In(); }
            """);

    [Fact]
    public Task Reports_E05_generic_method_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E05 { private {|FEX0049:T|} R<T>() => default!; public void M() { R<int>(); } }
            """);

    [Fact]
    public Task Ignores_N01_private_nested_record_Equals_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N01 { private record R(int A); public void M() { var r = new R(1); r.Equals(r); } }
            """);

    [Fact]
    public Task Ignores_N02_sealed_class_overrides_protected_virtual() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N02B { protected virtual int G() => 1; }
            public sealed class N02 : N02B { protected override int G() => 2; public void M() { G(); } }
            """);

    [Fact]
    public Task Ignores_N03_virtual_member_of_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N03 { private class In { public virtual int G() => 1; public void M() { G(); } } private class Der : In { public override int G() => 2; } public void Run() { new In().M(); new Der().M(); } }
            """);

    [Fact]
    public Task Ignores_N04_abstract_member_of_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N04 { private abstract class In { public abstract int G(); public void M() { G(); } } private class Der : In { public override int G() => 2; } public void Run() { new Der().M(); } }
            """);

    [Fact]
    public Task Ignores_N05_extension_called_as_instance_and_as() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            internal static class N05 { private static int Rn(this int x) => x; public static void M() { 5.Rn(); int y = Rn(5); Console.Write(y); } }
            """);

    [Fact]
    public Task Reports_N05b_extension_called_as_instance_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            internal static class N05bExt { private static {|FEX0049:int|} Q(this int x) => x; public static void Z() { 5.Q(); } }
            """);

    [Fact]
    public Task Ignores_N06_generic_method_ignored_for_one_instantiation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N06 { private T R<T>() => default!; public void M() { R<int>(); string s = R<string>(); Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_N07_await_in_parentheses() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N07 { private Task<int> R() => Task.FromResult(1); public async Task M() { await (R()); } }
            """);

    [Fact]
    public Task Reports_N08_for_loop_step() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N08 { private {|FEX0049:int|} R() => 1; public void M() { for (var i = 0; i < 3; R()) { i++; } } }
            """);

    [Fact]
    public Task Ignores_N34_private_delegate_invoked() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N34 { private delegate int D(); private void M(D d) { d(); } }
            """);

    [Fact]
    public Task Ignores_N35_local_function_in_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N35 { private class In { public void M() { int L() => 1; L(); } } public void G() => new In().M(); }
            """);

    [Fact]
    public Task Ignores_N36_explicit_interface_implementation_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface N36I { int R(); }
            public class N36 : N36I { int N36I.R() => 1; public void M() { ((N36I)this).R(); } }
            """);

    [Fact]
    public Task Reports_N37_private_extended_partial_method_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class N37 { private partial {|FEX0049:int|} R(); public void M() { R(); } }
            public partial class N37 { private partial {|FEX0049:int|} R() => 1; }
            """);

    [Fact]
    public Task Reports_UR1_conditional_access_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR1 { private {|FEX0049:int|} R() => 1; public void M(UR1? o) { o?.R(); } }
            """);

    [Fact]
    public Task Ignores_UR2_nameof_plus_ignored_call() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR2 { private int R() => 1; public string N() => nameof(R); public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_UR3_await_with_configureawait() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR3 { private async {|FEX0049:Task<int>|} R() { await Task.Yield(); return 1; } public async Task M() { await R().ConfigureAwait(false); } }
            """);

    [Fact]
    public Task Ignores_UR4_ValueTask_non_generic_awaited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR4 { private async ValueTask R() { await Task.Yield(); } public async Task M() { await R(); } }
            """);

    [Fact]
    public Task Ignores_UR5_Task_non_generic_wait() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR5 { private Task R() => Task.CompletedTask; public void M() { R().Wait(); } }
            """);

    [Fact]
    public Task Ignores_UR6_method_group_plus_ignored_call() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR6 { private int R() => 1; public void M() { Func<int> f = R; Console.Write(f); R(); } }
            """);

    [Fact]
    public Task Ignores_UR7_protected_in_unsealed_public_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR7 { protected int R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_UR8_Task_int_not_awaited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR8 { private {|FEX0049:Task<int>|} R() => Task.FromResult(1); public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_UR9_void_returning_Task_awaited_with_ConfigureAwait() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR9 { private async Task R() { await Task.Yield(); } public async Task M() { await R().ConfigureAwait(false); } }
            """);

    [Fact]
    public Task Ignores_UR10_call_result_discarded_with_assignment_to() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR10 { private int R() => 1; public void M() { int x; x = R(); Console.Write(x); } }
            """);

    [Fact]
    public Task Reports_UR11_private_method_in_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR11 { private class In { public {|FEX0049:int|} R() => 1; } public void M() { new In().R(); } }
            """);

    [Fact]
    public Task Reports_UR12_ref_return_method_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR12 { private int _f; private {|FEX0049:ref int|} R() => ref _f; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_UR13_invoked_from_a_lambda_statement_and() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR13 { private int R() => 1; public int M() { Action a = () => { R(); }; a(); return R() + 1; } }
            """);

    [Fact]
    public Task Ignores_UR14_ignored_call_inside_using_expression() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR14 { private IDisposable R() => new MemoryStream(); public void M() { using (R()) { } } }
            """);

    [Fact]
    public Task Reports_UR15_ignored_call_with_extension_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR15 { private {|FEX0049:int|} R() => 1; public void M() { switch (1) { case 1: R(); break; } } }
            """);

    [Fact]
    public Task Ignores_UR16_constructor_static_private_static_returning_void_like_Task() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR16 { private static Task R() => Task.CompletedTask; public static async Task M() { await R(); } }
            """);

    [Fact]
    public Task Reports_UR17_returns_tuple_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR17 { private {|FEX0049:(int, int)|} R() => (1, 2); public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_UR18_returns_generic_list_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR18 { private {|FEX0049:List<int>|} R() => new(); public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_UR19_delegate_invoke_result_private_method_is() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR19 { private {|FEX0049:Action|} R() => () => { }; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_UR20_pattern_if_R_is_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class UR20 { private object R() => 1; public void M() { if (R() is int) Console.Write(1); } }
            """);

    [Fact]
    public Task Reports_X01_oracle_private_method_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X01 { private {|FEX0049:int|} R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X02_result_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X02 { private int R() => 1; public int M() { var x = R(); return x; } }
            """);

    [Fact]
    public Task Ignores_X03_void() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X03 { private void R() { } public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X04_method_group() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X04 { private int R() => 1; public Func<int> M() => R; }
            """);

    [Fact]
    public Task Ignores_X05_internal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X05 { internal int R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X06_one_use_one_ignore() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X06 { private int R() => 1; public int M() { R(); return R(); } }
            """);

    [Fact]
    public Task Reports_X07_two_callers_both_ignore() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X07 { private {|FEX0049:int|} R() => 1; public void M() { R(); } public void N() { R(); } }
            """);

    [Fact]
    public Task Ignores_X08_never_called() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X08 { private int R() => 1; }
            """);

    [Fact]
    public Task Reports_X09_async_awaited_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X09 { private async {|FEX0049:Task<int>|} R() { await Task.Yield(); return 1; } public async Task M() { await R(); } }
            """);

    [Fact]
    public Task Reports_X10_Task_not_awaited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X10 { private {|FEX0049:Task|} R() => Task.CompletedTask; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X11_discard() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X11 { private int R() => 1; public void M() { _ = R(); } }
            """);

    [Fact]
    public Task Reports_X12_static() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X12 { private static {|FEX0049:int|} R() => 1; public static void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X13_protected_in_sealed_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class X13 { protected {|FEX0049:int|} R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X14_public_method_in_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X14 { private class In { public {|FEX0049:int|} R() => 1; public void M() { R(); } } public void G() => new In().M(); }
            """);

    [Fact]
    public Task Ignores_X15_explicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface X15I { int R(); }
            public class X15 : X15I { int X15I.R() => 1; public void M() { ((X15I)this).R(); } }
            """);

    [Fact]
    public Task Ignores_X16_local_function_result_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X16 { public void M() { int L() => 1; L(); } }
            """);

    [Fact]
    public Task Ignores_X17_used_in_condition() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X17 { private bool R() => true; public void M() { if (R()) Console.Write(1); } }
            """);

    [Fact]
    public Task Reports_X18_used_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X18 { private {|FEX0049:int|} R() => 1; public Action M() => () => R(); }
            """);

    [Fact]
    public Task Reports_X19_IDisposable_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X19 { private {|FEX0049:IDisposable|} R() => new MemoryStream(); public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X20_fluent_returns_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X20 { private {|FEX0049:X20|} R() => this; public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X21_out_param_bool_ignored() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X21 { private {|FEX0049:bool|} R(out int a) { a = 1; return true; } public void M() { R(out _); } }
            """);

    [Fact]
    public Task Ignores_X22_used_via_await_result() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X22 { private async Task<int> R() { await Task.Yield(); return 1; } public async Task<int> M() { return await R(); } }
            """);

    [Fact]
    public Task Ignores_X24_private_method_returning_Task_awaited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X24 { private async Task R() { await Task.Yield(); } public async Task M() { await R(); } }
            """);

    [Fact]
    public Task Reports_X25_private_called_through_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X25 { private {|FEX0049:int|} R() => 1; public void M() { this.R(); } }
            """);

    [Fact]
    public Task Reports_X26_private_called_on_other_instance() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X26 { private {|FEX0049:int|} R() => 1; public void M(X26 o) { o.R(); } }
            """);

    [Fact]
    public Task Reports_X27_private_override_private_cannot_private_extension() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public static class X27 { private static {|FEX0049:int|} R(this int a) => a; public static void M() { 1.R(); } }
            """);

    [Fact]
    public Task Ignores_X28_private_method_used_in_expression_bodied_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X28 { private int R() => 1; public int M() => R(); }
            """);

    [Fact]
    public Task Reports_X29_private_generic() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X29 { private {|FEX0049:T|} R<T>() => default!; public void M() { R<int>(); } }
            """);

    [Fact]
    public Task Reports_X30_private_method_with_side_effect_result() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X30 { private {|FEX0049:int|} R() => 1; public void M() { for (var i = 0; i < 3; R()) { i++; } } }
            """);

    [Fact]
    public Task Reports_X31_ignored_in_lambda_statement_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X31 { private {|FEX0049:int|} R() => 1; public Action M() => () => { R(); }; }
            """);

    [Fact]
    public Task Reports_X32_private_in_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public struct X32 { private {|FEX0049:int|} R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X33_private_method_used_with_nameof_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X33 { private int R() => 1; public string M() => nameof(R); }
            """);

    [Fact]
    public Task Reports_X34_private_virtual_invalid_private_returning_bool() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X34 { private {|FEX0049:bool|} R() => true; public void M() { try { R(); } catch (Exception) { Console.Write(1); } } }
            """);

    [Fact]
    public Task Ignores_X35_private_method_result_used_via_ternary() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X35 { private int R() => 1; public int M(bool b) => b ? R() : 0; }
            """);

    [Fact]
    public Task Ignores_X36_private_method_used_as_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X36 { private int R() => 1; public void M() { Console.Write(R()); } }
            """);

    [Fact]
    public Task Ignores_X37_private_method_called_in_null_conditional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X37 { private string? R() => null; public void M() { R()?.ToString(); } }
            """);

    [Fact]
    public Task Reports_X38_private_method_in_generic_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X38<T> { private {|FEX0049:int|} R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X39_private_method_in_nested_public_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X39 { public class In { private {|FEX0049:int|} R() => 1; public void M() { R(); } } }
            """);

    [Fact]
    public Task Ignores_X40_private_method_used_in_discard_assignment() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X40 { private int R() => 1; public void M() { (_, _) = (R(), 1); } }
            """);

    [Fact]
    public Task Ignores_X41_internal_in_internal_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            internal class X41 { internal int R() => 1; public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X42_private_called_inside_the_method_itself() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X42 { private int R() { return R(); } public void M() { R(); } }
            """);

    [Fact]
    public Task Reports_X43_private_async_method_ignoring_result_not() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X43 { private async {|FEX0049:Task<int>|} R() { await Task.Yield(); return 1; } public void M() { R(); } }
            """);

    [Fact]
    public Task Ignores_X44_private_method_called_by_a_delegate() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X44 { private int R() => 1; public void M() { Func<int> f = new Func<int>(R); f(); } }
            """);

    [Fact]
    public Task Reports_X45_expression_statement_via_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class X45 { private {|FEX0049:int|} R() => 1; public int P { get { R(); return 1; } } }
            """);

    [Fact]
    public Task Reports_a_call_in_the_step_of_a_for_loop() =>
        VerifyAsync("""
            public class C { private {|FEX0049:int|} Next() => 1; public void M() { for (var i = 0; i < 3; Next()) { i++; } } }
            """);

    [Fact]
    public Task Ignores_a_used_result_of_an_extension_call() =>
        VerifyAsync("""
            public static class C { private static int Ext(this int x) => x; public static int M() { var y = 1.Ext(); return y; } }
            """);

    [Fact]
    public Task Reports_an_unused_result_of_an_extension_call() =>
        VerifyAsync("""
            public static class C { private static {|FEX0049:int|} Ext(this int x) => x; public static void M() { 1.Ext(); } }
            """);

    [Fact]
    public Task Ignores_a_DllImport_method() =>
        VerifyAsync("""
            using System.Runtime.InteropServices;

            public class C { [DllImport("k")] private static extern int Free(int h); public void M() { Free(1); } }
            """);

    [Fact]
    public Task Ignores_an_extern_method_without_an_attribute() =>
        VerifyAsync("""
            public class C { private static extern int Free(int h); public void M() { Free(1); } }
            """);
}
