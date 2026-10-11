using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class UnusedTypeParameterAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<UnusedTypeParameterAnalyzer>(source);

    [Fact]
    public Task Reports_M60_nested_method_hides_the_class_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M60<{|FEX0048:T|}> { public void M<T>(T x) { Console.Write(x); } }
            """);

    [Fact]
    public Task Ignores_M61_block_documentation_comment_mentions_the_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            /** <typeparam name="T">the item</typeparam> */
            public class M61<T> { }
            """);

    [Fact]
    public Task Reports_M62_plain_comment_mentions_the_type_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            // <typeparam name="T">the item</typeparam>
            public class M62<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Reports_M63_nested_class_hides_the_class_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M63<{|FEX0048:T|}> { public class N<T> { public T? Item; } }
            """);

    [Fact]
    public Task Reports_T01_oracle() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T01 { public void M<{|FEX0048:T|}>() { } }
            """);

    [Fact]
    public Task Ignores_T02_used_in_parameter() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T02 { public void M<T>(T a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_T03_used_in_return() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T03 { public T M<T>() => default!; }
            """);

    [Fact]
    public Task Ignores_T04_used_only_in_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T04 { public Type M<T>() => typeof(T); }
            """);

    [Fact]
    public Task Ignores_T05_used_only_in_constraint_of_another() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T05 { public void M<T, U>(U u) where U : IEnumerable<T> { Console.Write(u); } }
            """);

    [Fact]
    public Task Reports_T06_class_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T06<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Ignores_T07_interface_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface T07 { void M<T>(); }
            """);

    [Fact]
    public Task Ignores_T08_abstract_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public abstract class T08 { public abstract void M<T>(); }
            """);

    [Fact]
    public Task Ignores_T09_override_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T09B { public virtual void M<T>() { } }
            public class T09 : T09B { public override void M<T>() { } }
            """);

    [Fact]
    public Task Ignores_T10_explicit_interface_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface T10I { void M<T>(); }
            public class T10 : T10I { void T10I.M<T>() { } }
            """);

    [Fact]
    public Task Reports_T11_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public struct T11<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Reports_T12_delegate() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public delegate void T12D<{|FEX0048:T|}>();
            """);

    [Fact]
    public Task Reports_T13_local_function() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T13 { public void M() { void L<{|FEX0048:T|}>() { } L<int>(); } }
            """);

    [Fact]
    public Task Reports_T14_two_params_one_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T14 { public void M<{|FEX0048:T|}, U>(U u) { Console.Write(u); } }
            """);

    [Fact]
    public Task Ignores_T15_class_T_used_in_nested_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T15<T> { public class In { public T? F; } }
            """);

    [Fact]
    public Task Ignores_T16_virtual_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T16 { public virtual void M<T>() { } }
            """);

    [Fact]
    public Task Reports_T17_record() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public record T17<{|FEX0048:T|}>;
            """);

    [Fact]
    public Task Reports_T18_interface_type_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface T18<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Ignores_T19_nameof_T() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T19 { public string M<T>() => nameof(T); }
            """);

    [Fact]
    public Task Ignores_T20_used_in_local_variable_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T20 { public void M<T>() { T t = default!; Console.Write(t); } }
            """);

    [Fact]
    public Task Reports_T21_used_in_cref_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T21 { /// <summary><see cref="M{T}"/> </summary>
                public void M<{|FEX0048:T|}>() { } }
            """);

    [Fact]
    public Task Ignores_T22_used_as_type_argument_to_another() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T22 { public void M<T>() { N<T>(); } private void N<U>() { Console.Write(typeof(U)); } }
            """);

    [Fact]
    public Task Ignores_T23_class_used_in_member() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T23<T> { public T? F; }
            """);

    [Fact]
    public Task Reports_T24_class_where_T_used_only_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T24<T> { public void M<{|FEX0048:U|}>() where U : T { } }
            """);

    [Fact]
    public Task Reports_T25_static_method_generic_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T25 { public static void M<{|FEX0048:T|}>() { } }
            """);

    [Fact]
    public Task Reports_T26_private_method_generic_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T26 { private void M<{|FEX0048:T|}>() { } public void G() => M<int>(); }
            """);

    [Fact]
    public Task Ignores_T27_generic_ctor_like_class_with_unused_T() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T27B<T> { public T? F; }
            public class T27<T> : T27B<T> { }
            """);

    [Fact]
    public Task Ignores_T28_class_T_used_in_attribute_less_interface() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T28<T> : IComparable<T> { public int CompareTo(T? other) => 0; }
            """);

    [Fact]
    public Task Ignores_T29_extern_partial_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class T29 { partial void M<T>(); }
            """);

    [Fact]
    public Task Ignores_T31_T_only_in_default_expression() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T31 { public void M<T>() { Console.Write(default(T)); } }
            """);

    [Fact]
    public Task Reports_T32_explicit_generic_arg_with_unused_constraint() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T32 { public void M<{|FEX0048:T|}>() where T : class { } }
            """);

    [Fact]
    public Task Reports_T33_enclosing_generic_type_shadow() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class T33<{|FEX0048:T|}> { public void M<{|FEX0048:T|}>() { } }
            """);

    [Fact]
    public Task Ignores_T34_interface_impl_implicit() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public interface T34I { void M<T>(); }
            public class T34 : T34I { public void M<T>() { } }
            """);

    [Fact]
    public Task Ignores_TA1_self_referential_constraint() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA1 { public void M<T>() where T : IComparable<T> { } }
            """);

    [Fact]
    public Task Ignores_TA2_sealed_override_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA2B { public virtual void M<T>() { } }
            public class TA2 : TA2B { public sealed override void M<T>() { } }
            """);

    [Fact]
    public Task Ignores_TA3_class_T_used_in_other_partial() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class TA3<T> { }
            public partial class TA3<T> { public T? F; }
            """);

    [Fact]
    public Task Ignores_TA4_class_T_used_only_in_nested() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA4<T> { public class In : List<T> { } }
            """);

    [Fact]
    public Task Reports_TA5_class_T_unused_with_base_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA5B { }
            public class TA5<{|FEX0048:T|}> : TA5B { }
            """);

    [Fact]
    public Task Ignores_TA6_typeparamref_doc_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            /// <typeparam name="T">x</typeparam>
            public class TA6<T> { }
            """);

    [Fact]
    public Task Reports_TA7_T_used_in_attribute_argument_typeof() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA7 { [Obsolete(nameof(M))] public void M<{|FEX0048:T|}>() { } }
            """);

    [Fact]
    public Task Ignores_TA8_protected_virtual_unused_in_sealed_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA8 { protected virtual void M<T>() { } }
            """);

    [Fact]
    public Task Ignores_TA9_interface_default_impl_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface TA9 { void M<T>() { } }
            """);

    [Fact]
    public Task Ignores_TA11_T_used_in_is_expression() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA11 { public bool M<T>(object o) => o is T; }
            """);

    [Fact]
    public Task Ignores_TA12_T_used_in_generic_call_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA12 { public void M<T>() { var l = new List<T>(); Console.Write(l); } }
            """);

    [Fact]
    public Task Reports_TA13_extern() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA13 { public static extern void M<{|FEX0048:T|}>(); }
            """);

    [Fact]
    public Task Reports_TA14_local_function_static_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA14 { public void M() { static void L<{|FEX0048:T|}>() { } L<int>(); } }
            """);

    [Fact]
    public Task Ignores_TA15_operator_class_T_used_only_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA15<T> { public int P => typeof(T).GetHashCode(); }
            """);

    [Fact]
    public Task Ignores_TA16_T_used_in_default_interface_member() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA16<T> : IEnumerable<T> { public IEnumerator<T> GetEnumerator() => null!; System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!; }
            """);

    [Fact]
    public Task Reports_TA18_constraint_only_on_itself_with_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class TA18 { public void M<{|FEX0048:T|}, U>(U u) where T : class where U : struct { Console.Write(u); } }
            """);

    [Fact]
    public Task Ignores_DT1_typeparamref_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            /// <summary>Uses <typeparamref name="T"/>.</summary>
            public class DT1<T> { }
            """);

    [Fact]
    public Task Ignores_DT2_typeparam_on_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class DT2 {
                /// <typeparam name="T">x</typeparam>
                public void M<T>() { } }
            """);

    [Fact]
    public Task Reports_DT3_param_tag_naming_T() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            /// <param name="T">x</param>
            public class DT3<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Reports_DT4_typeparam_of_other_name() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            /// <typeparam name="U">x</typeparam>
            public class DT4<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Reports_DT5_plain_comment_mentioning() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            // name="T"
            public class DT5<{|FEX0048:T|}> { }
            """);

    [Fact]
    public Task Ignores_generic_type_with_static_field() =>
        VerifyAsync("""
            public sealed class Id<T> { private static int s; public static int Next() => ++s; }
            """);

    [Fact]
    public Task Ignores_generic_type_with_static_auto_property() =>
        VerifyAsync("""
            public sealed class Id<T> { public static int Count { get; set; } }
            """);

    [Fact]
    public Task Reports_generic_type_with_instance_field_only() =>
        VerifyAsync("""
            public class Id<{|FEX0048:T|}> { private int x; public int X => x; }
            """);
}
