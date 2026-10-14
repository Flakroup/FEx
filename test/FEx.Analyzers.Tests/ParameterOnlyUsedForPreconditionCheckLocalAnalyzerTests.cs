using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class ParameterOnlyUsedForPreconditionCheckLocalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<ParameterOnlyUsedForPreconditionCheckLocalAnalyzer>(source);

    [Fact]
    public Task Reports_a_parameter_of_a_private_method_read_only_by_Debug_Assert() =>
        VerifyAsync("""
            using System;
            using System.Diagnostics;
            public class C
            {
                private void M(string {|FEX0058:s|})
                {
                    Debug.Assert(s.Length > 0);
                    Console.Write(1);
                }

                public void G() => M("a");
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_private_method_read_only_by_a_throwing_guard() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private static int M(int {|FEX0058:n|})
                {
                    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
                    return 1;
                }

                public int G() => M(2);
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_method_of_a_private_nested_type() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private class Inner
                {
                    public void M(string {|FEX0058:s|})
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }

                public void G() => new Inner().M("a");
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_method_of_a_sealed_class_that_is_protected() =>
        VerifyAsync("""
            using System;
            public sealed class C
            {
                protected void M(string {|FEX0058:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_constructor_whoever_can_call_it() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public C(string {|FEX0058:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_constructor_which_does_nothing_but_validate() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public C(string {|FEX0058:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_block_lambda() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public Action<string> M() => {|FEX0058:s|} =>
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                };
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_local_function() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M()
                {
                    Local("a");

                    void Local(string {|FEX0058:s|})
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_public_method_which_is_the_global_rule() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_private_method_that_holds_nothing_but_guards() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private void Validate(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    if (s.Length == 0) throw new ArgumentException("empty", nameof(s));
                }

                public void G() => Validate("a");
            }
            """);

    [Fact]
    public Task Ignores_a_local_function_that_holds_nothing_but_guards() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M()
                {
                    Validate("a");

                    void Validate(string s)
                    {
                        ArgumentNullException.ThrowIfNull(s);
                    }
                }
            }
            """);

    [Fact]
    public Task Ignores_an_explicit_interface_implementation() =>
        VerifyAsync("""
            using System;
            public interface IThing { void Do(string s); }
            public class C : IThing
            {
                void IThing.Do(string s)
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
                private void M(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(s);
                }

                public void G() => M("a");
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_a_nested_lambda_reads() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private Func<int> M(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    return () => s.Length + new Random().Next();
                }

                public Func<int> G() => M("a");
            }
            """);

    [Fact]
    public Task Ignores_a_guard_which_comes_after_other_work() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private void M(string s)
                {
                    Console.Write(1);
                    ArgumentNullException.ThrowIfNull(s);
                }

                public void G() => M("a");
            }
            """);

    [Fact]
    public Task Ignores_a_guard_made_by_a_helper_of_the_users() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private void M(string s)
                {
                    Guard(s);
                    Console.Write(1);
                }

                private static void Guard(string value) { if (value == null) throw new ArgumentNullException(nameof(value)); }

                public void G() => M("a");
            }
            """);

    [Fact]
    public Task Ignores_an_expression_bodied_lambda_and_an_expression_body() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private int M(string s) => s?.Length ?? throw new ArgumentNullException(nameof(s));

                public Func<string, int> L() => s => s.Length > 0 ? 1 : throw new ArgumentException("empty", nameof(s));

                public int G() => M("a");
            }
            """);

    [Fact]
    public Task Reports_a_ref_parameter_the_body_only_checks() =>
        VerifyAsync("""
            using System;
            public class C
            {
                private void M(ref int {|FEX0058:n|})
                {
                    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
                    Console.Write(1);
                }

                public void G() { var n = 1; M(ref n); }
            }
            """);

    [Fact]
    public Task Reports_a_block_lambda_which_does_nothing_but_validate() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public Action<string> M() => {|FEX0058:s|} =>
                {
                    ArgumentNullException.ThrowIfNull(s);
                };
            }
            """);

    [Fact]
    public Task Ignores_a_void_expression_bodied_lambda_that_validates() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public Action<string> M() => s => ArgumentNullException.ThrowIfNull(s);
            }
            """);

    [Fact]
    public Task Ignores_a_protected_override_of_a_sealed_class() =>
        VerifyAsync("""
            using System;
            public class Base { protected virtual void Run(string s) { Console.Write(s); } }
            public sealed class C : Base
            {
                protected override void Run(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_method_of_a_private_nested_type_that_a_derived_type_re_lists_an_interface_with() =>
        VerifyAsync("""
            using System;
            public class Outer
            {
                private interface IThing { void Do(string s); }
                private class A
                {
                    public void Do(string s)
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }
                private class B : A, IThing { }
            }
            """);

    [Fact]
    public Task Ignores_a_private_method_some_code_takes_as_a_method_group() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                private void Check(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Hook(List<string> items) => items.ForEach(Check);
            }
            """);

    [Fact]
    public Task Ignores_a_local_function_some_code_takes_as_a_method_group() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public void Hook(List<string> items)
                {
                    items.ForEach(Check);

                    void Check(string s)
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_local_function_that_is_only_called() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Hook()
                {
                    Check("a");

                    void Check(string {|FEX0058:s|})
                    {
                        ArgumentNullException.ThrowIfNull(s);
                        Console.Write(1);
                    }
                }
            }
            """);

    [Fact]
    public Task Ignores_a_generic_method_some_code_takes_as_a_method_group() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                private void Check<T>(T s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Hook(List<string> items) => items.ForEach(Check);
            }
            """);
}
