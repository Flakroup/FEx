using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class OptionalParameterHierarchyMismatchAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<OptionalParameterHierarchyMismatchAnalyzer>(source);

    [Fact]
    public Task Reports_M06_default_null_versus_text() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M06B { public virtual void M(string? s = null) { Console.Write(s); } }
            public class M06 : M06B { public override void M(string? {|FEX0042:s|} = "a") { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_M07_equal_huge_double_defaults() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M07B { public virtual void M(double d = double.MaxValue) { Console.Write(d); } }
            public class M07 : M07B { public override void M(double d = double.MaxValue) { Console.Write(d); } }
            """);

    [Fact]
    public Task Reports_P01_oracle() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P01B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P01 : P01B { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P02_override_omits_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P02B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P02 : P02B { public override void M(int {|FEX0042:a|}) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P03_override_adds_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P03B { public virtual void M(int a) { Console.Write(a); } }
            public class P03 : P03B { public override void M(int {|FEX0042:a|} = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P04_interface_impl_different_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P04I { void M(int a = 1); }
            public class P04 : P04I { public void M(int a = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P05_interface_impl_omits_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P05I { void M(int a = 1); }
            public class P05 : P05I { public void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P06_interface_impl_adds_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P06I { void M(int a); }
            public class P06 : P06I { public void M(int a = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P07_same_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P07B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P07 : P07B { public override void M(int a = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P08_null_vs_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P08B { public virtual void M(string? s = null) { Console.Write(s); } }
            public class P08 : P08B { public override void M(string? s = default) { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_P09_const_vs_literal_equal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P09B { public const int K = 1; public virtual void M(int a = K) { Console.Write(a); } }
            public class P09 : P09B { public override void M(int a = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P10_three_levels() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P10A { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P10B : P10A { public override void M(int a = 1) { Console.Write(a); } }
            public class P10C : P10B { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P11_base_optional_via_attributes() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P11B { public virtual void M([Optional, DefaultParameterValue(1)] int a) { Console.Write(a); } }
            public class P11 : P11B { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P12_generic_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P12B<T> { public virtual void M(T? a = default, int b = 1) { Console.Write(a + "" + b); } }
            public class P12 : P12B<string> { public override void M(string? a = null, int {|FEX0042:b|} = 2) { Console.Write(a + "" + b); } }
            """);

    [Fact]
    public Task Reports_P13_abstract_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class P13B { public abstract void M(int a = 1); }
            public class P13 : P13B { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P14_new_hiding_with_different_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P14B { public void M(int a = 1) { Console.Write(a); } }
            public class P14 : P14B { public new void M(int a = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P15_interface_implemented_by_base_class_derived() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P15I { void M(int a = 1); }
            public class P15B { public void M(int a = 5) { Console.Write(a); } }
            public class P15 : P15B, P15I { }
            """);

    [Fact]
    public Task Reports_P16_override_of_one_of_two_params() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P16B { public virtual void M(int a = 1, int b = 2) { Console.Write(a + b); } }
            public class P16 : P16B { public override void M(int a = 1, int {|FEX0042:b|} = 3) { Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_P17_explicit_interface_impl_can_t_have_defaults() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P17I { void M(int a = 1); }
            public class P17 : P17I { void P17I.M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P18_enum_default_differs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public enum P18E { A, B }
            public class P18B { public virtual void M(P18E e = P18E.A) { Console.Write(e); } }
            public class P18 : P18B { public override void M(P18E {|FEX0042:e|} = P18E.B) { Console.Write(e); } }
            """);

    [Fact]
    public Task Reports_P19_string_default_differs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P19B { public virtual void M(string s = "a") { Console.Write(s); } }
            public class P19 : P19B { public override void M(string {|FEX0042:s|} = "b") { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_P20_literal_1_vs_1L_ish_long_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P20B { public virtual void M(long a = 1) { Console.Write(a); } }
            public class P20 : P20B { public override void M(long a = 1L) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P21_override_of_a_base_that_omits() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P21I { void M(int a = 1); }
            public class P21B { public virtual void M(int a) { Console.Write(a); } }
            public class P21 : P21B, P21I { public override void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P23_interface_impl_different_default_implemented_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface P23I { void M(int a = 1); }
            public class P23 : P23I { public virtual void M(int a = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P24_indexer_override_once() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P24B { public virtual int this[int i, int j = 1] => i + j; }
            public class P24 : P24B { public override int this[int i, int {|FEX0042:j|} = 2] => i + j; }
            """);

    [Fact]
    public Task Ignores_P25_override_without_defaults_base_without() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P25B { public virtual void M(int a) { Console.Write(a); } }
            public class P25 : P25B { public override void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_P26_base_default_1_vs_override_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P26B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P26 : P26B { public override void M(int a = 0 + 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_P27_second_override_level_omits_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class P27B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class P27 : P27B { public override void M(int a = 1) { Console.Write(a); } }
            public class P27C : P27 { public override void M(int {|FEX0042:a|}) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_PA1_A_1_B_2_C_2() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA1A { public virtual void M(int a = 1) { Console.Write(a); } }
            public class PA1B : PA1A { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            public class PA1C : PA1B { public override void M(int a = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_PA2_A_1_B_2_C_1() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA2A { public virtual void M(int a = 1) { Console.Write(a); } }
            public class PA2B : PA2A { public override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            public class PA2C : PA2B { public override void M(int {|FEX0042:a|} = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_PA4_sealed_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA4B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class PA4 : PA4B { public sealed override void M(int {|FEX0042:a|} = 2) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_PA5_property_indexer_without_defaults_ok_override_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA5B<T> { public virtual void M(T a, int b = 1) { Console.Write(a + "" + b); } }
            public class PA5 : PA5B<int> { public override void M(int a, int b = 1) { Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_PA6_default_int_vs_0() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA6B { public virtual void M(int a = 0) { Console.Write(a); } }
            public class PA6 : PA6B { public override void M(int a = default) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_PA7_null_vs_omitted_on_reference_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA7B { public virtual void M(string? s = null) { Console.Write(s); } }
            public class PA7 : PA7B { public override void M(string? {|FEX0042:s|}) { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_PA8_default_struct_vs_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA8B { public virtual void M(CancellationToken ct = default) { Console.Write(ct); } }
            public class PA8 : PA8B { public override void M(CancellationToken ct = default(CancellationToken)) { Console.Write(ct); } }
            """);

    [Fact]
    public Task Reports_PA9_override_with_params_and_defaults_none() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class PA9B { public virtual void M(int a = 1, params int[] rest) { Console.Write(a + rest.Length); } }
            public class PA9 : PA9B { public override void M(int {|FEX0042:a|} = 2, params int[] rest) { Console.Write(a + rest.Length); } }
            """);

    [Fact]
    public Task Ignores_NaN_default_equal_to_NaN() =>
        VerifyAsync("""
            public class B { public virtual void M(double d = double.NaN, float f = float.NaN) { System.Console.Write(d + f); } }
            public class D : B { public override void M(double d = double.NaN, float f = float.NaN) { System.Console.Write(d + f); } }
            """);

    [Fact]
    public Task Ignores_base_parameter_typed_as_type_parameter() =>
        VerifyAsync("""
            #nullable enable
            public class B<T> { public virtual void M(T t = default!) { System.Console.Write(t); } public virtual void N(T t = default!) { System.Console.Write(t); } }
            public class D : B<int> { public override void M(int t = 0) { System.Console.Write(t); } public override void N(int t = default) { System.Console.Write(t); } }
            """);

    [Fact]
    public Task Reports_long_defaults_that_differ_above_two_to_the_53() =>
        VerifyAsync("""
            public class B { public virtual void M(long x = 9007199254740993L) { System.Console.Write(x); } }
            public class D : B { public override void M(long {|FEX0042:x|} = 9007199254740992L) { System.Console.Write(x); } }
            """);
}
