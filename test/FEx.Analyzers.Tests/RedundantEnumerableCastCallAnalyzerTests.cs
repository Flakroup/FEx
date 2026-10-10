using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantEnumerableCastCallAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantEnumerableCastCallAnalyzer>(source);

    [Fact]
    public Task Reports_Cast_and_OfType_to_the_element_type() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> Cast(IEnumerable<int> e) => e.{|FEX0027:Cast<int>|}();
                IEnumerable<int> OfType(IEnumerable<int> e) => e.{|FEX0027:OfType<int>|}();
                IEnumerable<string> Strings(IEnumerable<string> e) => e.{|FEX0027:Cast<string>|}();
                IEnumerable<object> Objects(IEnumerable<object> e) => e.{|FEX0027:OfType<object>|}();
            }
            """);

    [Fact]
    public Task Reports_the_static_call_form() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C { IEnumerable<int> M(IEnumerable<int> e) => Enumerable.{|FEX0027:Cast<int>|}(e); }
            """);

    [Fact]
    public Task Reports_a_sequence_that_is_a_collection_an_array_or_a_string() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> List(List<int> l) => l.{|FEX0027:Cast<int>|}();
                IEnumerable<int> Array(int[] a) => a.{|FEX0027:Cast<int>|}();
                IEnumerable<int> Interface(IList<int> l) => l.{|FEX0027:Cast<int>|}();
                IEnumerable<char> String(string s) => s.{|FEX0027:Cast<char>|}();
                IEnumerable<int> Values(Dictionary<string, int> d) => d.Values.{|FEX0027:Cast<int>|}();
                IEnumerable<KeyValuePair<string, int>> Pairs(Dictionary<string, int> d) => d.{|FEX0027:Cast<KeyValuePair<string, int>>|}();
            }
            """);

    [Fact]
    public Task Reports_in_a_chain_and_in_a_foreach() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int Chain(IEnumerable<int> e) => e.Where(i => i > 0).{|FEX0027:Cast<int>|}().Count();
                void Loop(IEnumerable<int> e) { foreach (var i in e.{|FEX0027:Cast<int>|}()) { } }
            }
            """);

    [Fact]
    public Task Reports_a_type_parameter() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C { IEnumerable<T> M<T>(IEnumerable<T> e) => e.{|FEX0027:Cast<T>|}(); }
            """);

    [Fact]
    public Task Ignores_a_cast_that_changes_the_element_type() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<long> Long(IEnumerable<int> e) => e.Cast<long>();
                IEnumerable<int> OfTypeFilter(IEnumerable<object> e) => e.OfType<int>();
                IEnumerable<object> Boxing(IEnumerable<int> e) => e.Cast<object>();
                IEnumerable<IComparable> Interface(IEnumerable<int> e) => e.Cast<IComparable>();
                IEnumerable<int> Unboxing(IEnumerable<int?> e) => e.Cast<int>();
                IEnumerable<int?> Lifting(IEnumerable<int> e) => e.Cast<int?>();
            }
            """);

    [Fact]
    public Task Ignores_a_cast_to_a_base_type_because_the_use_decides_whether_it_matters() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class Base { }
            class Derived : Base { }
            class C
            {
                IEnumerable<object> Objects(IEnumerable<string> e) => e.Cast<object>();
                IEnumerable<Base> Bases(IEnumerable<Derived> e) => e.Cast<Base>();
                IEnumerable<Derived> Narrowing(IEnumerable<Base> e) => e.Cast<Derived>();
            }
            """);

    [Fact]
    public Task Ignores_a_non_generic_sequence() =>
        VerifyAsync("""
            using System.Collections;
            using System.Linq;
            class C
            {
                object Cast(IEnumerable e) => e.Cast<int>();
                object Matrix(int[,] m) => m.Cast<int>();
            }
            """);

    [Fact]
    public Task Ignores_a_sequence_that_enumerates_two_element_types() =>
        VerifyAsync("""
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;
            class Both : IEnumerable<int>, IEnumerable<string>
            {
                IEnumerator<int> IEnumerable<int>.GetEnumerator() => null!;
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => null!;
                IEnumerator IEnumerable.GetEnumerator() => null!;
            }
            class C { object M(Both b) => b.Cast<int>(); }
            """);

    [Fact]
    public Task Ignores_nullable_annotations_that_differ() =>
        VerifyAsync("""
            #nullable enable
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<string> Cast(IEnumerable<string?> e) => e.Cast<string>();
                IEnumerable<string> OfType(IEnumerable<string?> e) => e.OfType<string>();
            }
            """);

    [Fact]
    public Task Ignores_a_null_conditional_call() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C { IEnumerable<int>? M(IEnumerable<int>? e) => e?.Cast<int>(); }
            """);

    [Fact]
    public Task Ignores_Cast_and_OfType_that_are_not_Enumerable_methods() =>
        VerifyAsync("""
            using System.Collections.Generic;
            static class Own
            {
                public static IEnumerable<T> Cast<T>(this IEnumerable<T> e) => e;
            }
            class Plain { public IEnumerable<int> OfType<T>() => new int[0]; }
            class C
            {
                IEnumerable<int> Extension(IEnumerable<int> e) => e.Cast<int>();
                IEnumerable<int> Member(Plain p) => p.OfType<int>();
            }
            """);

    [Fact]
    public Task Ignores_other_Enumerable_methods() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C { IEnumerable<int> M(IEnumerable<int> e) => e.Select(i => i).Where(i => i > 0); }
            """);

    [Fact]
    public Task Ignores_a_source_that_is_a_type_parameter_constrained_to_a_sequence() =>
        VerifyAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C { IEnumerable<int> M<TList>(TList list) where TList : IEnumerable<int> => list.Cast<int>(); }
            """);
}
