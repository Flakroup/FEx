using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantOverriddenMemberAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantOverriddenMemberAnalyzer>(source);

    [Fact]
    public Task Ignores_M35_async_override_that_does_not_await() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M35B { public virtual Task M() => Task.CompletedTask; }
            public class M35 : M35B { public override async Task M() { base.M(); } }
            """);

    [Fact]
    public Task Ignores_M36_override_calls_another_base_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M36B { public virtual void M(int a) { Console.Write(a); } public void N(int a) { Console.Write(a); } }
            public class M36 : M36B { public override void M(int a) { base.N(a); } }
            """);

    [Fact]
    public Task Ignores_M37_override_calls_itself_through_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M37B { public virtual void M(int a) { Console.Write(a); } }
            public class M37 : M37B { public override void M(int a) { this.M(a); } }
            """);

    [Fact]
    public Task Ignores_M38_getter_with_an_attribute() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M38B { public virtual int P { get; set; } }
            public class M38 : M38B { public override int P { [System.Diagnostics.DebuggerStepThrough] get => base.P; set => base.P = value; } }
            """);

    [Fact]
    public Task Ignores_M39_property_reads_another_base_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M39B { public virtual int P => 0; public int Q => 1; }
            public class M39 : M39B { public override int P => base.Q; }
            """);

    [Fact]
    public Task Reports_M40_init_accessor_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M40B { public virtual int P { get; init; } }
            public class M40 : M40B { public override int {|FEX0047:P|} { get => base.P; init => base.P = value; } }
            """);

    // ReSharper reports the compound assignment; it is not a plain pass-through of the value, so this analyzer stays silent.
    [Fact]
    public Task Ignores_M41_compound_assignment_in_setter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M41B { public virtual int P { get; set; } }
            public class M41 : M41B { public override int P { get => base.P; set => base.P += value; } }
            """);

    [Fact]
    public Task Ignores_M42_setter_assigns_a_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M42B { public virtual int P { get; set; } }
            public class M42 : M42B { public override int P { get => base.P; set => base.P = 5; } }
            """);

    [Fact]
    public Task Ignores_M43_indexer_reads_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M43B { public virtual int this[int a] => a; }
            public class M43 : M43B { public override int this[int a] => this[a]; }
            """);

    [Fact]
    public Task Ignores_M44_indexer_reads_another_object() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M44B { public virtual int this[int a] => a; }
            public class M44 : M44B { private readonly M44B _other = new(); public override int this[int a] => _other[a]; }
            """);

    [Fact]
    public Task Reports_M45_named_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M45B { public virtual void M(int a) { Console.Write(a); } }
            public class M45 : M45B { public override void {|FEX0047:M|}(int a) { base.M(a: a); } }
            """);

    [Fact]
    public Task Ignores_M46_in_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M46B { public virtual void M(in int a) { Console.Write(a); } }
            public class M46 : M46B { public override void M(in int a) { base.M(a); } }
            """);

    [Fact]
    public Task Ignores_M47_equatable_overload_next_to_GetHashCode_pass() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M47 : IEquatable<M47> { public override int GetHashCode() => base.GetHashCode(); public bool Equals(M47? other) => other is not null; }
            """);

    [Fact]
    public Task Ignores_M48_property_reads_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M48B { public virtual int P => 0; }
            public class M48 : M48B { public override int P => this.P; }
            """);

    [Fact]
    public Task Ignores_M49_property_of_another_object() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M49B { public virtual int P => 0; }
            public class M49 : M49B { private readonly M49B _other = new(); public override int P => _other.P; }
            """);

    [Fact]
    public Task Reports_M46b_in_parameter_passed_with_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M46bB { public virtual void M(in int a) { Console.Write(a); } }
            public class M46b : M46bB { public override void {|FEX0047:M|}(in int a) { base.M(in a); } }
            """);

    [Fact]
    public Task Ignores_M50_static_Equals_next_to_GetHashCode_pass() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M50 { public override int GetHashCode() => base.GetHashCode(); public static bool Equals(M50? a, M50? b) => a is null && b is null; }
            """);

    // ReSharper reports swapped named arguments; they change which value goes where, so this analyzer stays silent.
    [Fact]
    public Task Ignores_M53_named_arguments_swapped() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M53B { public virtual void M(int a, int b) { Console.Write(a + b); } }
            public class M53 : M53B { public override void M(int a, int b) { base.M(b: a, a: b); } }
            """);

    [Fact]
    public Task Reports_M54_named_arguments_in_order() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M54B { public virtual void M(int a, int b) { Console.Write(a + b); } }
            public class M54 : M54B { public override void {|FEX0047:M|}(int a, int b) { base.M(a: a, b: b); } }
            """);

    [Fact]
    public Task Reports_M55_ref_parameter_passed_with_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M55B { public virtual void M(ref int a) { Console.Write(a); } }
            public class M55 : M55B { public override void {|FEX0047:M|}(ref int a) { base.M(ref a); } }
            """);

    [Fact]
    public Task Ignores_N39_non_override_counterpart_calling_base_next_to() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N39 { public override int GetHashCode() => base.GetHashCode(); public new bool Equals(object? o) => base.Equals(o); }
            """);

    [Fact]
    public Task Ignores_V01_oracle_ToString() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V01 { public override string ToString() => base.ToString() ?? ""; }
            """);

    [Fact]
    public Task Reports_V02_oracle_GetHashCode() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V02 { public override int {|FEX0047:GetHashCode|}() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Reports_V03_void_block_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V03B { public virtual void M(int a) { Console.Write(a); } }
            public class V03 : V03B { public override void {|FEX0047:M|}(int a) { base.M(a); } }
            """);

    [Fact]
    public Task Ignores_V04_swapped_args() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V04B { public virtual void M(int a, int b) { Console.Write(a + b); } }
            public class V04 : V04B { public override void M(int a, int b) { base.M(b, a); } }
            """);

    [Fact]
    public Task Ignores_V05_extra_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V05B { public virtual void M(int a) { Console.Write(a); } }
            public class V05 : V05B { public override void M(int a) { Console.Write(1); base.M(a); } }
            """);

    [Fact]
    public Task Reports_V06_property_get_set_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V06B { public virtual int P { get; set; } }
            public class V06 : V06B { public override int {|FEX0047:P|} { get => base.P; set => base.P = value; } }
            """);

    [Fact]
    public Task Reports_V07_get_only_property_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V07B { public virtual int P => 1; }
            public class V07 : V07B { public override int {|FEX0047:P|} => base.P; }
            """);

    [Fact]
    public Task Ignores_V08_sealed_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V08B { public virtual void M(int a) { Console.Write(a); } }
            public class V08 : V08B { public sealed override void M(int a) { base.M(a); } }
            """);

    [Fact]
    public Task Ignores_V09_attribute_on_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V09B { public virtual void M(int a) { Console.Write(a); } }
            public class V09 : V09B { [Obsolete] public override void M(int a) { base.M(a); } }
            """);

    [Fact]
    public Task Ignores_V10_cast() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V10B { public virtual int M() => 1; }
            public class V10 : V10B { public override int M() => (int)(long)base.M(); }
            """);

    [Fact]
    public Task Ignores_V13_base_call_on_different_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V13B { public virtual void M() { } public virtual void N() { } }
            public class V13 : V13B { public override void M() { base.N(); } }
            """);

    [Fact]
    public Task Ignores_V14_override_with_different_default_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V14B { public virtual void M(int a = 1) { Console.Write(a); } }
            public class V14 : V14B { public override void M(int a = 2) { base.M(a); } }
            """);

    [Fact]
    public Task Ignores_V15_Equals_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V15 { public override bool Equals(object? obj) => base.Equals(obj); public override int GetHashCode() => 1; }
            """);

    [Fact]
    public Task Reports_V16_indexer_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V16B { public virtual int this[int i] { get => i; set { } } }
            public class V16 : V16B { public override int {|FEX0047:this|}[int i] { get => base[i]; set => base[i] = value; } }
            """);

    [Fact]
    public Task Ignores_V18_async_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V18B { public virtual Task M() => Task.CompletedTask; }
            public class V18 : V18B { public override async Task M() { await base.M(); } }
            """);

    [Fact]
    public Task Ignores_V19_ToString_with_different_fallback() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V19 { public override string ToString() => base.ToString() ?? "x"; }
            """);

    [Fact]
    public Task Ignores_V20_custom_base_returning_non_null_string_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V20B { public virtual string M() => "a"; }
            public class V20 : V20B { public override string M() => base.M() ?? ""; }
            """);

    [Fact]
    public Task Ignores_V21_extra_constant_arg_change() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V21B { public virtual int M(int a) => a; }
            public class V21 : V21B { public override int M(int a) => base.M(a + 1); }
            """);

    [Fact]
    public Task Reports_V24_void_expression_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V24B { public virtual void M() { } }
            public class V24 : V24B { public override void {|FEX0047:M|}() => base.M(); }
            """);

    [Fact]
    public Task Ignores_V25_void_with_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V25B { public virtual void M() { } }
            public class V25 : V25B { public override void M() { base.M(); return; } }
            """);

    [Fact]
    public Task Reports_V26_generic_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V26B { public virtual T M<T>(T a) => a; }
            public class V26 : V26B { public override T {|FEX0047:M|}<T>(T a) => base.M(a); }
            """);

    [Fact]
    public Task Reports_V27_get_return_set_body_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V27B { public virtual int P { get; set; } }
            public class V27 : V27B { public override int {|FEX0047:P|} { get { return base.P; } set { base.P = value; } } }
            """);

    [Fact]
    public Task Ignores_V28_getter_pass_through_setter_does_nothing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V28B { public virtual int P { get; set; } }
            public class V28 : V28B { public override int P { get => base.P; set { } } }
            """);

    [Fact]
    public Task Ignores_V29_abstract_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class V29 { public abstract override string ToString(); }
            """);

    [Fact]
    public Task Reports_V30_only_getter_overridden_of_get_set() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V30B { public virtual int P { get; set; } }
            public class V30 : V30B { public override int {|FEX0047:P|} { get => base.P; set => base.P = value; } }
            """);

    [Fact]
    public Task Ignores_V31_override_of_abstract_member_calling_nothing() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class V31B { public abstract void M(); }
            public class V31 : V31B { public override void M() { } }
            """);

    [Fact]
    public Task Ignores_V32_override_that_only_has_empty_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V32B { public virtual void M() { Console.Write(1); } }
            public class V32 : V32B { public override void M() { } }
            """);

    [Fact]
    public Task Reports_V33_base_ToString_with_ref_params() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V33B { public virtual void M(ref int a) { a++; } }
            public class V33 : V33B { public override void {|FEX0047:M|}(ref int a) { base.M(ref a); } }
            """);

    [Fact]
    public Task Reports_V34_override_with_params() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V34B { public virtual void M(params int[] a) { Console.Write(a); } }
            public class V34 : V34B { public override void {|FEX0047:M|}(params int[] a) { base.M(a); } }
            """);

    [Fact]
    public Task Reports_V35_override_of_property_with_expression_get() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V35B { public virtual int P { get; protected set; } }
            public class V35 : V35B { public override int {|FEX0047:P|} { get => base.P; protected set => base.P = value; } }
            """);

    [Fact]
    public Task Ignores_V36_override_changing_return_co_variance() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V36B { public virtual object M() => new object(); }
            public class V36 : V36B { public override string M() => (string)base.M(); }
            """);

    [Fact]
    public Task Ignores_V37_override_ToString_with_null_forgiving() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V37 { public override string ToString() => base.ToString()!; }
            """);

    [Fact]
    public Task Ignores_V38_override_value_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public struct V38 { public override string ToString() => base.ToString() ?? ""; }
            """);

    [Fact]
    public Task Reports_V39_base_call_with_this_qualified_args_same() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V39B { public virtual int M(int a) => a; }
            public class V39 : V39B { public override int {|FEX0047:M|}(int a) { return base.M(a); } }
            """);

    [Fact]
    public Task Ignores_V40_override_calling_base_then_returning_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V40B { public virtual int M() => 1; }
            public class V40 : V40B { public override int M() { base.M(); return 1; } }
            """);

    [Fact]
    public Task Reports_V41_protected_override_in_different_accessibility_same() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V41B { protected virtual void M() { } }
            public class V41 : V41B { protected override void {|FEX0047:M|}() { base.M(); } }
            """);

    [Fact]
    public Task Reports_V42_override_in_a_derived_of_derived_passes_to() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V42A { public virtual void M() { } }
            public class V42B : V42A { public override void M() { Console.Write(1); } }
            public class V42 : V42B { public override void {|FEX0047:M|}() { base.M(); } }
            """);

    [Fact]
    public Task Ignores_V44_event_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V44B { public virtual event Action? E; public void R() => E?.Invoke(); }
            public class V44 : V44B { public override event Action? E { add { base.E += value; } remove { base.E -= value; } } }
            """);

    [Fact]
    public Task Reports_V45_override_with_different_nullable_annotation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V45B { public virtual string? M() => null; }
            public class V45 : V45B { public override string? {|FEX0047:M|}() => base.M(); }
            """);

    [Fact]
    public Task Ignores_V46_override_with_on_custom_nullable() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class V46B { public virtual string? M() => null; }
            public class V46 : V46B { public override string? M() => base.M() ?? ""; }
            """);

    [Fact]
    public Task Reports_VA1_Equals_on_custom_base() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA1B { public virtual bool Equals2(object? o) => true; }
            public class VA1 : VA1B { public override bool {|FEX0047:Equals2|}(object? o) => base.Equals2(o); }
            """);

    [Fact]
    public Task Reports_VA2_Equals_alone() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA2 { public override bool {|FEX0047:Equals|}(object? obj) => base.Equals(obj); }
            """);

    [Fact]
    public Task Reports_VA3_nullable_disabled_ToString() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            #nullable disable
            public class VA3 { public override string {|FEX0047:ToString|}() => base.ToString(); }
            #nullable enable
            """);

    [Fact]
    public Task Reports_VA4_nullable_ToString_returns_string() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA4 { public override string? {|FEX0047:ToString|}() => base.ToString(); }
            """);

    [Fact]
    public Task Ignores_VA5_renamed_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA5B { public virtual void M(int a) { Console.Write(a); } }
            public class VA5 : VA5B { public override void M(int b) { base.M(b); } }
            """);

    [Fact]
    public Task Reports_VA6_non_async_Task_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA6B { public virtual Task M() => Task.CompletedTask; }
            public class VA6 : VA6B { public override Task {|FEX0047:M|}() { return base.M(); } }
            """);

    [Fact]
    public Task Reports_VA7_set_only_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA7B { public virtual int P { set { Console.Write(value); } } }
            public class VA7 : VA7B { public override int {|FEX0047:P|} { set => base.P = value; } }
            """);

    [Fact]
    public Task Reports_VA8_get_override_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA8B { public virtual int P { get; protected set; } }
            public class VA8 : VA8B { public override int {|FEX0047:P|} => base.P; }
            """);

    [Fact]
    public Task Ignores_VA9_override_with_inheritdoc_comment_irrelevant_MethodImpl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA9B { public virtual void M() { } }
            public class VA9 : VA9B { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] public override void M() => base.M(); }
            """);

    [Fact]
    public Task Reports_VA10_override_base_ToString_on_custom_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA10B { public override string ToString() => "a"; }
            public class VA10 : VA10B { public override string {|FEX0047:ToString|}() => base.ToString(); }
            """);

    [Fact]
    public Task Reports_VA11_override_in_struct_GetHashCode() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public struct VA11 { public override int {|FEX0047:GetHashCode|}() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Ignores_VA15_override_with_different_argument_count_default() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA15B { public virtual void M(int a = 0) { Console.Write(a); } }
            public class VA15 : VA15B { public override void M(int a = 0) { base.M(); } }
            """);

    [Fact]
    public Task Reports_VA16_override_of_method_with_out_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA16B { public virtual bool M(out int a) { a = 1; return true; } }
            public class VA16 : VA16B { public override bool {|FEX0047:M|}(out int a) => base.M(out a); }
            """);

    [Fact]
    public Task Reports_VA17_override_with_local_pass_return_base_M_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA17B<T> { public virtual T M(T a) => a; }
            public class VA17 : VA17B<int> { public override int {|FEX0047:M|}(int a) => base.M(a); }
            """);

    [Fact]
    public Task Reports_VA18_override_pass_through_of_protected_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA18B { protected virtual void M() { } }
            public class VA18 : VA18B { protected override void {|FEX0047:M|}() { base.M(); } }
            """);

    [Fact]
    public Task Ignores_VA19_override_of_method_accessing_explicit_this() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class VA19B { public virtual void M(int a) { Console.Write(a); } }
            public class VA19 : VA19B { public override void M(int a) { this.M2(); base.M(a); } private void M2() { } }
            """);

    [Fact]
    public Task Reports_EQ1_both_pass_through() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ1 { public override bool {|FEX0047:Equals|}(object? obj) => base.Equals(obj); public override int {|FEX0047:GetHashCode|}() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Ignores_EQ2_Equals_pass_through_hash_custom() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ2 { public override bool Equals(object? obj) => base.Equals(obj); public override int GetHashCode() => 1; }
            """);

    [Fact]
    public Task Ignores_EQ3_hash_pass_through_Equals_custom() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ3 { public override bool Equals(object? obj) => obj is EQ3; public override int GetHashCode() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Reports_EQ4_ToString_pass_through_hash_custom() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ4 { public override string {|FEX0047:ToString|}() => base.ToString(); public override int GetHashCode() => 1; }
            """);

    [Fact]
    public Task Reports_EQ5_Equals_pass_through_alone_block_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ5 { public override bool {|FEX0047:Equals|}(object? obj) { return base.Equals(obj); } }
            """);

    [Fact]
    public Task Ignores_EQ6_Equals_custom_base_pass_through_with_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ6B { public override bool Equals(object? obj) => true; public override int GetHashCode() => 1; }
            public class EQ6 : EQ6B { public override bool Equals(object? obj) => base.Equals(obj); public override int GetHashCode() => 2; }
            """);

    [Fact]
    public Task Ignores_EQ7_Equals_pass_through_plus_operator() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ7 { public override bool Equals(object? obj) => base.Equals(obj); public static bool operator ==(EQ7? a, EQ7? b) => ReferenceEquals(a, b); public static bool operator !=(EQ7? a, EQ7? b) => !(a == b); public override int GetHashCode() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Reports_EQ8_Equals_object_pass_through_with_another_Equals_overload() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class EQ8 { public bool Equals(EQ8? o) => o != null; public override bool {|FEX0047:Equals|}(object? obj) => base.Equals(obj); }
            """);

    [Fact]
    public Task Ignores_an_Equals_overload_pass_through_next_to_a_written_GetHashCode() =>
        VerifyAsync("""
            public class B { public virtual bool Equals(int a, int b) => true; }
            public class C : B { public override bool Equals(int a, int b) => base.Equals(a, b); public override int GetHashCode() => 1; }
            """);

    [Fact]
    public Task Ignores_a_GetHashCode_overload_pass_through_next_to_a_written_Equals() =>
        VerifyAsync("""
            public class B { public virtual int GetHashCode(int seed) => seed; }
            public class C : B { public override int GetHashCode(int seed) => base.GetHashCode(seed); public override bool Equals(object? o) => true; public override int GetHashCode() => 1; }
            """);

    [Fact]
    public Task Ignores_record_overrides() =>
        VerifyAsync("""
            public record Base2(int A);
            public record Rec(int A) : Base2(A) { public override string ToString() => base.ToString(); public override int GetHashCode() => base.GetHashCode(); }
            """);

    [Fact]
    public Task Ignores_base_call_with_explicit_type_arguments() =>
        VerifyAsync("""
            public class B { public virtual string M<T>(T x) => typeof(T).Name; }
            public class D : B { public override string M<T>(T x) => base.M<object>(x); }
            """);

    [Fact]
    public Task Ignores_override_that_adds_a_parameter_attribute() =>
        VerifyAsync("""
            using System.Runtime.CompilerServices;
            public class B { public virtual string M(string c = "") => c; }
            public class D : B { public override string M([CallerMemberName] string c = "") => base.M(c); }
            """);

    [Fact]
    public Task Ignores_override_in_a_type_that_re_lists_the_interface() =>
        VerifyAsync("""
            public interface I { string M(); }
            public class B : I { string I.M() => "explicit"; public virtual string M() => "virtual"; }
            public class D : B, I { public override string M() => base.M(); }
            """);
}
