using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantOverloadGlobalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantOverloadGlobalAnalyzer>(source);

    [Fact]
    public Task Ignores_E02_forwarder_implements_interface_member() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface E02I { void M(int a); }
            public class E02 : E02I { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_E06_forwarder_is_explicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public interface E06I { void M(int a); }
            public class E06 : E06I { public void M(int a, int b = 0) { Console.Write(a + b); } void E06I.M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_E08_forwarder_is_new_hiding() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E08B { public void M(int a) { Console.Write(a); } }
            public class E08 : E08B { public void M(int a, int b = 0) { Console.Write(a + b); } public new void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_E09_forwarder_is_sealed_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E09B { public virtual void M(int a) { Console.Write(a); } }
            public class E09 : E09B { public void M(int a, int b = 0) { Console.Write(a + b); } public sealed override void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_E10_private_nested_class_public_forwarder() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E10 { private class In { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } } public void G() => new In().M(1); }
            """);

    [Fact]
    public Task Ignores_M16_call_reaches_the_base_class_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M16B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public class M16 : M16B { public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_M17_async_void_forwarder() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M17 { public void M(int a, int b = 0) { Console.Write(a + b); } public async void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M18_this_call_with_explicit_type_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M18 { public void M<T>(int a, int b = 0) { Console.Write(a + b); } public void M<T>(int a) { this.M<T>(a, 0); } }
            """);

    [Fact]
    public Task Reports_M19_forwarder_has_other_arity() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M19 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}<T>(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M20_forwarder_returns_wider_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M20 { public int M(int a, int b = 0) { return a + b; } public long M(int a) { return M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M21_void_forwarder_drops_result() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M21 { public int M(int a, int b = 0) { return a + b; } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_M22_omitted_trailing_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M22 { public void M(int a, int b = 0, int c = 5) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M23_self_recursion() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M23 { public void M(int a) { M(a); } }
            """);

    [Fact]
    public Task Ignores_M24_call_to_a_shorter_sibling() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M24 { public void M(int a) { Console.Write(a); } public void M(int a, int b) { M(a); } }
            """);

    [Fact]
    public Task Ignores_M25_swapped_arguments() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M25 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b) { M(b, a, 0); } }
            """);

    [Fact]
    public Task Ignores_M26_computed_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M26 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a + 1, 0); } }
            """);

    [Fact]
    public Task Ignores_M27_null_for_a_required_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M27 { public void M(int a, string? s) { Console.Write(a + s); } public void M(int a) { M(a, null); } }
            """);

    [Fact]
    public Task Ignores_M28_non_constant_argument_for_null_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M28 { private static string? Make() => null; public void M(int a, string? s = null) { Console.Write(a + s); } public void M(int a) { M(a, Make()); } }
            """);

    [Fact]
    public Task Reports_M29_other_overload_with_params_needs_one() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M29 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, params int[] rest) { Console.Write(rest); } }
            """);

    [Fact]
    public Task Reports_M30_other_overload_with_params_takes_many() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M30 { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b) { M(a, b, 0); } public void M(params int[] xs) { Console.Write(xs); } }
            """);

    [Fact]
    public Task Ignores_M51_inferred_generic_target() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M51 { public void M<T>(T a, int b = 0) { Console.Write(a); Console.Write(b); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M52_constructor_chaining_to_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M52B { public M52B(int a, int b = 0) { Console.Write(a + b); } }
            public class M52 : M52B { public M52(int a) : base(a, 0) { } }
            """);

    [Fact]
    public Task Ignores_M56_async_forwarder_returning_task() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M56 { public Task<int> M(int a, int b = 0) => Task.FromResult(a + b); public async Task<int> M(int a) { return await M(a, 0); } }
            """);

    [Fact]
    public Task Reports_M57_params_sibling_several_arguments_with_optional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M57 { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b) { M(a, b, 0); } public void M(int a, int b, params int[] rest) { Console.Write(rest); } }
            """);

    [Fact]
    public Task Reports_M58_sibling_that_takes_the_arguments_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M58 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, long b) { Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_M59_forwarder_converts_its_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M59 { public void M(long a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_M64_params_target_gets_an_empty_array() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M64 { public void M(int a, int b = 0, params int[] rest) { Console.Write(a + b + rest.Length); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M65_forwarder_in_a_derived_class_calls() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M65B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public class M65 : M65B { public void M(int a) { base.M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M66_forwarder_on_another_instance() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M66 { private readonly M66 _o = new(); public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { _o.M(a, 0); } }
            """);

    [Fact]
    public Task Reports_M67_constructor_with_this_initializer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M67 { public M67(int a, int b = 0) { Console.Write(a + b); } public {|FEX0039:M67|}(int a) : this(a, 0) { } }
            """);

    [Fact]
    public Task Ignores_M69_params_target_gets_a_real_element() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M69 { public void M(int a, int b = 0, params int[] rest) { Console.Write(a + b + rest.Length); } public void M(int a) { M(a, 0, 5); } }
            """);

    [Fact]
    public Task Reports_O01_oracle_public_forwarding_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O01 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_O02_forwards_a_different_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O02 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 1); } }
            """);

    [Fact]
    public Task Ignores_O03_extra_statement_before_forwarding() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O03 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { Console.Write(1); M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_O04_expression_bodied_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O04 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) => M(a, 0); }
            """);

    [Fact]
    public Task Ignores_O05_returning_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O05 { public int M(int a, int b = 0) => a + b; public int M(int a) => M(a, 0); }
            """);

    [Fact]
    public Task Reports_O06_named_argument_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O06 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, b: 0); } }
            """);

    [Fact]
    public Task Reports_O08_return_statement_block_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O08 { public int M(int a, int b = 0) => a + b; public int {|FEX0039:M|}(int a) { return M(a, 0); } }
            """);

    [Fact]
    public Task Reports_O09_null_default_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O09 { public void M(int a, string? s = null) { Console.Write(a + s); } public void {|FEX0039:M|}(int a) { M(a, null); } }
            """);

    [Fact]
    public Task Ignores_O10_private() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O10 { private void M(int a, int b = 0) { Console.Write(a + b); } private void M(int a) { M(a, 0); } public void G() { M(1); M(1, 2); } }
            """);

    [Fact]
    public Task Reports_O11_internal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O11 { internal void M(int a, int b = 0) { Console.Write(a + b); } internal void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_O12_protected() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O12 { protected void M(int a, int b = 0) { Console.Write(a + b); } protected void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_O13_oracle_Mow_no_forwarding() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O13 { public void Mo(int a = 0) { Console.Write(a); } public void Mo() { } }
            """);

    [Fact]
    public Task Ignores_O14_optional_and_different_type_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O14 { public void Mo(int a = 0) { Console.Write(a); } public void Mo(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_O15_two_optionals_overload_forwarding_first() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O15 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Reports_O17_ctor_overloads() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O17 { public O17(int a, int b = 0) { Console.Write(a + b); } public {|FEX0039:O17|}(int a) : this(a, 0) { } }
            """);

    [Fact]
    public Task Reports_O18_generic() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O18 { public void M<T>(T a, int b = 0) { Console.Write(a + "" + b); } public void {|FEX0039:M|}<T>(T a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_O19_static() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O19 { public static void M(int a, int b = 0) { Console.Write(a + b); } public static void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_O21_optional_first_param_overload_takes_none() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O21 { public void M(int a = 0, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}() { M(0, 0); } }
            """);

    [Fact]
    public Task Ignores_O22_interface() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface O22 { void M(int a, int b = 0); void M(int a); }
            """);

    [Fact]
    public Task Ignores_O23_virtual() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O23 { public virtual void M(int a, int b = 0) { Console.Write(a + b); } public virtual void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_O24_forward_with_default_keyword() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O24 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, default); } }
            """);

    [Fact]
    public Task Ignores_O25_overload_with_different_leading_type_not() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O25 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(string a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_O26_optional_param_params_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O26 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(params int[] a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_O27_overload_fewer_params_different_order_of() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O27 { public void M(int a, string s = "") { Console.Write(a + s); } public void M(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_O28_optional_method_hidden_by_overload_where() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O28 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a, int b, int c) { Console.Write(a + b + c); } }
            """);

    [Fact]
    public Task Ignores_O29_two_methods_both_optional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O29 { public void M(int a = 0) { Console.Write(a); } public void M(string s, int a = 0) { Console.Write(s + a); } }
            """);

    [Fact]
    public Task Ignores_O30_overload_with_same_prefix_and_fewer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O30 { public int M(int a, int b = 0) => a + b; public void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_O31_overload_forwarding_with_swapped_args() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O31 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(0, a); } }
            """);

    [Fact]
    public Task Ignores_O32_forwarding_with_extra_call_after() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O32 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); Console.Write(1); } }
            """);

    [Fact]
    public Task Reports_O33_forwarding_but_with_const_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O33 { private const int D = 3; public void M(int a, int b = D) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, D); } }
            """);

    [Fact]
    public Task Reports_O34_forwards_expression_default_in_body_1_2() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O34 { public void M(int a, int b = 3) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 1 + 2); } }
            """);

    [Fact]
    public Task Reports_O35_forwarding_with_this_qualifier() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O35 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { this.M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_O36_indexer_method_with_CT_optional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O36 { public Task M(int a, CancellationToken ct = default) => Task.CompletedTask; public Task M(int a) => M(a, default); }
            """);

    [Fact]
    public Task Ignores_O37_optional_param_in_different_class_hierarchy() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O37B { public void M(int a) { Console.Write(a); } }
            public class O37 : O37B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_O38_same_but_base_has_optional_and() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O38B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public class O38 : O38B { public void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_O39_forward_string_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O39 { public void M(int a, string s = "x") { Console.Write(a + s); } public void {|FEX0039:M|}(int a) { M(a, "x"); } }
            """);

    [Fact]
    public Task Reports_O40_forward_null_default_for_reference_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O40 { public void M(int a, object? o = null) { Console.Write(a + "" + o); } public void {|FEX0039:M|}(int a) { M(a, default(object)); } }
            """);

    [Fact]
    public Task Reports_O41_params_of_optional_is_nullable_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O41 { public void M(int a, int? b = null) { Console.Write(a + "" + b); } public void {|FEX0039:M|}(int a) { M(a, null); } }
            """);

    [Fact]
    public Task Reports_O42_forwarding_with_one_more_optional_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O42 { public void M(int a, int b = 0, int c = 5) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a) { M(a, 0, 5); } }
            """);

    [Fact]
    public Task Ignores_O43_forwarding_when_only_part_of_the() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O43 { public void M(int a, int b = 0, int c = 5) { Console.Write(a + b + c); } public void M(int a, int b) { M(a, b, 5); } }
            """);

    [Fact]
    public Task Ignores_O44_explicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface O44I { void M(int a, int b = 0); void M(int a); }
            public class O44 : O44I { void O44I.M(int a, int b) { Console.Write(a + b); } void O44I.M(int a) { ((O44I)this).M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA1_private_ctor_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA1 { private OA1(int a, int b = 0) { Console.Write(a + b); } private OA1(int a) : this(a, 0) { } public static OA1 Make() => new OA1(1); }
            """);

    [Fact]
    public Task Ignores_OA2_ctor_with_body_statements() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA2 { public OA2(int a, int b = 0) { Console.Write(a + b); } public OA2(int a) : this(a, 0) { Console.Write(1); } }
            """);

    [Fact]
    public Task Ignores_OA3_ctor_with_different_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA3 { public OA3(int a, int b = 0) { Console.Write(a + b); } public OA3(int a) : this(a, 1) { } }
            """);

    [Fact]
    public Task Ignores_OA4_override_forwarding() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA4B { public virtual void M(int a, int b = 0) { Console.Write(a + b); } public virtual void M(int a) { M(a, 0); } }
            public class OA4 : OA4B { public override void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_OA5_optional_2_forwarding_overloads() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA5 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a) { M(a, 0, 0); } public void M(int a, int b) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Reports_OA6_forwarding_overload_with_its_own_optional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA6 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b = 0) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Reports_OA7_reordered_named_args() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA7 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(b: 0, a: a); } }
            """);

    [Fact]
    public Task Reports_OA8_explicit_cast_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA8 { public void M(int a, long b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, (long)0); } }
            """);

    [Fact]
    public Task Reports_OA9_renamed_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA9 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int x) { M(x, 0); } }
            """);

    [Fact]
    public Task Ignores_OA10_generic_explicit_type_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA10 { public void M<T>(T a, int b = 0) { Console.Write(a + "" + b); } public void M<T>(T a) { M<T>(a, 0); } }
            """);

    [Fact]
    public Task Reports_OA11_Task_block_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA11 { public Task M(int a, int b = 0) => Task.CompletedTask; public Task {|FEX0039:M|}(int a) { return M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA12_expression_bodied_void_forward_again() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA12 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) => M(a, 0); }
            """);

    [Fact]
    public Task Reports_OA13_no_required_M_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA13 { public void M(int a = 1) { Console.Write(a); } public void {|FEX0039:M|}() { M(1); } }
            """);

    [Fact]
    public Task Ignores_OA14_only_zero_length_prefix_with_required_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA14 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M() { Console.Write(1); } }
            """);

    [Fact]
    public Task Ignores_OA15_indexer_pair() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA15 { public int this[int a, int b = 0] => a + b; public int this[int a] => this[a, 0]; }
            """);

    [Fact]
    public Task Ignores_OA16_ref_kind_mismatch_prefix() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA16 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(ref int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA17_extension_pair() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public static class OA17 { public static void M(this string s, int a, int b = 0) { Console.Write(s + a + b); } public static void M(this string s, int a) { s.M(a, 0); } }
            """);

    [Fact]
    public Task Reports_OA18_sealed_override_forward_no_overload_new() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA18B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public class OA18 : OA18B { public new void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA19_overload_chain_forwarding_to_one_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA19 { public int M(int a, int b = 0) => a + b; public int M(int a) { var r = M(a, 0); return r; } }
            """);

    [Fact]
    public Task Ignores_OA20_params_two_optionals_mismatch_order_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA20 { public void M(int a, int b = 0, int c = 5) { Console.Write(a + b + c); } public void M(int a) { M(a, 0, 6); } }
            """);

    [Fact]
    public Task Ignores_OA21_async_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA21 { public Task<int> M(int a, int b = 0) => Task.FromResult(a + b); public async Task<int> M(int a) { return await M(a, 0); } }
            """);

    [Fact]
    public Task Reports_OA22_partial_class_overloads_across_parts() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class OA22 { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public partial class OA22 { public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA23_private_public_mix_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA23 { public void M(int a, int b = 0) { Console.Write(a + b); } private void M(int a) { M(a, 0); } public void G() => M(1); }
            """);

    [Fact]
    public Task Reports_OA24_protected_internal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA24 { protected internal void M(int a, int b = 0) { Console.Write(a + b); } protected internal void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_OA25_private_in_private_nested_class_public() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA25 { private class In { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } } public object G() => new In(); }
            """);

    [Fact]
    public Task Reports_OA26_internal_class_public_methods() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            internal class OA26 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_OA27_forwarding_via_discard_variable_assignments() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA27 { public void M(int a, string s = "") { Console.Write(a + s); } public void {|FEX0039:M|}(int a) { M(a, ""); } }
            """);

    [Fact]
    public Task Reports_OA28_forwarding_double_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA28 { public void M(int a, double d = 1.5) { Console.Write(a + d); } public void {|FEX0039:M|}(int a) { M(a, 1.5); } }
            """);

    [Fact]
    public Task Reports_OA29_forwarding_enum_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA29 { public void M(int a, DayOfWeek d = DayOfWeek.Monday) { Console.Write(a + "" + d); } public void {|FEX0039:M|}(int a) { M(a, DayOfWeek.Monday); } }
            """);

    [Fact]
    public Task Reports_OA30_forwarding_passes_two_params_to_3() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA30 { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Ignores_OA31_block_body_with_two_statements_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA31 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); return; } }
            """);

    [Fact]
    public Task Ignores_OA32_forward_with_optional_parameter_order_first_is_optional() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA32 { public void M(int a = 0, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_RS1_optional_forward_unrelated_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS1 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a) { M(a, 0, 0); } public void M(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Reports_RS2_two_clean_prefixes_one_with_its() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS2 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a) { M(a, 0, 0); } public void {|FEX0039:M|}(int a, int b = 0) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Reports_RS3_forward_overload_with_more_params() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS3 { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, int b, int c) { Console.Write(a + b + c); } }
            """);

    [Fact]
    public Task Ignores_RS4_forward_other_optional_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS4 { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M(string s, int b = 0) { Console.Write(s + b); } }
            """);

    [Fact]
    public Task Reports_RS5_ctor_trio() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS5 { public RS5(int a, int b = 0) { Console.Write(a + b); } public {|FEX0039:RS5|}(int a) : this(a, 0) { } public RS5(int a, int b, int c) { Console.Write(a + b + c); } }
            """);

    [Fact]
    public Task Ignores_RS6_static_optional_instance_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS6 { public static void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_RS7_different_name_forwards() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS7 { public void M(int a, int b = 0) { Console.Write(a + b); } public void N(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_RS8_ctor_with_base_chain_not_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS8B { public RS8B(int a, int b = 0) { Console.Write(a + b); } }
            public class RS8 : RS8B { public RS8(int a) : base(a, 0) { } }
            """);

    [Fact]
    public Task Reports_RS9_two_forwards_of_different_prefix_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS9 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a) { M(a, 1, 0); } public void {|FEX0039:M|}(int a, int b = 0) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Ignores_RS10_forward_with_optional_carries_different_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS10 { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b = 1) { M(a, b, 0); } }
            """);

    [Fact]
    public Task Reports_RS11_three_optional_chain_all_clean() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS11 { public void M(int a, int b = 0, int c = 0, int d = 0) { Console.Write(a + b + c + d); } public void {|FEX0039:M|}(int a, int b = 0, int c = 0) { M(a, b, c, 0); } }
            """);

    [Fact]
    public Task Reports_RS12_forward_where_overload_has_a_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RS12 { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b) { M(a, b, 0); } public void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_Ta_extra_public_void_M_string_s() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Ta { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Reports_Tb_extra_public_void_M_string_s_int() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tb { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(string s, int t) { Console.Write(s + t); } }
            """);

    [Fact]
    public Task Ignores_Tc_extra_public_void_M_bool_z() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tc { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M(bool z) { Console.Write(z); } }
            """);

    [Fact]
    public Task Reports_Td_extra_public_void_M_int_a_int() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Td { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, int b, int c, int d) { Console.Write(a + b + c + d); } }
            """);

    [Fact]
    public Task Ignores_Te_extra_public_void_M_long_a() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Te { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M(long a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_Tf_extra_public_void_M() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tf { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M() { } }
            """);

    [Fact]
    public Task Reports_Tg_extra_public_void_M_int_a_int() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tg { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, int b, int c) { Console.Write(a + b + c); } }
            """);

    [Fact]
    public Task Reports_Th_extra_public_void_M_string_s_int() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Th { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(string s, int b) { Console.Write(s + b); } }
            """);

    [Fact]
    public Task Ignores_Ti_extra_public_void_M_string_s_int() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Ti { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M(string s, int b = 0, int c = 0) { Console.Write(s + b + c); } }
            """);

    [Fact]
    public Task Ignores_Tj_extra_public_void_M_T_T_t() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tj { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } public void M<T>(T t) { Console.Write(t); } }
            """);

    [Fact]
    public Task Reports_Tk_extra_public_void_M_int_a_string() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tk { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, string s) { Console.Write(a + s); } }
            """);

    [Fact]
    public Task Reports_Tl_extra_public_void_N_string_s() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tl { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void N(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Reports_Tm_extra_public_void_M_int_a_long() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tm { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0039:M|}(int a) { M(a, 0); } public void M(int a, long b) { Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_Tn_extra_private_void_M_string_s() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tn { public void M(int a, int b = 0) { Console.Write(a + b); } public void M(int a) { M(a, 0); } private void M(string s) { Console.Write(s); } }
            """);

    [Fact]
    public Task Ignores_Tq_two_clean_forwards_of_same_M() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tq { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b) { M(a, b, 0); } public void M(int a, int b, int c, int d) { Console.Write(d); } }
            """);

    [Fact]
    public Task Ignores_Tr_M_a_clean_to_M1_and_M_a_b_0() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tr { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void M(int a) { M(a, 0, 0); } public void M(int a, int b = 0) { Console.Write(b); } }
            """);

    [Fact]
    public Task Reports_Ts_M_a_b_0_clean_plus_M_a_clean_plus() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Ts { public void M(int a, int b = 0, int c = 0) { Console.Write(a + b + c); } public void {|FEX0039:M|}(int a, int b = 0) { M(a, b, 0); } public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Reports_Tt_M_a_forwards_to_M_a_b_0_and_M_a_b_0() =>
        VerifyAsync("""
            #nullable enable
            using System;

            public class Tt { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b = 0) { M(a, b, 0); } public void {|FEX0039:M|}(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_positional_swap_of_the_parameters() =>
        VerifyAsync("""
            using System;

            public class C { public void M(int a, int b, int c = 0) { Console.Write(a + b + c); } public void M(int a, int b) { M(b, a, 0); } }
            """);

    [Fact]
    public Task Ignores_arglist_forwarder() =>
        VerifyAsync("""
            public class A { public void V(int a) { V(a, 0, __arglist()); } public void V(int a, int b = 0, __arglist) { System.Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_target_less_accessible_than_forwarder() =>
        VerifyAsync("""
            public class A { public void M(int a) { M(a, 0); } private void M(int a, int b = 0) { System.Console.Write(a + b); } }
            public class B { protected void M(int a) { M(a, 0); } internal void M(int a, int b = 0) { System.Console.Write(a + b); } }
            public class C { public C(int a) : this(a, 0) { } private C(int a, int b = 0) { System.Console.Write(a + b); } }
            """);

    [Fact]
    public Task Ignores_params_forwarder() =>
        VerifyAsync("""
            public class A { public void M(params int[] a) { M(a, 0); } public void M(int[] a, int b = 0) { System.Console.Write(a.Length + b); } }
            """);

    [Fact]
    public Task Ignores_parameterless_constructor() =>
        VerifyAsync("""
            public class A { public A() : this(0) { } public A(int a = 0) { System.Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_forwarder_used_as_method_group() =>
        VerifyAsync("""
            public class A { public void M(int a) { M(a, 0); } public void M(int a, int b = 0) { System.Console.Write(a + b); } public System.Action<int> F() => M; }
            """);

    [Fact]
    public Task Ignores_in_out_and_dynamic_parameters() =>
        VerifyAsync("""
            public class A
            {
                public void I(in int a) { I(in a, 0); }
                public void I(in int a, int b = 0) { System.Console.Write(a + b); }
                public void O(out int a) { O(out a, 0); }
                public void O(out int a, int b = 0) { a = b; }
                public void D(object a) { D(a, 0); }
                public void D(dynamic a, int b = 0) { System.Console.Write(b); }
            }
            """);

    [Fact]
    public Task Ignores_forwarder_that_leaves_a_caller_info_parameter_to_the_compiler() =>
        VerifyAsync("""
            using System.Runtime.CompilerServices;
            public class A { public void M(string s) { M(s, 0); } public void M(string s, int n = 0, [CallerMemberName] string caller = "") { System.Console.Write(s + n + caller); } }
            """);

    [Fact]
    public Task Reports_default_literal_for_default_cancellation_token() =>
        VerifyAsync("""
            using System.Threading;
            public class A { public void {|FEX0039:M|}(int a) { M(a, default); } public void M(int a, CancellationToken ct = default) { System.Console.Write(a); ct.ThrowIfCancellationRequested(); } }
            """);

    [Fact]
    public Task Reports_NaN_forwarded_as_the_default() =>
        VerifyAsync("""
            public class A { public void {|FEX0039:M|}(int a) { M(a, double.NaN); } public void M(int a, double d = double.NaN) { System.Console.Write(a + d); } }
            """);
}
