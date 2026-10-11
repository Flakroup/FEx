using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class ValueParameterNotUsedAnalyzerTests
{
    private static Task VerifyAsync(string source, LanguageVersion? languageVersion = null, string[]? otherFiles = null) =>
        AnalyzerTestHelper.VerifyAsync<ValueParameterNotUsedAnalyzer>(source, languageVersion: languageVersion, otherFiles: otherFiles);

    [Fact]
    public Task Reports_M42_setter_assigns_a_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M42B { public virtual int P { get; set; } }
            public class M42 : M42B { public override int P { get => base.P; {|FEX0050:set|} => base.P = 5; } }
            """);

    [Fact]
    public Task Reports_N09_setter_static_local_function_parameter_named() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class N09 { private int _v; public int V { get { return _v; } {|FEX0050:set|} { static int L(int value) => value; _v = L(1); } } }
            """);

    [Fact]
    public Task Reports_M70_implicit_interface_implementation_setter_with_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface M70I { int P { get; set; } }
            public class M70 : M70I { public int P { get => 1; {|FEX0050:set|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Reports_M71_explicit_interface_implementation_setter_with_a() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface M71I { int P { get; set; } }
            public class M71 : M71I { int M71I.P { get => 1; {|FEX0050:set|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Reports_M72_override_setter_with_return_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M72B { public virtual int P { get; set; } }
            public class M72 : M72B { public override int P { get => 1; {|FEX0050:set|} { return; } } }
            """);

    [Fact]
    public Task Reports_M73_override_setter_with_a_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M73B { public virtual int P { get; set; } }
            public class M73 : M73B { public override int P { get => 1; {|FEX0050:set|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Ignores_M74_override_setter_with_a_comment_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M74B { public virtual int P { get; set; } }
            public class M74 : M74B { public override int P { get => 1; set { /* nothing */ } } }
            """);

    [Fact]
    public Task Reports_M75_override_event_add_with_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M75B { public virtual event Action? E { {|FEX0050:add|} { } {|FEX0050:remove|} { } } }
            public class M75 : M75B { public override event Action? E { {|FEX0050:add|} { Console.Write(1); } {|FEX0050:remove|} { Console.Write(2); } } }
            """);

    [Fact]
    public Task Reports_M76_override_init_with_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M76B { public virtual int P { get; init; } }
            public class M76 : M76B { public override int P { get => 1; {|FEX0050:init|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Reports_M77_abstract_override_with_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class M77B { public abstract int P { get; set; } }
            public class M77 : M77B { public override int P { get => 1; {|FEX0050:set|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Reports_W01_oracle_set() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W01 { private int _p; public int P { get => _p; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Reports_W02_set_assigns_constant() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W02 { private int _p; public int P { get => _p; {|FEX0050:set|} => _p = 1; } }
            """);

    [Fact]
    public Task Ignores_W03_set_throws_block() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W03 { public int P { get => 1; set { throw new NotSupportedException(); } } }
            """);

    [Fact]
    public Task Ignores_W04_set_throw() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W04 { public int P { get => 1; set => throw new NotSupportedException(); } }
            """);

    [Fact]
    public Task Reports_W05_init() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W05 { public int P { get => 1; {|FEX0050:init|} { } } }
            """);

    [Fact]
    public Task Reports_W06_indexer_set() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W06 { public int this[int i] { get => i; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Reports_W07_event_accessors() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W07 { public event Action E { {|FEX0050:add|} { } {|FEX0050:remove|} { } } }
            """);

    [Fact]
    public Task Ignores_W08_value_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W08 { private Action? _a; public Action P { get => _a!; set { _a = () => Console.Write(value); } } }
            """);

    [Fact]
    public Task Ignores_W09_nameof_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W09 { public int P { get => 1; set { Console.Write(nameof(value)); } } }
            """);

    [Fact]
    public Task Reports_W10_private_set_empty() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W10 { public int P { get => 1; private {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Ignores_W11_assign_to_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W11 { public int P { get => 1; set { value = 1; } } }
            """);

    [Fact]
    public Task Reports_W12_set_with_other_statements() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W12 { public int P { get => 1; {|FEX0050:set|} { Console.Write(1); } } }
            """);

    [Fact]
    public Task Ignores_W13_auto_property_silent() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W13 { public int P { get; set; } }
            """);

    [Fact]
    public Task Ignores_W14_abstract_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class W14 { public abstract int P { get; set; } }
            """);

    [Fact]
    public Task Ignores_W15_set_uses_value_in_local_function() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W15 { public int P { get => 1; set { void L() => Console.Write(value); L(); } } }
            """);

    [Fact]
    public Task Ignores_W16_override_setter_ignoring() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W16B { public virtual int P { get; set; } }
            public class W16 : W16B { public override int P { get => 1; set { } } }
            """);

    [Fact]
    public Task Ignores_W17_explicit_interface_impl_setter_ignoring() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface W17I { int P { get; set; } }
            public class W17 : W17I { int W17I.P { get => 1; set { } } }
            """);

    [Fact]
    public Task Reports_W18_event_add_only_discarding_remove_ok() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W18 { public event Action E { add { Console.Write(value); } {|FEX0050:remove|} { } } }
            """);

    [Fact]
    public Task Reports_W19_expression_bodied_setter_ignoring_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W19 { public int P { get => 1; {|FEX0050:set|} => Console.Write(1); } }
            """);

    [Fact]
    public Task Reports_W20_set_with_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W20 { public int P { get => 1; {|FEX0050:set|} { return; } } }
            """);

    [Fact]
    public Task Reports_W21_static_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W21 { public static int P { get => 1; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Reports_W22_interface_property_with_default_impl_set() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface W22 { int P { get => 1; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Reports_W23_field_keyword_C_14() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class W23 { public int P { get => field; {|FEX0050:set|} { } } }
            """, languageVersion: LanguageVersion.Preview);

    [Fact]
    public Task Ignores_WA1_implicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface WA1I { int P { get; set; } }
            public class WA1 : WA1I { public int P { get => 1; set { } } }
            """);

    [Fact]
    public Task Reports_WA2_virtual() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA2 { public virtual int P { get => 1; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Ignores_WA3_sealed_override() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA3B { public virtual int P { get; set; } }
            public class WA3 : WA3B { public sealed override int P { get => 1; set { } } }
            """);

    [Fact]
    public Task Reports_WA4_throw_after_statement() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA4 { public int P { get => 1; {|FEX0050:set|} { Console.Write(1); throw new NotSupportedException(); } } }
            """);

    [Fact]
    public Task Reports_WA5_conditional_throw() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA5 { public int P { get => 1; {|FEX0050:set|} { if (DateTime.Now.Year > 3000) throw new NotSupportedException(); } } }
            """);

    [Fact]
    public Task Ignores_WA6_expression_throw_with_call() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA6 { public int P { get => 1; set => throw new NotSupportedException(); } }
            """);

    [Fact]
    public Task Reports_WA7_event_add_throw() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA7 { public event Action E { add => throw new NotSupportedException(); {|FEX0050:remove|} { } } }
            """);

    [Fact]
    public Task Ignores_WA8_init_uses_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA8 { private int _p; public int P { get => _p; init { _p = value; } } }
            """);

    [Fact]
    public Task Reports_WA10_value_in_string_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA10 { public int P { get => 1; {|FEX0050:set|} { Console.Write("value"); } } }
            """);

    [Fact]
    public Task Ignores_WA11_value_used_in_nested_accessor_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA11 { private int _p; public int P { get => _p; set { var x = value; _p = 1; Console.Write(x); } } }
            """);

    [Fact]
    public Task Reports_WA12_generic_class_setter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA12<T> { public T? P { get => default; {|FEX0050:set|} { } } }
            """);

    [Fact]
    public Task Reports_WA13_setter_calling_method_with_comment() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA13 { public int P { get => 1; {|FEX0050:set|} { /* nothing */ } } }
            """);

    [Fact]
    public Task Ignores_WA14_abstract_class_overriding_abstract_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class WA14B { public abstract int P { get; set; } }
            public class WA14 : WA14B { public override int P { get => 1; set { } } }
            """);

    [Fact]
    public Task Ignores_WA15_explicit_interface_event() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface WA15I { event Action E; }
            public class WA15 : WA15I { event Action WA15I.E { add { } remove { } } }
            """);

    [Fact]
    public Task Reports_WA16_setter_that_only_throws_via_helper() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA16 { private static void Fail() => throw new InvalidOperationException(); public int P { get => 1; {|FEX0050:set|} { Fail(); } } }
            """);

    [Fact]
    public Task Ignores_WA17_setter_returns_inside_if_with_throw() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class WA17 { public int P { get => 1; set { throw new NotSupportedException(); } } }
            """);
}
