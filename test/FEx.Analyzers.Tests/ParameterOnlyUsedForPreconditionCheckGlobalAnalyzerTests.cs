using System.Threading.Tasks;
using FEx.Analyzers.Tests.Fakes;
using Microsoft.CodeAnalysis;
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

    [Fact]
    public Task Ignores_a_virtual_method_an_override_and_an_interface_implementation() =>
        VerifyAsync("""
            using System;
            public interface IThing { void Do(string s); }
            public interface IOther { void Other(string s); }
            public class Base { public virtual void Run(string s) { Console.Write(s); } }
            public class C : Base, IThing, IOther
            {
                public override void Run(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public virtual void Open(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Do(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                void IOther.Other(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_of_a_user_defined_operator_and_conversion() =>
        VerifyAsync("""
            using System;
            public class Op
            {
                public static Op operator +(Op a, Op {|FEX0057:b|})
                {
                    ArgumentNullException.ThrowIfNull(b);
                    return a;
                }

                public static explicit operator Op(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    return new Op();
                }
            }
            """);

    [Fact]
    public Task Ignores_a_method_some_code_takes_as_a_method_group_or_an_event_handler() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public event Action<string>? Raised;

                public void Check(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void OnRaised(string s)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Hook(List<string> items)
                {
                    items.ForEach(Check);
                    Raised += OnRaised;
                }
            }
            """);

    [Fact]
    public Task Reports_a_method_that_is_only_called() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Check(string {|FEX0057:s|})
                {
                    ArgumentNullException.ThrowIfNull(s);
                    Console.Write(1);
                }

                public void Call() => Check("a");
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_through_the_pure_types() =>
        VerifyAsync("""
            using System;
            using System.Linq;
            public class C
            {
                public void String(string {|FEX0057:s|}) { if (s.Length > 3 || s.Trim() == "") throw new ArgumentException(); Console.Write(1); }
                public void Math(int {|FEX0057:n|}) { if (System.Math.Abs(n) > 3) throw new ArgumentException(); Console.Write(1); }
                public void Char(char {|FEX0057:c|}) { if (char.IsDigit(c)) throw new ArgumentException(); Console.Write(1); }
                public void Object(object {|FEX0057:o|}) { if (o.Equals(1) || o.GetHashCode() == 0) throw new ArgumentException(); Console.Write(1); }
                public void Array(int[] {|FEX0057:a|}) { if (a.Length > 3 || System.Array.IndexOf(a, 1) > 0) throw new ArgumentException(); Console.Write(1); }
                public void Type(Type {|FEX0057:t|}) { if (t.IsEnum) throw new ArgumentException(); Console.Write(1); }
                public void Nullable(int? {|FEX0057:n|}) { if (!n.HasValue) throw new ArgumentException(); Console.Write(1); }
                public void Enumerable(System.Collections.Generic.IEnumerable<int> {|FEX0057:items|}) { if (items.Any()) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Reports_a_collection_read_through_its_named_members() =>
        VerifyAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;
            public class C
            {
                public void Contains(List<int> {|FEX0057:l|}) { if (l.Contains(1)) throw new ArgumentException(); Console.Write(1); }
                public void NonGeneric(ArrayList {|FEX0057:l|}) { if (l.Contains(1)) throw new ArgumentException(); Console.Write(1); }
                public void Keys(Dictionary<int, int> {|FEX0057:d|}) { if (d.ContainsKey(1) || d.ContainsValue(1) || d.Keys.Count > 0 || d.Values.Count > 0) throw new ArgumentException(); Console.Write(1); }
                public void Index(List<int> {|FEX0057:l|}) { if (l.IndexOf(1) > 0 || l.LastIndexOf(1) > 0 || l[0] > 1) throw new ArgumentException(); Console.Write(1); }
                public void Predicates(List<int> {|FEX0057:l|}) { if (l.Exists(x => x > 1) || l.TrueForAll(x => x > 1)) throw new ArgumentException(); Console.Write(1); }
                public void Peek(Stack<int> {|FEX0057:s|}) { if (s.Peek() > 1 || s.Count > 1) throw new ArgumentException(); Console.Write(1); }
                public void AsIndex(List<int> {|FEX0057:l|}, int {|FEX0057:i|}) { if (l[i] > 1) throw new ArgumentException(); Console.Write(1); }
                public void AsArrayIndex(int[] {|FEX0057:a|}, int {|FEX0057:i|}) { if (a[i] > 1) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_collection_the_guard_changes() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public void Remove(List<int> l) { if (!l.Remove(1)) throw new ArgumentException(); Console.Write(1); }
                public void Add(HashSet<int> s) { if (!s.Add(1)) throw new ArgumentException(); Console.Write(1); }
                public void Pop(Stack<int> s) { if (s.Pop() > 1) throw new ArgumentException(); Console.Write(1); }
                public void Dequeue(Queue<int> q) { if (q.Dequeue() > 1) throw new ArgumentException(); Console.Write(1); }
                public void Write(List<int> l) { if (l.Count > 0 && l.RemoveAll(x => x > 1) > 0) throw new ArgumentException(); Console.Write(1); }
                public void Assign(List<int> l) { if ((l[0] = 1) > 0) throw new ArgumentException(); Console.Write(1); }
                public void Capacity(List<int> l) { if ((l.Capacity = 1) > 0) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_collection_type_of_the_users_in_a_system_namespace() =>
        VerifyAsync("""
            using System;
            namespace System.Collections.Generic
            {
                public class Mine { public bool Contains(int x) { Console.Write(x); return true; } }
            }
            public class C
            {
                public void M(System.Collections.Generic.Mine m) { if (m.Contains(1)) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_that_a_pattern_or_an_out_argument_carries_on() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public int Declaration(object o) { if (o is not string s) throw new ArgumentException(); return s.Length; }
                public int Recursive(string s) { if (s is not { Length: var n }) throw new ArgumentException(); return n; }
                public int Designation(object o) { if (o is not string { Length: > 0 } t) throw new ArgumentException(); return t.Length; }
                public int List(int[] a) { if (a is not [_, .. var rest]) throw new ArgumentException(); return rest.Length; }
                public int Designated(int[] a) { if (a is not [1, 2] list) throw new ArgumentException(); return list.Length; }
                public int Out(Dictionary<string, int> d, string key) { if (!d.TryGetValue(key, out var v)) throw new ArgumentException(); return v; }
                public char Parse(string s) { if (!char.TryParse(s, out var c)) throw new ArgumentException(); return c; }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_a_pattern_tests_without_binding_anything() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void Property(string {|FEX0057:s|}) { if (s is not { Length: > 0 }) throw new ArgumentException(); Console.Write(1); }
                public void Discard(object {|FEX0057:o|}) { if (o is not string _) throw new ArgumentException(); Console.Write(1); }
                public void Type(object {|FEX0057:o|}) { if (o is string) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Counts_a_parameter_a_lambda_in_the_guard_captures_as_used() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public void M(List<int> {|FEX0057:items|}, int limit)
                {
                    if (items.Exists(x => x > limit)) throw new ArgumentException();
                    Console.Write(1);
                }

                public void Own(List<int> {|FEX0057:items|})
                {
                    if (items.Exists(x => x > 3)) throw new ArgumentException();
                    Console.Write(1);
                }

                public void Delegate(List<int> {|FEX0057:items|}, Predicate<int> {|FEX0057:limit|})
                {
                    if (items.Exists(limit)) throw new ArgumentException();
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_read_through_the_operators_and_operands_that_only_read() =>
        VerifyAsync("""
            using System;
            public struct Money
            {
                public int V;
                public static bool operator ==(Money a, Money b) => a.V == b.V;
                public static bool operator !=(Money a, Money b) => a.V != b.V;
                public static explicit operator int(Money m) => m.V;
                public static Money operator -(Money m) => m;
                public override bool Equals(object? o) => false;
                public override int GetHashCode() => 0;
            }
            public class C
            {
                public void Binary(Money {|FEX0057:m|}) { if (m == default) throw new ArgumentException(); Console.Write(1); }
                public void Conversion(Money {|FEX0057:m|}) { if ((int)m > 3) throw new ArgumentException(); Console.Write(1); }
                public void Unary(Money {|FEX0057:m|}) { if (-m == default) throw new ArgumentException(); Console.Write(1); }
                public void IsType(object {|FEX0057:o|}) { if (o is string) throw new ArgumentException(); Console.Write(1); }
                public void Conditional(bool {|FEX0057:b|}) { if ((b ? 1 : 0) > 3) throw new ArgumentException(); Console.Write(1); }
                public void Coalesce(string? {|FEX0057:s|}) { if ((s ?? "").Length > 3) throw new ArgumentException(); Console.Write(1); }
                public void Access(string? {|FEX0057:s|}) { if (s?.Length > 3) throw new ArgumentException(); Console.Write(1); }
                public void Chain(string? {|FEX0057:s|}) { if (s?.Trim()?.Length > 3) throw new ArgumentException(); Console.Write(1); }
                public void Array(int[] {|FEX0057:a|}) { if (a[0] > 3) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_a_conditional_access_changes() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            public class C
            {
                public void Remove(List<int>? l) { if (l?.Remove(1) == true) throw new ArgumentException(); Console.Write(1); }
                public void Chain(List<int>? l) { if (l?.ToArray()?.Length == 0 || l?.Remove(1) == true) throw new ArgumentException(); Console.Write(1); }

                public void Nested(List<int>? {|FEX0057:l|}, List<int>? q)
                {
                    if (l?.Contains(q?.Remove(1) == true ? 1 : 0) == true) throw new ArgumentException();
                    Console.Write(1);
                }
            }
            """);

    [Fact]
    public Task Reports_a_parameter_when_Contract_Assert_or_EndContractBlock_guards() =>
        VerifyAsync("""
            using System;
            using System.Diagnostics.Contracts;
            public class C
            {
                public void Assert(int {|FEX0057:n|}) { Contract.Assert(n > 0); Console.Write(1); }
                public void End(int {|FEX0057:n|}, int other) { Contract.EndContractBlock(); if (n < 0) throw new ArgumentException(); Console.Write(other); }
                public void Disposed(bool {|FEX0057:disposed|}) { ObjectDisposedException.ThrowIf(disposed, typeof(C)); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_throw_helper_that_is_neither_the_frameworks_nor_an_exceptions() =>
        AnalyzerTestHelper.VerifyAsync<ParameterOnlyUsedForPreconditionCheckGlobalAnalyzer>(
            """
            using System;
            using FEx.Analyzers.Tests.Fakes;
            public class MyException : ArgumentException
            {
                public static void ThrowIfBad(int n) { if (n < 0) throw new MyException(); }
            }
            public class C
            {
                public void Library(int n) { OutsideGuard.ThrowIfBad(n); Console.Write(1); }
                public void Source(int n) { MyException.ThrowIfBad(n); Console.Write(1); }
            }
            """,
            references: [MetadataReference.CreateFromFile(typeof(OutsideGuard).Assembly.Location)]);

    [Fact]
    public Task Ignores_a_parameter_that_is_never_used_and_one_a_guard_does_not_read() =>
        VerifyAsync("""
            using System;
            public class C
            {
                public void M(string unused, int n)
                {
                    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
                    Console.Write(n);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_read_through_a_member_of_the_users_own_type() =>
        VerifyAsync("""
            using System;
            public class Named { public string? Name { get; set; } }
            public class C
            {
                public void M(Named n) { if (n.Name is null) throw new ArgumentException(); Console.Write(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_a_chain_of_conditional_accesses_changes() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            public class C
            {
                public void M(Dictionary<string, HashSet<int>>? d) { if (d?.Values.FirstOrDefault()?.Add(1) == null) throw new ArgumentException(); Console.Write(1); }
            }
            """);
}
