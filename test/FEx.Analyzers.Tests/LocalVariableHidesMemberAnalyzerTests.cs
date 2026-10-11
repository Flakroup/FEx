using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class LocalVariableHidesMemberAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<LocalVariableHidesMemberAnalyzer>(source);

    [Fact]
    public Task Reports_L01_local_hides_field_oracle() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L01 { private int _count; public int M() { int {|FEX0038:_count|} = 1; return _count; } public int G() => _count; }
            """);

    [Fact]
    public Task Ignores_L02_static_method_local_vs_instance_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L02 { private int _count; public static int M() { int _count = 1; return _count; } public int G() => _count; }
            """);

    [Fact]
    public Task Reports_L03_foreach_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L03 { private int item; public void M(int[] a) { foreach (var {|FEX0038:item|} in a) Console.Write(item); } public int G() => item; }
            """);

    [Fact]
    public Task Reports_L04_pattern_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L04 { private int value; public void M(object o) { if (o is int {|FEX0038:value|}) Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L05_out_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L05 { private int value; public void M() { if (int.TryParse("1", out var {|FEX0038:value|})) Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L06_catch_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L06 { private Exception ex = new(); public void M() { try { Console.Write(1); } catch (Exception {|FEX0038:ex|}) { Console.Write(ex); } } public Exception G() => ex; }
            """);

    [Fact]
    public Task Reports_L07_local_in_local_function() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L07 { private int value; public int M() { int L() { int {|FEX0038:value|} = 1; return value; } return L(); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L08_using_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L08 { private IDisposable d = null!; public void M() { using var {|FEX0038:d|} = new System.IO.MemoryStream(); Console.Write(d); } public IDisposable G() => d; }
            """);

    [Fact]
    public Task Reports_L09_deconstruction() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L09 { private int value; public void M() { var (a, {|FEX0038:value|}) = (1, 2); Console.Write(a + value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L10_for_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L10 { private int i; public void M() { for (int {|FEX0038:i|} = 0; i < 3; i++) Console.Write(i); } public int G() => i; }
            """);

    [Fact]
    public Task Reports_L11_local_hides_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L11 { public int Value { get; set; } public void M() { int {|FEX0038:Value|} = 1; Console.Write(Value); } }
            """);

    [Fact]
    public Task Ignores_L12_local_hides_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L12 { public void Run() { } public void M() { int Run = 1; Console.Write(Run); } }
            """);

    [Fact]
    public Task Reports_L13_local_hides_inherited_protected_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L13B { protected int f; }
            public class L13 : L13B { public void M() { int {|FEX0038:f|} = 1; Console.Write(f); } }
            """);

    [Fact]
    public Task Ignores_L14_local_hides_inherited_private_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L14B { private int f; public int G() => f; }
            public class L14 : L14B { public void M() { int f = 1; Console.Write(f); } }
            """);

    [Fact]
    public Task Reports_L15_local_in_lambda_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L15 { private int value; public Func<int> M() => () => { int {|FEX0038:value|} = 1; return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Reports_L16_const_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L16 { private int value; public int M() { const int {|FEX0038:value|} = 1; return value; } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L17_local_in_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L17 { private int value; public L17() { int {|FEX0038:value|} = 1; Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L18_local_in_getter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L18 { private int value; public int P { get { int {|FEX0038:value|} = 1; return value; } } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L19_local_hides_static_field_in_static() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L19 { private static int value; public static int M() { int {|FEX0038:value|} = 1; return value; } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_L20_local_hides_static_field_in_instance() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L20 { private static int value; public int M() { int {|FEX0038:value|} = 1; return value; } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_L21_local_hides_const() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L21 { private const int Max = 1; public int M() { int {|FEX0038:Max|} = 2; return Max; } public int G() => Max; }
            """);

    [Fact]
    public Task Ignores_L22_local_hides_nested_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L22 { private class Inner { } public int M() { int Inner = 2; return Inner; } public object G() => new Inner(); }
            """);

    [Fact]
    public Task Ignores_L23_local_func_name_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L23 { private int value; public int M() { int value() => 1; return value(); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_L24_local_hides_outer_class_field_from() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L24 { private int value; public class In { public int M() { int value = 1; return value; } } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L25_switch_pattern_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L25 { private int value; public int M(object o) => o switch { int {|FEX0038:value|} => value, _ => 0 }; public int G() => value; }
            """);

    [Fact]
    public Task Reports_L26_local_hides_field_in_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public struct L26 { private int value; public int M() { int {|FEX0038:value|} = 1; return value; } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_L27_local_named_like_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L27 { public int M() { int L27 = 1; return L27; } }
            """);

    [Fact]
    public Task Ignores_L28_local_different_case() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L28 { private int value; public int M() { int Value = 1; return Value; } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_L29_query_range_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L29 { private int x; public IEnumerable<int> M(int[] a) => from x in a select x; public int G() => x; }
            """);

    [Fact]
    public Task Reports_L30_local_in_field_init_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L30 { private int value; public Func<int> F = () => { int {|FEX0038:value|} = 1; return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Reports_L31_local_in_static_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L31 { private static int value; static L31() { int {|FEX0038:value|} = 1; Console.Write(value); } public static int G() => value; }
            """);

    [Fact]
    public Task Ignores_L32_local_hides_event() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L32 { public event Action? E; public int M() { int E = 1; return E; } public void R() => E?.Invoke(); }
            """);

    [Fact]
    public Task Reports_L33_local_in_iterator() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L33 { private int value; public IEnumerable<int> M() { int {|FEX0038:value|} = 1; yield return value; } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L34_local_in_async() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L34 { private int value; public async Task<int> M() { int {|FEX0038:value|} = 1; await Task.Yield(); return value; } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L35_local_hides_property_in_record() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public record L35(int X) { public int M() { int {|FEX0038:X|} = 1; return X; } }
            """);

    [Fact]
    public Task Reports_L36_local_hides_base_public_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L36B { public int Value { get; set; } }
            public class L36 : L36B { public int M() { int {|FEX0038:Value|} = 1; return Value; } }
            """);

    [Fact]
    public Task Ignores_L37_local_in_static_local_function_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L37 { private int value; public int M() { static int L() { int value = 1; return value; } return L(); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_L38_out_var_in_lambda_less_statement_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L38 { private int value; public void M() { if (int.TryParse("1", out _)) Console.Write(1); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_L39_local_with_field_also_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L39 { private int value; public int M(int value) { return value; } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L40_foreach_deconstruction() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L40 { private int value; public void M(Dictionary<int, int> d) { foreach (var (k, {|FEX0038:value|}) in d) Console.Write(k + value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L41_is_not_pattern_with_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L41 { private int value; public void M(object o) { if (o is var {|FEX0038:value|}) Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_L42_local_hides_field_in_explicit_interface() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class L42 : IDisposable { private int value; void IDisposable.Dispose() { int {|FEX0038:value|} = 1; Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_M15_event_field_named_like_a_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M15B { protected int P; }
            public class M15 : M15B { public event Action? P; public void Raise() { P?.Invoke(); } }
            """);

    [Fact]
    public Task Reports_LA1_local_in_static_local_function_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA1 { private static int value; public static int M() { static int L() { int {|FEX0038:value|} = 1; return value; } return L(); } public static int G() => value; }
            """);

    [Fact]
    public Task Ignores_LA2_local_in_nested_class_vs_outer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA2 { private static int value; public class In { public int M() { int value = 1; return value; } } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_LA3_foreach_var_in_static_method_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA3 { private static int item; public static void M(int[] a) { foreach (var {|FEX0038:item|} in a) Console.Write(item); } public static int G() => item; }
            """);

    [Fact]
    public Task Ignores_LA4_local_in_lambda_in_static_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA4 { private int value; public Func<int> M() => static () => { int value = 1; return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Reports_LA5_local_in_interface_default_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface LA5 { int value { get; } int M() { int {|FEX0038:value|} = 1; return value; } }
            """);

    [Fact]
    public Task Reports_LA6_local_in_partial_other_part() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class LA6 { private int value; }
            public partial class LA6 { public int M() { int {|FEX0038:value|} = 1; return value; } public int G() => value; }
            """);

    [Fact]
    public Task Reports_LA7_local_hides_generic_base_protected_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA7B<T> { protected T? value; }
            public class LA7 : LA7B<int> { public int M() { int {|FEX0038:value|} = 1; return value; } }
            """);

    [Fact]
    public Task Reports_LA8_local_in_property_initializer_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA8 { private int value; public Func<int> P { get; } = () => { int {|FEX0038:value|} = 1; return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Reports_LA9_local_hides_base_class_property_Exception_Message() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA9 : Exception { public string M() { string {|FEX0038:Message|} = "a"; return Message; } }
            """);

    [Fact]
    public Task Reports_LA10_local_in_async_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA10 { private int value; public Func<Task<int>> M() => async () => { int {|FEX0038:value|} = 1; await Task.Yield(); return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Ignores_LA11_out_var_in_static_method_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class LA11 { private int value; public static void M() { if (int.TryParse("1", out var value)) Console.Write(value); } public int G() => value; }
            """);
}
