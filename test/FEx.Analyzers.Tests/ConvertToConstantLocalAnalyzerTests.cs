using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class ConvertToConstantLocalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<ConvertToConstantLocalAnalyzer>(source);

    [Fact]
    public Task Ignores_N10_nullable_int_field_read_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N10 { private int? _n = 5; public int? G() => _n; }
            """);

    [Fact]
    public Task Reports_N11_default_int_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N11 { private int {|FEX0051:_n|} = default(int); public int G() => _n; }
            """);

    [Fact]
    public Task Reports_N12_default_literal_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N12 { private int {|FEX0051:_n|} = default; public int G() => _n; }
            """);

    [Fact]
    public Task Reports_N13_default_int_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N13 { public void M() { int {|FEX0051:n|} = default(int); Console.Write(n); } }
            """);

    [Fact]
    public Task Reports_N14_constant_interpolated_string_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N14 { public void M() { string {|FEX0051:s|} = $"abc"; Console.Write(s); } }
            """);

    [Fact]
    public Task Reports_N15_constant_interpolated_string_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N15 { private string {|FEX0051:_s|} = $"abc"; public string G() => _s; }
            """);

    [Fact]
    public Task Ignores_N16_public_field_with_constant_initializer_read() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N16 { public int F = 1; public int G() => F; }
            """);

    [Fact]
    public Task Ignores_N17_ref_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N17 { private int _f; public void M() { ref int r = ref _f; Console.Write(r); } }
            """);

    [Fact]
    public Task Ignores_N18_nameof_of_a_public_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N18 { public int F = 1; public string G() => nameof(F); }
            """);

    [Fact]
    public Task Reports_N38_string_default_expression_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N38 { public void M() { string {|FEX0051:s|} = default(string); Console.Write(s); } }
            """);

    [Fact]
    public Task Reports_N40_default_literal_string_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N40 { public void M() { string? {|FEX0051:s|} = default; Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_KA1_readonly_field_assigned_in_ctor_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA1 { private readonly int _f = 5; public KA1() { _f = 6; } public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA2_private_static_readonly_int() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA2 { private static readonly int {|FEX0051:_f|} = 5; public static int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA3_protected_in_sealed_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class KA3 { protected int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_KA4_private_protected() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA4 { private protected int _f = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA5_internal_in_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA5 { private class In { internal int {|FEX0051:_f|} = 5; } public int G() => new In()._f; }
            """);

    [Fact]
    public Task Ignores_KA6_internal_in_internal_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            internal class KA6 { internal int _f = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_KA7_field_written_via_other_instance() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA7 { private int _f = 5; public static void S(KA7 o) { o._f = 1; } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_KA8_field_with_in_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA8 { private int _f = 5; private static void R(in int a) { Console.Write(a); } public void S() { R(in _f); } }
            """);

    [Fact]
    public Task Ignores_KA9_field_address_of() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public unsafe class KA9 { private int _f = 5; public int* S() { fixed (int* p = &_f) { return p; } } }
            """);

    [Fact]
    public Task Ignores_KA10_field_alias_ref_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA10 { private int _f = 5; public void S() { ref int r = ref _f; r = 2; } }
            """);

    [Fact]
    public Task Ignores_KA11_field_return_by_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA11 { private int _f = 5; public ref int S() => ref _f; }
            """);

    [Fact]
    public Task Ignores_KA12_field_object_initializer_elsewhere() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA12 { private int _f = 5; public static KA12 Make() => new KA12 { _f = 3 }; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA13_field_with_unary_minus_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA13 { private int {|FEX0051:_f|} = -1; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA14_field_double() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA14 { private double {|FEX0051:_f|} = 1.5; public double G() => _f; }
            """);

    [Fact]
    public Task Reports_KA15_field_uint_ulong_ushort_sbyte() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA15 { private ushort {|FEX0051:_f|} = 1; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA16_local_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA16 { public Func<int> M() => () => { int {|FEX0051:x|} = 5; return x; }; }
            """);

    [Fact]
    public Task Ignores_KA17_local_referencing_via_in_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA17 { private static void R(in int a) { Console.Write(a); } public void M() { int x = 5; R(in x); } }
            """);

    // ReSharper reports nint, but a nint cannot be a const in C#, so this analyzer stays silent.
    [Fact]
    public Task Ignores_KA18_local_of_nint() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA18 { public nint M() { nint x = 5; return x; } }
            """);

    [Fact]
    public Task Reports_KA19_local_with_explicit_cast_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA19 { public int M() { int {|FEX0051:x|} = (int)1.5; return x; } }
            """);

    [Fact]
    public Task Reports_KA20_local_init_from_other_readonly_not() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA20 { private static readonly int {|FEX0051:S|} = 5; public int M() { int x = S; return x; } }
            """);

    [Fact]
    public Task Reports_KA21_field_with_initializer_from_const_expression() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA21 { private const int C = 2; private int {|FEX0051:_f|} = C * 2; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA22_static_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA22 { public int M() { static int L() { int {|FEX0051:x|} = 5; return x; } return L(); } }
            """);

    [Fact]
    public Task Reports_KA24_field_in_a_record() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public record KA24 { private int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA25_field_with_default_for_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA25 { private string? {|FEX0051:_f|} = default; public string? G() => _f; }
            """);

    [Fact]
    public Task Reports_KA27_two_fields_in_one_declaration() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA27 { private int {|FEX0051:_a|} = 1, {|FEX0051:_b|} = 2; public int G() => _a + _b; }
            """);

    [Fact]
    public Task Reports_KA28_private_field_read_only_in_nameof() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA28 { private int {|FEX0051:_f|} = 5; public int P => _f; }
            """);

    [Fact]
    public Task Ignores_KA29_local_used_only_inside_nameof_and() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA29 { public string M() { int x = 5; return nameof(x); } }
            """);

    [Fact]
    public Task Ignores_KA30_local_for_loop_declaration_flagged() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA30 { public void M() { for (int i = 5, j = 6; ; ) { Console.Write(i + j); } } }
            """);

    [Fact]
    public Task Reports_KA33_field_in_static_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class KA33 { private static int {|FEX0051:_f|} = 5; public static int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA35_local_passed_to_nameof_only_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA35 { public string M() { int {|FEX0051:x|} = 5; return nameof(x) + x; } }
            """);

    [Fact]
    public Task Reports_KA36_field_of_const_able_type_but_readonly() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA36 { private static readonly int {|FEX0051:_f|} = 5; static KA36() { Console.Write(1); } public static int G() => _f; }
            """);

    [Fact]
    public Task Reports_KA38_local_string_with_escapes() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA38 { public string M() { string {|FEX0051:s|} = "a\nb"; return s; } }
            """);

    [Fact]
    public Task Reports_KA39_local_char_arithmetic() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA39 { public int M() { int {|FEX0051:x|} = 'a' + 1; return x; } }
            """);

    [Fact]
    public Task Reports_KA40_local_ternary_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class KA40 { public int M() { int {|FEX0051:x|} = true ? 1 : 2; return x; } }
            """);

    [Fact]
    public Task Ignores_NC1_nameof_plus_genuine_read_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC1 { private int _f = 5; public string G() => nameof(_f); public Func<int> H() => () => _f; }
            """);

    [Fact]
    public Task Reports_NC2_enum_local_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC2 { public DayOfWeek M() { DayOfWeek {|FEX0051:d|} = default; return d; } }
            """);

    [Fact]
    public Task Reports_NC3_string_local_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC3 { public string? M() { string? {|FEX0051:s|} = default; return s; } }
            """);

    [Fact]
    public Task Ignores_NC4_string_field_null_literal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC4 { private string? _f = null; public string? G() => _f; }
            """);

    [Fact]
    public Task Ignores_NC5_protected_internal_in_sealed_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class NC5 { protected internal int F = 5; public int G() => F; }
            """);

    [Fact]
    public Task Reports_NC6_private_protected_in_sealed_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class NC6 { private protected int {|FEX0051:F|} = 5; public int G() => F; }
            """);

    [Fact]
    public Task Ignores_NC7_public_in_internal_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            internal class NC7 { public int F = 5; public int G() => F; }
            """);

    [Fact]
    public Task Ignores_NC8_private_field_read_in_other_nested() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC8 { private int _f = 5; public class In { public void S(NC8 o) { o._f = 2; } } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_NC9_field_Interlocked_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC9 { private int _f = 5; public void S() { Interlocked.Increment(ref _f); } public int G() => _f; }
            """);

    [Fact]
    public Task Reports_NC10_field_read_from_another_field_initializer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC10 { private int {|FEX0051:_a|} = 5; private int {|FEX0051:_b|} = 1; public int B => _b; public int G() => _a + 1; }
            """);

    [Fact]
    public Task Reports_NC11_local_from_char_cast() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC11 { public char M() { char {|FEX0051:c|} = (char)65; return c; } }
            """);

    [Fact]
    public Task Reports_NC12_local_declared_in_finally_and_catch() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC12 { public int M() { try { return 1; } catch (Exception) { int {|FEX0051:x|} = 5; return x; } finally { int {|FEX0051:y|} = 6; Console.Write(y); } } }
            """);

    [Fact]
    public Task Reports_NC13_local_in_switch_expression_arm_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC13 { public int M(int i) => i switch { 1 => new Func<int>(() => { int {|FEX0051:x|} = 5; return x; })(), _ => 0 }; }
            """);

    [Fact]
    public Task Reports_NC14_field_in_partial_class_read_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class NC14 { private int {|FEX0051:_f|} = 5; }
            public partial class NC14 { public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_NC15_field_in_partial_class_written_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class NC15 { private int _f = 5; }
            public partial class NC15 { public void S() { _f = 1; } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_NC16_local_deconstruct_foreach_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC16 { public int M((int, int)[] a) { int s = 0; foreach (var (x, y) in a) s += x + y; return s; } }
            """);

    [Fact]
    public Task Reports_NC17_private_const_like_static_readonly_string_from() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC17 { private static readonly string {|FEX0051:S|} = "abc"; public string G() => S; }
            """);

    [Fact]
    public Task Ignores_NC19_local_ref_aliasing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC19 { public int M() { int x = 5; ref int r = ref x; r = 6; return x; } }
            """);

    [Fact]
    public Task Ignores_NC20_local_readonly_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC20 { public int M() { int x = 5; ref readonly int r = ref x; return r; } }
            """);

    [Fact]
    public Task Reports_NC21_field_only_read_in_expression_tree() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC21 { private int {|FEX0051:_f|} = 5; public int G(int i) => i switch { 1 => _f, _ => 0 }; }
            """);

    [Fact]
    public Task Reports_NC22_local_used_only_in_interpolated_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC22 { public string M() { int {|FEX0051:x|} = 5; return $"v{x}"; } }
            """);

    [Fact]
    public Task Reports_NC23_local_used_in_a_using_declaration() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC23 { public int M() { int {|FEX0051:x|} = 5; using var s = new MemoryStream(); return (int)s.Length + x; } }
            """);

    [Fact]
    public Task Reports_NC24_local_same_name_as_field_initial() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC24 { public int M() { int {|FEX0051:x|} = 5; { int y = x; return y; } } }
            """);

    [Fact]
    public Task Reports_NC25_private_field_with_property_backing_assigned() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class NC25 { private int {|FEX0051:_f|} = 5; public int A => _f; public int B => _f; }
            """);

    [Fact]
    public Task Reports_K01_oracle_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K01 { public int M() { int {|FEX0051:x|} = 5; return x + 1; } }
            """);

    [Fact]
    public Task Ignores_K02_assigned_later() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K02 { public int M() { int x = 5; x = 6; return x; } }
            """);

    [Fact]
    public Task Reports_K03_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K03 { public string M() { string {|FEX0051:s|} = "a"; return s + "b"; } }
            """);

    [Fact]
    public Task Ignores_K04_non_const_init() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K04 { public object M() { object o = new object(); return o; } }
            """);

    [Fact]
    public Task Ignores_K05_passed_by_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K05 { private static void R(ref int a) { a++; } public int M() { int x = 5; R(ref x); return x; } }
            """);

    [Fact]
    public Task Ignores_K06_increment() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K06 { public int M() { int x = 5; x++; return x; } }
            """);

    [Fact]
    public Task Reports_K07_captured_read_only_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K07 { public Func<int> M() { int {|FEX0051:x|} = 5; return () => x; } }
            """);

    [Fact]
    public Task Reports_K08_multi_declarators() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K08 { public int M() { int {|FEX0051:a|} = 1, {|FEX0051:b|} = 2; return a + b; } }
            """);

    [Fact]
    public Task Reports_K09_var() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K09 { public int M() { var {|FEX0051:x|} = 5; return x; } }
            """);

    [Fact]
    public Task Ignores_K10_never_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K10 { public void M() { int x = 5; } }
            """);

    [Fact]
    public Task Reports_K11_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K11 { public int M() { int {|FEX0051:x|} = default; return x; } }
            """);

    [Fact]
    public Task Ignores_K12_null_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K12 { public string? M() { string? s = null; return s; } }
            """);

    [Fact]
    public Task Ignores_K13_nullable_int() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K13 { public int? M() { int? x = 5; return x; } }
            """);

    [Fact]
    public Task Reports_K14_decimal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K14 { public decimal M() { decimal {|FEX0051:d|} = 1.5m; return d * 2; } }
            """);

    [Fact]
    public Task Reports_K15_enum() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K15 { public DayOfWeek M() { DayOfWeek {|FEX0051:d|} = DayOfWeek.Monday; return d; } }
            """);

    [Fact]
    public Task Ignores_K16_object_with_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K16 { public object M() { object o = "x"; return o; } }
            """);

    [Fact]
    public Task Ignores_K17_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K17 { public DateTime M() { DateTime d = DateTime.MinValue; return d; } }
            """);

    [Fact]
    public Task Reports_K18_in_loop_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K18 { public void M() { for (var i = 0; i < 3; i++) { int {|FEX0051:x|} = 5; Console.Write(x + i); } } }
            """);

    [Fact]
    public Task Reports_K19_initialised_from_const_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K19 { public int M() { const int a = 1; int {|FEX0051:y|} = a; return y + 1; } }
            """);

    [Fact]
    public Task Reports_K20_from_const_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K20 { private const int Max = 3; public int M() { int {|FEX0051:y|} = Max; return y + 1; } }
            """);

    [Fact]
    public Task Reports_K21_private_non_readonly_field_read_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K21 { private int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K22_private_readonly_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K22 { private readonly int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K23_private_field_written_in_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K23 { private int _f = 5; public K23() { _f = 6; } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K24_private_field_written_in_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K24 { private int _f = 5; public void S() { _f = 6; } public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K25_private_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K25 { private static int {|FEX0051:_f|} = 5; public static int G() => _f; }
            """);

    [Fact]
    public Task Reports_K26_private_string_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K26 { private string {|FEX0051:_f|} = "a"; public string G() => _f; }
            """);

    [Fact]
    public Task Ignores_K27_no_initializer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K27 { private int _f; public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K28_field_ref_passed() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K28 { private int _f = 5; private static void R(ref int a) { a++; } public void S() { R(ref _f); } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K29_private_field_never_read() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K29 { private int _f = 5; }
            """);

    [Fact]
    public Task Ignores_K30_internal_readonly() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K30 { internal readonly int F = 5; }
            """);

    [Fact]
    public Task Ignores_K31_protected_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K31 { protected int F = 5; }
            """);

    [Fact]
    public Task Ignores_K32_private_field_in_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public struct K32 { private int _f = 5; public K32() { } public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K33_public_field_in_private_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K33 { private class In { public int {|FEX0051:F|} = 5; } public int G() => new In().F; }
            """);

    [Fact]
    public Task Reports_K34_volatile() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K34 { private volatile int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K35_thread_static() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K35 { [ThreadStatic] private static int {|FEX0051:_f|} = 5; public static int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K37_local_out() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K37 { private static void R(out int a) { a = 1; } public int M() { int x = 5; R(out x); return x; } }
            """);

    [Fact]
    public Task Reports_K39_local_arithmetic_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K39 { public int M() { int {|FEX0051:x|} = 1 + 2; return x; } }
            """);

    [Fact]
    public Task Ignores_K40_local_interpolated_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K40 { public string M() { string s = $"abc{1}"; return s; } }
            """);

    [Fact]
    public Task Reports_K41_local_bool() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K41 { public bool M() { bool {|FEX0051:c|} = true; return c; } }
            """);

    [Fact]
    public Task Reports_K42_char_bool_fields_private() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K42 { private char {|FEX0051:_c|} = 'a'; private bool {|FEX0051:_b|} = true; public string G() => _c + "" + _b; }
            """);

    [Fact]
    public Task Reports_K47_long_from_int_literal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K47 { public long M() { long {|FEX0051:x|} = 5; return x; } }
            """);

    [Fact]
    public Task Ignores_K48_modified_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K48 { public Action M() { int x = 5; return () => x++; } }
            """);

    [Fact]
    public Task Reports_K49_readonly_string_concat_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K49 { private readonly string {|FEX0051:_s|} = "a" + "b"; public string G() => _s; }
            """);

    [Fact]
    public Task Ignores_K51_for_init_never_modified() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K51 { public void M() { for (int i = 5; ; ) { Console.Write(i); } } }
            """);

    [Fact]
    public Task Reports_K52_local_nameof_sizeof() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K52 { public string M() { string {|FEX0051:s|} = nameof(K52); return s; } public int N() { int {|FEX0051:n|} = sizeof(int); return n; } }
            """);

    [Fact]
    public Task Reports_K53_readonly_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K53 { private readonly int {|FEX0051:_f|} = default; public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K54_field_assigned_via_out_in_other() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K54 { private int _f = 5; private static void R(out int a) { a = 1; } public void S() { R(out _f); } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K55_field_compound_assign() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K55 { private int _f = 5; public void S() { _f += 1; } public int G() => _f; }
            """);

    [Fact]
    public Task Ignores_K56_field_used_in_nameof_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K56 { private int _f = 5; public string G() => nameof(_f); }
            """);

    [Fact]
    public Task Reports_K57_local_const_able_but_declared_in_switch() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K57 { public int M(int i) { switch (i) { case 1: int {|FEX0051:x|} = 5; return x; default: return 0; } } }
            """);

    [Fact]
    public Task Ignores_K59_local_with_string_Empty_const_no() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K59 { public string M() { string s = string.Empty; return s; } }
            """);

    [Fact]
    public Task Reports_K60_local_double() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K60 { public double M() { double {|FEX0051:d|} = 1.5; return d * 2; } }
            """);

    [Fact]
    public Task Reports_K61_local_byte_from_literal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K61 { public byte M() { byte {|FEX0051:b|} = 1; return b; } }
            """);

    [Fact]
    public Task Reports_K62_local_with_uint_ulong() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K62 { public ulong M() { ulong {|FEX0051:b|} = 1UL; return b; } }
            """);

    [Fact]
    public Task Reports_K63_local_int_Max_from_int_MaxValue() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K63 { public int M() { int {|FEX0051:x|} = int.MaxValue; return x; } }
            """);

    [Fact]
    public Task Ignores_K64_local_used_in_unsafe_address() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public unsafe class K64 { public int M() { int x = 5; int* p = &x; return *p; } }
            """);

    [Fact]
    public Task Reports_K65_field_used_with_ref_in_nested() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K65 { private int {|FEX0051:_f|} = 5; public class In { public void M(K65 k) { Console.Write(k._f); } } }
            """);

    [Fact]
    public Task Reports_K66_private_field_in_generic_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K66<T> { private int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K67_private_field_with_attribute() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K67 { [Obsolete] private int {|FEX0051:_f|} = 5; public int G() => _f; }
            """);

    [Fact]
    public Task Reports_K68_private_field_of_enum_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K68 { private DayOfWeek {|FEX0051:_f|} = DayOfWeek.Monday; public DayOfWeek G() => _f; }
            """);

    [Fact]
    public Task Reports_K69_local_in_lambda_used_in_closure() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K69 { public async Task<int> M() { int {|FEX0051:x|} = 5; await Task.Yield(); return x; } }
            """);

    [Fact]
    public Task Ignores_K72_field_assigned_only_by_initializer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K72 { private int _f = 5; public int G() => _f; public static K72 Make() => new K72 { _f = 3 }; }
            """);

    [Fact]
    public Task Reports_K73_local_int_x_5_inside() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K73 { public int M() { int L() { int {|FEX0051:x|} = 5; return x; } return L(); } }
            """);

    [Fact]
    public Task Reports_K74_nullable_string_with_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K74 { public string? M() { string? {|FEX0051:s|} = "a"; return s; } }
            """);

    [Fact]
    public Task Ignores_K75_local_assigned_in_different_branch() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K75 { public int M(bool b) { int x = 5; if (b) { x = 2; } return x; } }
            """);

    [Fact]
    public Task Ignores_K76_local_declared_and_shadowed_by_deconstruction() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K76 { public int M() { int x = 5; int y; (x, y) = (1, 2); return x + y; } }
            """);

    [Fact]
    public Task Reports_K77_local_typed_char() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K77 { public char M() { char {|FEX0051:c|} = 'x'; return c; } }
            """);

    [Fact]
    public Task Reports_K78_local_float() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K78 { public float M() { float {|FEX0051:f|} = 1.5f; return f; } }
            """);

    [Fact]
    public Task Ignores_K79_field_typeof_not_const() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K79 { private Type _t = typeof(int); public Type G() => _t; }
            """);

    [Fact]
    public Task Ignores_K80_local_int_not_const() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K80 { public int[] M() { int[] a = null!; return a; } }
            """);

    [Fact]
    public Task Reports_K81_local_used_in_typeof_local_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class K81 { public bool M(int i) { int {|FEX0051:x|} = 5; return i is 5 && x == 5; } }
            """);

    [Fact]
    public Task Ignores_a_nullable_field_with_a_constant_initializer() =>
        VerifyAsync("""
            public class C { private int? n = 5; public int M() => n ?? 0; }
            """);

    [Fact]
    public Task Ignores_an_object_field_with_a_constant_initializer() =>
        VerifyAsync("""
            public class C { private object o = "s"; public object M() => o; }
            """);

    [Fact]
    public Task Ignores_an_auto_property_with_an_initializer() =>
        VerifyAsync("""
            public class C { private int P { get; } = 5; public int M() => P; }
            """);

    // The write helper's three disputed cases, decided once: an address may be written through, a ref alias may be written through, an in argument cannot be.
    [Fact]
    public Task Ignores_a_local_whose_address_is_taken() =>
        VerifyAsync("""
            public unsafe class C { public int M() { int x = 5; int* p = &x; return *p; } }
            """);

    [Fact]
    public Task Ignores_a_local_aliased_by_a_ref_local() =>
        VerifyAsync("""
            public class C { public int M() { int x = 5; ref int r = ref x; r = 6; return x; } }
            """);

    [Fact]
    public Task Reports_a_local_passed_to_an_in_parameter_without_the_keyword() =>
        VerifyAsync("""
            public class C { private static int Read(in int a) => a; public int M() { int {|FEX0051:x|} = 5; return Read(x); } }
            """);

    [Fact]
    public Task Ignores_a_local_passed_with_the_in_keyword() =>
        VerifyAsync("""
            public class C { private static int Read(in int a) => a; public int M() { int x = 5; return Read(in x); } }
            """);

    [Fact]
    public Task Ignores_a_ref_local_over_a_constant() =>
        VerifyAsync("""
            public class C { public int M() { var y = 1; ref var x = ref y; return x; } }
            """);

    [Fact]
    public Task Ignores_a_nullable_field_set_to_default() =>
        VerifyAsync("""
            public class C { private int? n = default; public int M() => n ?? 0; }
            """);

    [Fact]
    public Task Ignores_local_whose_constant_changes_overload_resolution() =>
        VerifyAsync("""
            using System;
            public class K
            {
                private static void F(object o) { }
                private static void F(DayOfWeek d) { }
                private static void G(long l) { }
                private static void G(byte b) { }
                public void M() { int zero = 0; F(zero); int one = 1; G(one); }
            }
            """);

    [Fact]
    public Task Ignores_field_whose_constant_changes_overload_resolution() =>
        VerifyAsync("""
            using System;
            public class K
            {
                private int _zero = 0;
                private static void F(object o) { }
                private static void F(DayOfWeek d) { }
                public void M() => F(_zero);
            }
            """);

    [Fact]
    public Task Reports_local_whose_constant_keeps_the_overload() =>
        VerifyAsync("""
            public class K
            {
                private static void F(object o) { }
                private static void F(long d) { }
                public void M() { int {|FEX0051:size|} = 5; F(size); }
            }
            """);
}
