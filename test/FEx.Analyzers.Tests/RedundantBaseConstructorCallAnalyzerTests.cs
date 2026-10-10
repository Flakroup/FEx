using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantBaseConstructorCallAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantBaseConstructorCallAnalyzer>(source);

    [Fact]
    public Task Reports_an_empty_base_call_on_object_and_on_a_class() =>
        VerifyAsync("""
            class Base { }
            class First { public First() {|FEX0036:: base()|} { } }
            class Second : Base { public Second() {|FEX0036:: base()|} { } }
            """);

    [Fact]
    public Task Reports_an_empty_base_call_when_the_base_has_more_constructors() =>
        VerifyAsync("""
            class Base
            {
                public Base() { }
                public Base(int a) { }
            }
            class Optional
            {
                public Optional(int a = 0) { }
            }
            class First : Base { public First() {|FEX0036:: base()|} { } }
            class Second : Optional { public Second() {|FEX0036:: base()|} { } }
            """);

    [Fact]
    public Task Reports_an_empty_base_call_to_a_protected_or_params_constructor() =>
        VerifyAsync("""
            abstract class Abstract { protected Abstract() { } }
            class Params { public Params(params int[] rest) { } }
            class First : Abstract { public First() {|FEX0036:: base()|} { } }
            class Second : Params { public Second() {|FEX0036:: base()|} { } }
            """);

    [Fact]
    public Task Reports_an_empty_base_call_on_a_constructor_with_parameters_and_a_body() =>
        VerifyAsync("""
            using System;
            class Base { }
            class Generic<T> { }
            class First : Base
            {
                private int _x;
                public First(int a) {|FEX0036:: base()|} { _x = a; }
            }
            class Second : Generic<int> { public Second() {|FEX0036:: base()|} { } }
            class Third : InvalidOperationException { public Third() {|FEX0036:: base()|} { } }
            class Outer { class Nested : Base { public Nested() {|FEX0036:: base()|} { } } }
            """);

    [Fact]
    public Task Reports_an_empty_base_call_of_a_primary_constructor() =>
        VerifyAsync("""
            class Base { }
            class First(int a) : Base{|FEX0036:()|} { }
            """);

    [Fact]
    public Task Ignores_a_base_call_that_passes_arguments() =>
        VerifyAsync("""
            class Base
            {
                protected Base(int a) { }
            }
            class First : Base
            {
                public First(int a) : base(a) { }
                public First() : base(1) { }
            }
            class Second(int a) : Base(a) { }
            """);

    [Fact]
    public Task Ignores_a_call_to_this() =>
        VerifyAsync("""
            class First
            {
                public First() { }
                public First(int a) : this() { }
            }
            struct Second
            {
                public Second(int a) : this() { }
            }
            """);

    [Fact]
    public Task Ignores_a_constructor_without_an_initializer_and_a_base_type_without_a_list() =>
        VerifyAsync("""
            class Base { }
            class First : Base { public First() { } }
            class Second(int a) : Base { }
            """);
}
