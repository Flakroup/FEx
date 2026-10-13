using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class ParameterOnlyUsedForPreconditionCheckGlobalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<ParameterOnlyUsedForPreconditionCheckGlobalAnalyzer>(source);

    [Fact]
    public Task Reports_a_parameter_read_only_by_a_throwing_guard() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string {|FEX0057:s|})
                {
                    if (string.IsNullOrEmpty(s)) throw new ArgumentException("empty", nameof(s));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_only_by_a_guard_in_a_block() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public int M(int {|FEX0057:n|})
                {
                    if (n < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(n));
                    }

                    return 1;
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_only_by_Debug_Assert_in_a_public_method() =>
        VerifyAsync("""
            using System;
            using System.Diagnostics;
            public class C
            {
                public void M(string {|FEX0057:s|})
                {
                    Debug.Assert(s.Length > 0);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_only_by_ThrowIfNull() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(object {|FEX0057:o|})
                {
                    ArgumentNullException.ThrowIfNull(o);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_only_by_ThrowIfNullOrEmpty_and_a_second_guard() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string {|FEX0057:s|}, int {|FEX0057:n|})
                {
                    ArgumentException.ThrowIfNullOrEmpty(s);
                    if (n == 0) throw new ArgumentException("zero", nameof(n));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_only_the_parameter_the_body_never_reads() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string {|FEX0057:checkedOnly|}, string used)
                {
                    ArgumentNullException.ThrowIfNull(checkedOnly);
                    ArgumentNullException.ThrowIfNull(used);
                    Console.Write(used);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_through_members_that_only_read() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public void M(List<int> {|FEX0057:items|})
                {
                    if (items.Count == 0 || items[0] < 0) throw new ArgumentException("bad", nameof(items));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_guarded_by_a_pattern() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(object {|FEX0057:o|})
                {
                    if (o is not string) throw new ArgumentException("not a string", nameof(o));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_guard_made_by_a_method_marked_AssertionMethod() =>
        VerifyAsync("""
            using System;
            public sealed class AssertionMethodAttribute : Attribute { }
            public static class Check
            {
                [AssertionMethod]
                public static void That(bool condition) { if (!condition) throw new InvalidOperationException(); }
            }
            public class C
            {
                public void M(int {|FEX0057:n|})
                {
                    Check.That(n > 0);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_only_by_Debug_Fail_Trace_Assert_and_Contract_guards() =>
        VerifyAsync("""
            using System;
            using System.Diagnostics;
            using System.Diagnostics.Contracts;
            public class C
            {
                public void Fail(int {|FEX0057:n|})
                {
                    if (n < 0) Debug.Fail("negative");
                    Console.Write(1);
                }

                public void TraceFail(int {|FEX0057:n|})
                {
                    if (n < 0) { Trace.Fail("negative"); }
                    Console.Write(1);
                }

                public void TraceAssert(int {|FEX0057:n|})
                {
                    Trace.Assert(n > 0);
                    Console.Write(1);
                }

                public void Requires(int {|FEX0057:n|})
                {
                    Contract.Requires(n > 0);
                    Console.Write(1);
                }

                public void Assume(int {|FEX0057:n|})
                {
                    Contract.Assume(n > 0);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_through_a_negation() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(int {|FEX0057:n|})
                {
                    if (!(n > 0)) throw new ArgumentOutOfRangeException(nameof(n));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_whose_field_the_guard_reads() =>
        VerifyAsync("""
            using System;
            public struct Box { public int Value; }
            public class C
            {
                public void M(Box box)
                {
                    if (box.Value < 0) throw new ArgumentOutOfRangeException(nameof(box));
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_an_internal_and_a_protected_method() =>
        VerifyAsync("""
            using System;
            public class C
            {
                internal void Internal(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                protected void Protected(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_private_method_which_is_the_local_rule() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private void M(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_constructor_a_lambda_and_a_local_function() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public C(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void M()
                {
                    Action<string> lambda = s =>
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    };
                    lambda("a");
                    Local("a");

                    void Local(string s)
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_an_override_and_of_an_interface_implementation() =>
        VerifyAsync("""
            using System;
            public interface IThing { void Do(string s); }
            public class Base { public virtual void Run(string s) { Console.Write(s); } }
            public class C : Base, IThing
            {
                public override void Run(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Do(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_the_body_reads_after_the_guard() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string s)
                {
                    if (string.IsNullOrEmpty(s)) throw new ArgumentException("empty", nameof(s));
                    Console.Write(s);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_the_throw_message_reads() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string s)
                {
                    if (string.IsNullOrEmpty(s)) throw new ArgumentException("bad " + s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_method_that_holds_nothing_but_guards() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Validate(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    if (s.Length == 0) throw new ArgumentException("empty", nameof(s));
                }
            }
            """);

    [Fact]
    public Task Ignores_a_guard_which_comes_after_other_work() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string s)
                {
                    Console.Write(1);
                    ArgumentNullException.ThrowIfNull(s);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_guard_with_an_else_branch_or_a_body_that_does_not_fail() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Else(string s)
                {
                    if (s == null) throw new ArgumentNullException(nameof(s)); else Console.Write(2);
                    Console.Write(1);
                }

                public void Quiet(string s)
                {
                    if (s == null) return;
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_handed_to_a_helper_of_the_users() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Custom(string s)
                {
                    Guard(s);
                    Console.Write(1);
                }

                public void Member(string s)
                {
                    if (s.Trim().Length == 0 || Helper(s)) throw new ArgumentException("bad", nameof(s));
                    Console.Write(1);
                }

                private static void Guard(string value) { if (value == null) throw new ArgumentNullException(nameof(value)); }
                private static bool Helper(string value) => value == "";
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_that_is_stored_and_reports_a_ref_one() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private string stored = "";

                public void Store(string s)
                {
                    if (s == null) throw new ArgumentNullException(nameof(s));
                    stored = s;
                }

                public void Ref(ref int {|FEX0057:n|})
                {
                    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
                    Console.Write(1);
                }

                public void Out(out int n)
                {
                    n = 0;
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_an_expression_body_a_null_coalescing_throw_and_a_method_with_no_guard() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public int Expression(string s) => s?.Length ?? throw new ArgumentNullException(nameof(s));

                public void Discard(string s)
                {
                    _ = s ?? throw new ArgumentNullException(nameof(s));
                    Console.Write(1);
                }

                public void Unused(string s)
                {
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_an_abstract_and_an_extern_style_member() =>
        VerifyAsync("""
            public abstract class C
            {
                public abstract void M(string s);
                public virtual string P => "";
            }
            """);
}
