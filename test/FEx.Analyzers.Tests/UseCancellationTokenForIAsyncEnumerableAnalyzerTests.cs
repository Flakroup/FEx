using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class UseCancellationTokenForIAsyncEnumerableAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<UseCancellationTokenForIAsyncEnumerableAnalyzer>(source);

    [Fact]
    public Task Reports_A01_oracle() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A01 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A02_no_CT_in_scope() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A02 { public async Task M(IAsyncEnumerable<int> e) { await foreach (int x in e) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_A03_WithCancellation_already() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A03 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in e.WithCancellation(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_A04_ConfigureAwait_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A04 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in {|FEX0053:e.ConfigureAwait(false)|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A05_async_iterator_method_call_receiving_ct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A05 { private async IAsyncEnumerable<int> Items([EnumeratorCancellation] CancellationToken ct = default) { await Task.Yield(); Console.Write(ct); yield return 1; } public async Task M(CancellationToken ct) { await foreach (int x in Items(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_A06_async_iterator_call_without_passing_ct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A06 { private async IAsyncEnumerable<int> Items([EnumeratorCancellation] CancellationToken ct = default) { await Task.Yield(); Console.Write(ct); yield return 1; } public async Task M(CancellationToken ct) { await foreach (int x in Items()) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A07_lambda_with_ct_in_outer_scope() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A07 { public Func<Task> M(IAsyncEnumerable<int> e, CancellationToken ct) => async () => { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); }; }
            """);

    [Fact]
    public Task Reports_A08_CT_as_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A08 { private CancellationToken _ct; public async Task M(IAsyncEnumerable<int> e) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(_ct); } }
            """);

    [Fact]
    public Task Ignores_A09_sync_foreach_over_IEnumerable() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A09 { public async Task M(IEnumerable<int> e, CancellationToken ct) { foreach (int x in e) { Console.Write(x); } await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A11_inside_async_iterator_with_EnumeratorCancellation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A11 { public async IAsyncEnumerable<int> M(IAsyncEnumerable<int> e, [EnumeratorCancellation] CancellationToken ct = default) { await foreach (int x in {|FEX0053:e|}) { yield return x; } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A12_WithCancellation_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A12 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in e.WithCancellation(default)) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A14_custom_type_with_GetAsyncEnumerator_CT() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A14Seq { public A14Enum GetAsyncEnumerator(CancellationToken ct = default) => new(); }
            public class A14Enum { public int Current => 1; public ValueTask<bool> MoveNextAsync() => new(false); public ValueTask DisposeAsync() => default; }
            public class A14 { public async Task M(A14Seq e, CancellationToken ct) { await foreach (int x in e) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A15_CT_local_variable() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A15 { public async Task M(IAsyncEnumerable<int> e) { var ct = CancellationToken.None; await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A16_deconstruction() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A16 { public async Task M(IAsyncEnumerable<(int, int)> e, CancellationToken ct) { await foreach (var (a, b) in {|FEX0053:e|}) { Console.Write(a + b); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A17_local_function_with_CT_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A17 { public async Task M(IAsyncEnumerable<int> e) { async Task L(CancellationToken ct) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); } await L(default); } }
            """);

    [Fact]
    public Task Reports_A18_optional_CT_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A18 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct = default) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A19_CT_in_scope_but_is_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A19 { public Task Other(CancellationToken ct) => Task.CompletedTask; public async Task M(IAsyncEnumerable<int> e) { await foreach (int x in e) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_A20_ct_unused_in_the_method_but() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A20 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_A21_await_foreach_over_method_call_returning() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A21 { private IAsyncEnumerable<int> Get() => null!; public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:Get()|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A22_await_foreach_over_IAsyncEnumerable_ConfigureAwait_false_WithCancellation_ct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A22 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in e.ConfigureAwait(false).WithCancellation(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_A24_async_iterator_with_ct_but_no() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A24 { public async IAsyncEnumerable<int> M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in {|FEX0053:e|}) { yield return x; } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_A25_two_foreach_one_with_WithCancellation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A25 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in e.WithCancellation(ct)) { Console.Write(x); } await foreach (int y in {|FEX0053:e|}) { Console.Write(y); } } }
            """);

    [Fact]
    public Task Reports_A27_foreach_over_IAsyncEnumerable_T_returned_by_extension() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class A27Ext { public static IAsyncEnumerable<int> Ext(this int i) => null!; }
            public class A27 { public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:1.Ext()|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_A28_ct_passed_to_the_source_expression() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A28 { private IAsyncEnumerable<int> Get(CancellationToken ct) => null!; public async Task M(CancellationToken ct) { await foreach (int x in Get(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_A29_non_async_iterator_method_Get_ct_without_EnumeratorCancellation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class A29 { private async IAsyncEnumerable<int> Get(CancellationToken ct) { await Task.Yield(); Console.Write(ct); yield return 1; } public async Task M(CancellationToken ct) { await foreach (int x in Get(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_E11_await_foreach_with_cts_Token_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E11 { private IAsyncEnumerable<int> Get(CancellationToken ct) => null!; public async Task M(CancellationTokenSource cts) { await foreach (int x in Get(cts.Token)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_E12_token_on_this_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E12 { private CancellationToken Token => default; public async Task M(IAsyncEnumerable<int> e) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_E13_token_local_declared_after_loop() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E13 { public async Task M(IAsyncEnumerable<int> e) { await foreach (int x in e) { Console.Write(x); } CancellationToken ct = default; Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_N32_custom_ConfigureAwait_taking_a_token() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class N32Ext { public static IAsyncEnumerable<int> ConfigureAwait(this IAsyncEnumerable<int> e, CancellationToken ct) => e; }
            public class N32 { private static IAsyncEnumerable<int> Get() => null!; public async Task M(CancellationToken ct) { await foreach (int x in Get().ConfigureAwait(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_N33_producer_parameter_with_another_attribute() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N33 { private static IAsyncEnumerable<int> Get([System.ComponentModel.Description("d")] int n) => null!; public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:Get(1)|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_AA1_Get_ct_ConfigureAwait_false() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA1 { private IAsyncEnumerable<int> Get(CancellationToken ct) => null!; public async Task M(CancellationToken ct) { await foreach (int x in Get(ct).ConfigureAwait(false)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_AA2_Get_ConfigureAwait_false_no_ct_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA2 { private IAsyncEnumerable<int> Get() => null!; public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:Get().ConfigureAwait(false)|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_AA3_property_source() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA3 { public IAsyncEnumerable<int> Items { get; } = null!; public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:Items|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_AA4_custom_type_implementing_IAsyncEnumerable() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA4Seq : IAsyncEnumerable<int> { public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken ct = default) => null!; }
            public class AA4 { public async Task M(AA4Seq s, CancellationToken ct) { await foreach (int x in s) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_AA5_source_method_with_CT_param_passing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA5 { private IAsyncEnumerable<int> Get(CancellationToken ct) => null!; public async Task M(CancellationToken ct) { await foreach (int x in Get(CancellationToken.None)) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_AA6_CT_from_lambda_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA6 { public Func<CancellationToken, Task> M(IAsyncEnumerable<int> e) => async ct => { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); }; }
            """);

    [Fact]
    public Task Reports_AA7_source_is_extension_taking_CT() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class AA7Ext { public static IAsyncEnumerable<int> Ext(this int i, CancellationToken ct = default) => null!; }
            public class AA7 { public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:1.Ext()|}) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_AA8_source_with_other_param_types_passing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA8 { private IAsyncEnumerable<int> Get(Func<int> f) => null!; public async Task M(CancellationToken ct) { await foreach (int x in {|FEX0053:Get(() => ct.GetHashCode())|}) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_AA9_ValueTask_foreach_over_IAsyncEnumerable_with_await() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA9 { public async Task M(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (var x in e.ConfigureAwait(false).WithCancellation(ct)) { Console.Write(x); } } }
            """);

    [Fact]
    public Task Ignores_AA10_ct_in_scope_is_Nullable_CancellationToken() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA10 { public async Task M(IAsyncEnumerable<int> e, CancellationToken? ct) { await foreach (int x in e) { Console.Write(x); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_AA11_foreach_in_static_local_function_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class AA11 { public async Task M(IAsyncEnumerable<int> e) { static async Task L(IAsyncEnumerable<int> e, CancellationToken ct) { await foreach (int x in {|FEX0053:e|}) { Console.Write(x); } Console.Write(ct); } await L(e, default); } }
            """);

    [Fact]
    public Task Ignores_local_initialized_from_a_call() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            public class A
            {
                private static IAsyncEnumerable<int> Produce(CancellationToken ct) => throw new System.NotSupportedException();
                public async Task M(CancellationToken ct) { var seq = Produce(ct); await foreach (var x in seq) { System.Console.Write(x); } }
            }
            """);

    [Fact]
    public Task Reports_user_ConfigureAwait_without_arguments_without_crashing() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            public class TaskAsyncEnumerableExtensions { public IAsyncEnumerable<int> ConfigureAwait() => throw new System.NotSupportedException(); }
            public class A { public async Task M(CancellationToken ct, TaskAsyncEnumerableExtensions e) { await foreach (var x in {|FEX0053:e.ConfigureAwait()|}) { System.Console.Write(x + ct.GetHashCode()); } } }
            """);
}
