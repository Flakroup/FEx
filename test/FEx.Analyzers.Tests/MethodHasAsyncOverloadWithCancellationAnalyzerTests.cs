using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class MethodHasAsyncOverloadWithCancellationAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<MethodHasAsyncOverloadWithCancellationAnalyzer>(source);

    [Fact]
    public Task Reports_C01_oracle_sync_with_CT_async_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C01 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C02_async_overload_without_CT_MethodHasAsyncOverload_no() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C02 { public void Do() { } public Task DoAsync() => Task.CompletedTask; public async Task M() { Do(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_C03_both_async_overloads_exist_ct_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C03 { public void Do() { } public Task DoAsync() => Task.CompletedTask; public Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C04_non_async_method_with_CT_in_scope() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C04 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public void M(CancellationToken ct) { Do2(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C05_async_no_CT_in_scope_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C05 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M() { Do2(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_C06_async_CT_in_scope_optional_CT() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C06 { public void Do2() { } public Task Do2Async(CancellationToken ct = default) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C07_async_lambda_with_ct_in_outer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C07 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public Func<Task> M(CancellationToken ct) => async () => { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); }; }
            """);

    [Fact]
    public Task Reports_C09_CT_as_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C09 { private CancellationToken _ct; public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M() { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(_ct); } }
            """);

    [Fact]
    public Task Reports_C10_call_on_another_object() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C10Svc { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; }
            public class C10 { public async Task M(C10Svc s, CancellationToken ct) { {|FEX0052:s.Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C11_static_call() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C11 { public static void Do2() { } public static Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C12_value_returning() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C12 { public int Do3() => 1; public Task<int> Do3Async(CancellationToken ct) => Task.FromResult(1); public async Task<int> M(CancellationToken ct) { var x = {|FEX0052:Do3()|}; await Task.Yield(); Console.Write(ct); return x; } }
            """);

    [Fact]
    public Task Reports_C13_ValueTask() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C13 { public void Do2() { } public ValueTask Do2Async(CancellationToken ct) => default; public async Task M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C14_async_overload_returns_void() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C14 { public void Do2() { } public void Do2Async(CancellationToken ct) { } public async Task M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C15_extension_async_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C15Svc { public void Do2() { } }
            public static class C15Ext { public static Task Do2Async(this C15Svc s, CancellationToken ct) => Task.CompletedTask; }
            public class C15 { public async Task M(C15Svc s, CancellationToken ct) { s.Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C16_CT_overload_with_extra_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C16 { public void Do2(int a) { Console.Write(a); } public Task Do2Async(int a, CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2(1)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C17_CT_param_not_last() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C17 { public void Do2(int a) { Console.Write(a); } public Task Do2Async(CancellationToken ct, int a) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2(1)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C18_task_returning_non_async_method_with_ct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C18 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public Task M(CancellationToken ct) { Do2(); return Task.CompletedTask; } }
            """);

    [Fact]
    public Task Reports_C19_async_void() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C19 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async void M(CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C20_BCL_writer_Flush() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C20 { public async Task M(TextWriter w, CancellationToken ct) { {|FEX0052:w.Flush()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C21_BCL_File_ReadAllText() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C21 { public async Task<string> M(string p, CancellationToken ct) { var s = {|FEX0052:File.ReadAllText(p)|}; await Task.Yield(); Console.Write(ct); return s; } }
            """);

    [Fact]
    public Task Reports_C22_BCL_stream_Read() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C22 { public async Task M(Stream st, byte[] buf, CancellationToken ct) { {|FEX0052:st.Read(buf, 0, 1)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C23_BCL_no_CT_param_variant_Console_WriteLine() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C23 { public async Task M(TextWriter w, CancellationToken ct) { w.WriteLine("a"); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C24_sync_call_inside_nested_sync_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C24 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Action a = () => Do2(); a(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C25_CT_is_a_local_variable() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C25 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M() { var ct = CancellationToken.None; {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C26_async_overload_on_interface() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface C26I { void Do2(); Task Do2Async(CancellationToken ct); }
            public class C26 { public async Task M(C26I s, CancellationToken ct) { {|FEX0052:s.Do2()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C28_call_in_async_method_returning_Task_T() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C28 { public int Do3() => 1; public Task<int> Do3Async(CancellationToken ct) => Task.FromResult(1); public async Task<int> M(CancellationToken ct) => {|FEX0052:Do3()|} + await Task.FromResult(ct.GetHashCode()); }
            """);

    [Fact]
    public Task Ignores_C29_CT_overload_where_CT_is_the() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C29 { public void Do2() { } public Task Do2Async(CancellationToken? ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C30_async_name_without_Async_suffix() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C30 { public void Do2() { } public Task Do2Task(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C31_Async_overload_takes_different_params_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C31 { public void Do2(int a) { Console.Write(a); } public Task Do2Async(string a, CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(1); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_C33_call_sits_in_local_function_inside() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C33 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { void L() { Do2(); } L(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_C34_iterator_async_iterator_method_with_ct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class C34 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async IAsyncEnumerable<int> M([EnumeratorCancellation] CancellationToken ct) { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); yield return 1; } }
            """);

    [Fact]
    public Task Ignores_N20_static_async_lambda_in_class_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N20 { private CancellationToken _ct; public void M() { Func<Task> f = static async () => { Do(); await Task.Yield(); }; Console.Write(f); Console.Write(_ct); } private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; }
            """);

    [Fact]
    public Task Ignores_N21_static_local_function_in_async_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N21 { private CancellationToken _ct; private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M() { await L(); static async Task L() { Do(); await Task.Yield(); } Console.Write(_ct); } }
            """);

    [Fact]
    public Task Reports_N22_indexer_of_token_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N22 { public CancellationToken this[int i] => default; private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M() { {|FEX0052:Do()|}; await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_N23_instance_token_property_in_static_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N23 { private CancellationToken Tok => default; private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public static async Task M() { Do(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_N24_static_token_field_in_instance_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N24 { private static readonly CancellationToken Shutdown = default; private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M() { Do(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_N25_outer_class_instance_token_field_seen() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N25 { private CancellationToken _ct; public class In { private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M() { Do(); await Task.Yield(); } } }
            """);

    [Fact]
    public Task Ignores_N26_token_field_inherited_from_base_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N26B { protected CancellationToken Ct; }
            public class N26 : N26B { private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M() { Do(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_N27_generic_sync_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N27 { private static void Do<T>() { } private static Task DoAsync<T>(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do<int>()|}; await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_N28_extension_sync_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class N28Ext { public static void Do(this string s) { } public static Task DoAsync(this string s, CancellationToken ct) => Task.CompletedTask; }
            public class N28 { public async Task M(CancellationToken ct) { {|FEX0052:"x".Do()|}; await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_N29_async_overload_in_base_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N29B { public Task DoAsync(CancellationToken ct) => Task.CompletedTask; }
            public class N29 : N29B { public void Do() { } public async Task M(CancellationToken ct) { Do(); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_N30_generic_async_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N30 { private static void Do() { } private static Task DoAsync<T>(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do()|}; await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_N31_ref_parameter_differs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N31 { private static void Do(ref int a) { } private static Task DoAsync(int a, CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { int a = 1; Do(ref a); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_G3_generic_sync_with_concrete_return_types() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G3 { public int Get<T>() => 1; public Task<int> GetAsync<T>(CancellationToken ct) => Task.FromResult(1); public async Task M(CancellationToken ct) { {|FEX0052:Get<int>()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G4_generic_parameter_and_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G4 { public T Get<T>(T a) => a; public Task<T> GetAsync<T>(T a, CancellationToken ct) => Task.FromResult(a); public async Task M(CancellationToken ct) { Get(1); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_G5_generic_parameter_concrete_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G5 { public int Get<T>(T a) => 1; public Task<int> GetAsync<T>(T a, CancellationToken ct) => Task.FromResult(1); public async Task M(CancellationToken ct) { {|FEX0052:Get(1)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G6_return_types_differ() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G6 { public int Get() => 1; public Task<string> GetAsync(CancellationToken ct) => Task.FromResult(""); public async Task M(CancellationToken ct) { Get(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G7_void_sync_returning_value_task() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G7 { public void Get() { } public Task<int> GetAsync(CancellationToken ct) => Task.FromResult(1); public async Task M(CancellationToken ct) { Get(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G8_value_sync_with_plain_Task() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G8 { public int Get() => 1; public Task GetAsync(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Get(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G9_async_overload_returns_int() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G9 { public void Get() { } public int GetAsync(CancellationToken ct) => 1; public async Task M(CancellationToken ct) { Get(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_G10_generic_sync_with_void_and_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G10 { public void Put<T>(T a) { Console.Write(a); } public Task PutAsync<T>(T a, CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Put(1)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_G11_generic_return_only_differs_in_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G11 { public T Get<T>() => default!; public Task<T> GetAsync<T>(CancellationToken ct) => Task.FromResult(default(T)!); public async Task M(CancellationToken ct) { var x = Get<int>(); await Task.Yield(); Console.Write(x + ct.GetHashCode()); } }
            """);

    [Fact]
    public Task Ignores_G12_statement_call_of_the_generic_returning() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class G12 { public T Get<T>() => default!; public Task<T> GetAsync<T>(CancellationToken ct) => Task.FromResult(default(T)!); public async Task M(CancellationToken ct) { Get<int>(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA1_sync_call_inside_the_async_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA1 { public void Do2() { } public async Task Do2Async(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_CA2_property_CT() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA2 { public CancellationToken Token { get; } public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M() { {|FEX0052:Do2()|}; await Task.Yield(); } }
            """);

    [Fact]
    public Task Ignores_CA3_instance_field_CT_in_static_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA3 { private CancellationToken _ct; public static void Do2() { } public static Task Do2Async(CancellationToken ct) => Task.CompletedTask; public static async Task M() { Do2(); await Task.Yield(); } public CancellationToken G() => _ct; }
            """);

    [Fact]
    public Task Ignores_CA4_static_field_CT() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA4 { private static CancellationToken _ct; public static void Do2() { } public static Task Do2Async(CancellationToken ct) => Task.CompletedTask; public static async Task M() { Do2(); await Task.Yield(); } public static CancellationToken G() => _ct; }
            """);

    [Fact]
    public Task Ignores_CA5_local_declared_after_the_call() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA5 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M() { Do2(); await Task.Yield(); var ct = CancellationToken.None; Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA6_generic_methods() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA6 { public T Get<T>() => default!; public Task<T> GetAsync<T>(CancellationToken ct) => Task.FromResult(default(T)!); public async Task M(CancellationToken ct) { var x = Get<int>(); await Task.Yield(); Console.Write(x + ct.GetHashCode()); } }
            """);

    [Fact]
    public Task Ignores_CA7_private_async_overload_inaccessible_in_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA7B { public void Do2() { } private Task Do2Async(CancellationToken ct) => Task.CompletedTask; }
            public class CA7 : CA7B { public async Task M(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA8_async_overload_defined_in_derived_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA8B { public void Do2() { } }
            public class CA8D : CA8B { public Task Do2Async(CancellationToken ct) => Task.CompletedTask; }
            public class CA8 { public async Task M(CA8D d, CancellationToken ct) { d.Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_CA9_async_lambda_with_ct_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA9 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public Func<CancellationToken, Task> M() => async ct => { {|FEX0052:Do2()|}; await Task.Yield(); Console.Write(ct); }; }
            """);

    [Fact]
    public Task Ignores_CA10_Task_Run_lambda_sync_lambda_inside_async() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA10 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { await Task.Run(() => Do2()); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA11_ref_out_params_sync_vs_async() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA11 { public bool TryDo(out int a) { a = 1; return true; } public Task<bool> TryDoAsync(CancellationToken ct) => Task.FromResult(true); public async Task M(CancellationToken ct) { TryDo(out _); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA12_CT_param_by_ref_kind_none() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA12 { public void Do2() { } public Task Do2Async(in CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_CA13_params_array() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA13 { public void Do2(params int[] a) { Console.Write(a); } public Task Do2Async(int[] a, CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|FEX0052:Do2(1, 2)|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA14_two_CT_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA14 { public void Do2() { } public Task Do2Async(CancellationToken a, CancellationToken b) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_CA15_sync_method_has_CT_param_already() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA15 { public void Do2(CancellationToken ct) { Console.Write(ct); } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { Do2(ct); await Task.Yield(); } }
            """);

    [Fact]
    public Task Reports_CA16_call_inside_using_try_in_async() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class CA16 { public void Do2() { } public Task Do2Async(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { try { {|FEX0052:Do2()|}; } finally { await Task.Yield(); } Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_an_extension_call_with_an_extension_async_twin() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            public static class E { public static int Get(this string s) => 1; public static Task<int> GetAsync(this string s, CancellationToken ct) => Task.FromResult(1); }
            public class C { public async Task M(CancellationToken ct) { {|FEX0052:"a".Get()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_a_void_async_twin_of_a_value_method() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            public class C { public int Get() => 1; public void GetAsync(CancellationToken ct) { } public async Task M(CancellationToken ct) { Get(); await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_a_void_async_twin_of_a_void_method() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            public class C { public void Get() { } public void GetAsync(CancellationToken ct) { } public async Task M(CancellationToken ct) { {|FEX0052:Get()|}; await Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_an_async_twin_returning_a_user_defined_Task() =>
        VerifyAsync("""
            using System;
            using System.Threading;

            namespace Other { public class Task { } public class Task<T> { } }

            public class C { public void Put() { } public Other.Task PutAsync(CancellationToken ct) => new(); public async System.Threading.Tasks.Task M(CancellationToken ct) { Put(); await System.Threading.Tasks.Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_an_async_twin_returning_a_user_defined_generic_Task() =>
        VerifyAsync("""
            using System;
            using System.Threading;

            namespace Other { public class Task { } public class Task<T> { } }

            public class C { public int Get() => 1; public Other.Task<int> GetAsync(CancellationToken ct) => new(); public async System.Threading.Tasks.Task M(CancellationToken ct) { Get(); await System.Threading.Tasks.Task.Yield(); Console.Write(ct); } }
            """);

    [Fact]
    public Task Ignores_call_inside_lock() =>
        VerifyAsync("""
            using System.Threading;
            using System.Threading.Tasks;
            public class L
            {
                private readonly object _g = new();
                private static void Do() { }
                private static Task DoAsync(CancellationToken ct) => Task.CompletedTask;
                public async Task M(CancellationToken ct) { lock (_g) { Do(); } await Task.Yield(); System.Console.Write(ct); }
            }
            """);

    [Fact]
    public Task Reports_static_lambda_with_its_own_token_parameter() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            public class L
            {
                private static void Do() { }
                private static Task DoAsync(CancellationToken ct) => Task.CompletedTask;
                public void M() { Func<CancellationToken, Task> f = static async ct => { {|FEX0052:Do()|}; await Task.Yield(); Console.Write(ct); }; Console.Write(f); }
            }
            """);

    [Fact]
    public Task Ignores_static_lambda_that_would_capture_the_token_of_the_enclosing_method() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            public class L
            {
                private static void Do() { }
                private static Task DoAsync(CancellationToken ct) => Task.CompletedTask;
                public void M(CancellationToken ct) { var local = ct; Func<Task> f = static async () => { Do(); await Task.Yield(); }; Console.Write(f); Console.Write(local); }
            }
            """);
}
