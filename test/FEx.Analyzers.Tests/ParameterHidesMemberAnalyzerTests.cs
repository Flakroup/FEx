using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class ParameterHidesMemberAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<ParameterHidesMemberAnalyzer>(source);

    [Fact]
    public Task Ignores_H01_ctor_param_same_as_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H01 { private int value; public H01(int value) { this.value = value; } }
            """);

    [Fact]
    public Task Reports_H02_method_param_same_as_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H02 { private int value; public void M(int {|FEX0037:value|}) { this.value = value; } }
            """);

    [Fact]
    public Task Ignores_H03_static_method_instance_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H03 { private int value; public static void M(int value) { Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H04_static_method_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H04 { private static int value; public static void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Reports_H05_hides_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H05 { public int Value { get; set; } public void M(int {|FEX0037:Value|}) { Console.Write(Value); } }
            """);

    [Fact]
    public Task Ignores_H06_hides_method_name() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H06 { public void Run() { } public void M(int Run) { Console.Write(Run); } }
            """);

    [Fact]
    public Task Reports_H07_inherited_protected_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H07B { protected int f; }
            public class H07 : H07B { public void M(int {|FEX0037:f|}) { Console.Write(f); } }
            """);

    [Fact]
    public Task Ignores_H08_inherited_private_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H08B { private int f; public int G() => f; }
            public class H08 : H08B { public void M(int f) { Console.Write(f); } }
            """);

    [Fact]
    public Task Reports_H09_lambda_parameter_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H09 { private int value; public Func<int, int> M() => {|FEX0037:value|} => value; public int G() => value; }
            """);

    [Fact]
    public Task Reports_H10_local_function_param_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H10 { private int value; public int M() { int L(int {|FEX0037:value|}) => value; return L(1); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H11_indexer_param_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H11 { private int value; public int this[int {|FEX0037:value|}] => value; public int G() => value; }
            """);

    [Fact]
    public Task Reports_H12_record_method_param_hides_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public record H12(int X) { public void M(int {|FEX0037:X|}) { Console.Write(X); } }
            """);

    [Fact]
    public Task Reports_H13_hides_const() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H13 { private const int Max = 1; public void M(int {|FEX0037:Max|}) { Console.Write(Max); } public int G() => Max; }
            """);

    [Fact]
    public Task Ignores_H14_hides_nested_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H14 { private class Inner { } public void M(int Inner) { Console.Write(Inner); } public object G() => new Inner(); }
            """);

    [Fact]
    public Task Ignores_H15_hides_event() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H15 { public event Action? E; public void M(int E) { Console.Write(E); } public void Raise() => E?.Invoke(); }
            """);

    [Fact]
    public Task Ignores_H16_different_casing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H16 { private int value; public void M(int Value) { Console.Write(Value); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H17_interface_method_param_same_as_interface() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface H17 { int value { get; } void M(int value); }
            """);

    [Fact]
    public Task Ignores_H18_abstract_method_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class H18 { protected int value; public abstract void M(int value); }
            """);

    [Fact]
    public Task Reports_H19_override_param_same_as_base_member() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H19B { protected int value; public virtual void M(int a) { Console.Write(a); } }
            public class H19 : H19B { public override void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Ignores_H20_operator_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H20 { private int value; public static H20 operator +(H20 a, int value) { Console.Write(value); return a; } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H21_static_local_function_param_instance_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H21 { private int value; public int M() { static int L(int value) => value; return L(1); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H22_struct_ctor_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public struct H22 { private int value; public H22(int value) { this.value = value; } }
            """);

    [Fact]
    public Task Ignores_H23_generic_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H23<T> { private T value; public H23(T value) { this.value = value; } }
            """);

    [Fact]
    public Task Ignores_H24_nested_class_method_param_hides_OUTER() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H24 { private int value; public class In { public void M(int value) { Console.Write(value); } } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H25_hides_static_field_from_instance_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H25 { private static int value; public void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Reports_H26_explicit_interface_impl_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface H26I { void M(int value); }
            public class H26 : H26I { private int value; void H26I.M(int {|FEX0037:value|}) { Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H27_anonymous_method_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H27 { private int value; public Func<int, int> M() => delegate (int {|FEX0037:value|}) { return value; }; public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H28_delegate_type_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H28 { private int value; public delegate void D(int value); public int G() => value; }
            """);

    [Fact]
    public Task Reports_H29_extension_method_param_vs_static_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public static class H29 { private static int value; public static void M(this int a, int {|FEX0037:value|}) { Console.Write(value); } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_H30_primary_ctor_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H30(int x) { private int x2 = x; public void M(int {|FEX0037:x2|}) { Console.Write(x2); } }
            """);

    [Fact]
    public Task Ignores_H31_classic_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H31 { private readonly string name; public H31(string name) { this.name = name; } public string Name => name; }
            """);

    [Fact]
    public Task Ignores_H33_local_func_param_hides_outer_local() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H33 { public int M() { int a = 1; int L(int a) => a; return L(a); } }
            """);

    [Fact]
    public Task Ignores_H34_property_setter_value_and_a_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H34 { private int value; public int P { get => value; set => this.value = value; } }
            """);

    [Fact]
    public Task Ignores_H36_param_named_same_as_type_itself() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H36 { public void M(int H36) { Console.Write(H36); } }
            """);

    [Fact]
    public Task Ignores_H37_param_hides_nested_enum() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H37 { public enum E { A } public void M(int E) { Console.Write(E); } }
            """);

    [Fact]
    public Task Reports_H38_protected_internal_inherited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H38B { protected internal int f; }
            public class H38 : H38B { public void M(int {|FEX0037:f|}) { Console.Write(f); } }
            """);

    [Fact]
    public Task Reports_H39_internal_inherited_field_same_assembly() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H39B { internal int f; }
            public class H39 : H39B { public void M(int {|FEX0037:f|}) { Console.Write(f); } }
            """);

    [Fact]
    public Task Ignores_H41_ctor_param_hides_property_assigned_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H41 { public int Value { get; } public H41(int Value) { this.Value = Value; } }
            """);

    [Fact]
    public Task Reports_H42_params_array_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H42 { private int[] args = []; public void M(params int[] {|FEX0037:args|}) { Console.Write(args); } public int[] G() => args; }
            """);

    [Fact]
    public Task Reports_H48_hides_base_public_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H48B { public int Value { get; set; } }
            public class H48 : H48B { public void M(int {|FEX0037:Value|}) { Console.Write(Value); } }
            """);

    [Fact]
    public Task Reports_H49_interface_impl_param_hides_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface H26I { void M(int value); }
            public class H49 : H26I { private int value; public void M(int {|FEX0037:value|}) { Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H50_private_const_hides_in_static_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H50 { private const int Max = 1; public static void M(int {|FEX0037:Max|}) { Console.Write(Max); } public int G() => Max; }
            """);

    [Fact]
    public Task Ignores_H51_param_hides_static_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H51 { public static void Run() { } public void M(int Run) { Console.Write(Run); } }
            """);

    [Fact]
    public Task Reports_H53_partial_class_other_part_member() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class H53 { private int value; }
            public partial class H53 { public void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Reports_H54_hides_field_declared_after_use_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H54 { public void M<T>(T {|FEX0037:value|}) { Console.Write(value); } private int value; public int G() => value; }
            """);

    [Fact]
    public Task Reports_H55_delegate_lambda_explicit_typed() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H55 { private int value; public Func<int, int> M() => (int {|FEX0037:value|}) => value; public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H56_static_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H56 { private int value; public Func<int, int> M() => static value => value; public int G() => value; }
            """);

    [Fact]
    public Task Ignores_H57_param_hides_private_field_in_nested() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H57 { private class In { public int value; } public void M(int value) { Console.Write(value); } }
            """);

    [Fact]
    public Task Ignores_H58_ctor_param_hides_field_and_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H58 { private int value; public H58(int value) { Console.Write(value); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_H59_record_primary_ctor_param_plain() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public record H59(int Value) { public int Other(int {|FEX0037:Value|}, int x) => Value + x; }
            """);

    [Fact]
    public Task Ignores_H60_local_func_in_static_method_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class H60 { private int value; public static int M() { int L(int value) => value; return L(1); } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_M08_parameter_named_like_a_field_like_event() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M08 { public event Action? Evt; public void M(int Evt) { Console.Write(Evt); } public void Raise() { Evt?.Invoke(); } }
            """);

    [Fact]
    public Task Ignores_M09_extern_operator_parameter_named_like_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M09 { public int Prop { get; set; } public static extern M09 operator +(M09 a, int Prop); }
            """);

    [Fact]
    public Task Ignores_M10_extern_conversion_operator_parameter_named_like() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M10 { public int Prop { get; set; } public static extern explicit operator int(M10 Prop); }
            """);

    [Fact]
    public Task Ignores_M11_extern_local_function_parameter_named_like() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M11 { public int Prop { get; set; } public void M() { [DllImport("x")] static extern void L(int Prop); L(1); } }
            """);

    [Fact]
    public Task Ignores_M12_delegate_parameter_named_like_a_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M12 { public int Prop { get; set; } public delegate void D(int Prop); }
            """);

    [Fact]
    public Task Reports_M13_indexer_with_accessor_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M13 { public int Prop { get; set; } public int this[int {|FEX0037:Prop|}] { get { return Prop; } } }
            """);

    [Fact]
    public Task Reports_M14_indexer_with_expression_bodied_accessor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M14 { public int Prop { get; set; } public int this[int {|FEX0037:Prop|}] { get => Prop; } }
            """);

    [Fact]
    public Task Ignores_M68_private_nested_record_primary_constructor_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M68 { private record R(int Prop) { public int Other { get; } = 1; } public void M() { Console.Write(new R(1)); } }
            """);

    [Fact]
    public Task Reports_HA1_static_lambda_vs_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA1 { private static int value; public Func<int, int> M() => static {|FEX0037:value|} => value; public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA2_operator_vs_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA2 { private static int value; public static HA2 operator +(HA2 a, int {|FEX0037:value|}) { Console.Write(value); return a; } public static int G() => value; }
            """);

    [Fact]
    public Task Ignores_HA3_ctor_vs_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA3 { private static int value; public HA3(int value) { Console.Write(value); } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA4_local_function_in_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA4 { private int value; public HA4() { int L(int {|FEX0037:value|}) => value; Console.Write(L(1)); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_HA5_lambda_in_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA5 { private int value; public HA5() { Func<int, int> f = {|FEX0037:value|} => value; Console.Write(f(1)); } public int G() => value; }
            """);

    [Fact]
    public Task Reports_HA6_interface_default_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface HA6 { int value { get; } void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Ignores_HA7_nested_class_vs_outer_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA7 { private static int value; public class In { public void M(int value) { Console.Write(value); } } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA8_field_initializer_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA8 { private int value; public Func<int, int> F = {|FEX0037:value|} => value; public int G() => value; }
            """);

    [Fact]
    public Task Reports_HA9_partial_method_decl_and_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class HA9 { private int value; partial void M(int value); }
            public partial class HA9 { partial void M(int {|FEX0037:value|}) { Console.Write(value); } public int G() { M(1); return value; } }
            """);

    [Fact]
    public Task Reports_HA10_local_function_in_static_method_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA10 { private static int value; public static int M() { int L(int {|FEX0037:value|}) => value; return L(1); } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA11_derived_from_generic_base_protected_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA11B<T> { protected T? value; }
            public class HA11 : HA11B<int> { public void M(int {|FEX0037:value|}) { Console.Write(value); } }
            """);

    [Fact]
    public Task Reports_HA12_static_local_function_vs_static_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA12 { private static int value; public static int M() { static int L(int {|FEX0037:value|}) => value; return L(1); } public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA13_expression_bodied_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA13 { private int value; public int M(int {|FEX0037:value|}) => value; public int G() => value; }
            """);

    [Fact]
    public Task Ignores_HA14_extern_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA14 { private int value; [DllImport("x")] public static extern void M(int value); public int G() => value; }
            """);

    [Fact]
    public Task Reports_HA15_private_protected_inherited() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA15B { private protected int f; }
            public class HA15 : HA15B { public void M(int {|FEX0037:f|}) { Console.Write(f); } }
            """);

    [Fact]
    public Task Reports_HA16_field_declared_in_a_base_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA16 : Exception { public void M(string {|FEX0037:Message|}) { Console.Write(Message); } }
            """);

    [Fact]
    public Task Reports_HA17_setter_with_param_none_indexer_set() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA17 { public int Value { get; set; } public int M() { int L(int {|FEX0037:Value|}) => Value; return L(1); } }
            """);

    [Fact]
    public Task Reports_HA18_anonymous_method_in_static_method_vs() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA18 { private static int value; public static Func<int, int> M() => delegate (int {|FEX0037:value|}) { return value; }; public static int G() => value; }
            """);

    [Fact]
    public Task Reports_HA19_record_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public record struct HA19(int X) { public void M(int {|FEX0037:X|}) { Console.Write(X); } }
            """);

    [Fact]
    public Task Reports_HA20_private_field_of_base_accessible_via() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class HA20 { private int value; public class D : HA20 { public void M(int {|FEX0037:value|}) { Console.Write(value); } } public int G() => value; }
            """);

    [Fact]
    public Task Ignores_extern_operator_parameter() =>
        VerifyAsync("""
            public class C { static int x; public static extern C operator +(C a, int x); public int X => x; }
            """);

    [Fact]
    public Task Ignores_extern_conversion_parameter() =>
        VerifyAsync("""
            public class C { static int x; public static extern implicit operator C(int x); public int X => x; }
            """);

    [Fact]
    public Task Reports_extern_local_function_parameter() =>
        VerifyAsync("""
            using System.Runtime.InteropServices;

            public class C { static int x; public int M() { [DllImport("k")] static extern void L(int {|FEX0037:x|}); L(1); return x; } }
            """);

    [Fact]
    public Task Reports_interface_indexer_parameter() =>
        VerifyAsync("""
            public interface I { int P { get; } int this[int {|FEX0037:P|}] { get; } }
            """);

    [Fact]
    public Task Reports_abstract_indexer_parameter() =>
        VerifyAsync("""
            public abstract class C { public int P { get; } public abstract int this[int {|FEX0037:P|}] { get; } }
            """);

    [Fact]
    public Task Ignores_delegate_parameter() =>
        VerifyAsync("""
            public class C { int x; delegate void D(int x); public int X => x; }
            """);

    [Fact]
    public Task Ignores_parameter_named_like_a_field_like_event() =>
        VerifyAsync("""
            using System;

            public class C { public event Action? E; public void M(int E) { Console.Write(E); } public void Raise() { E?.Invoke(); } }
            """);

    [Fact]
    public Task Reports_expression_bodied_operator_parameter() =>
        VerifyAsync("""
            public class C { static int x; public static C operator +(C a, int {|FEX0037:x|}) => a; public int X => x; }
            """);

    [Fact]
    public Task Reports_expression_bodied_conversion_parameter() =>
        VerifyAsync("""
            public class C { static int x; public static implicit operator C(int {|FEX0037:x|}) => new(); public int X => x; }
            """);

    [Fact]
    public Task Ignores_arglist_parameter() =>
        VerifyAsync("""
            public class A { public void V(int a) { V(a, 0, __arglist()); } public void V(int a, int b = 0, __arglist) { System.Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_delegate_parameters_named_like_delegate_members() =>
        VerifyAsync("""
            public delegate int D(int Method, object Target);
            """);

    [Fact]
    public Task Ignores_partial_method_implementation_in_the_declaration_of_its_definition() =>
        VerifyAsync("""
            public partial class P { private int value; partial void Q(int value); partial void Q(int value) { } public void G() => Q(1); }
            """);
}
