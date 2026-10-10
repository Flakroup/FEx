using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class MemberHidesStaticFromOuterClassAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<MemberHidesStaticFromOuterClassAnalyzer>(source);

    [Fact]
    public Task Reports_a_field_that_hides_a_static_field() =>
        VerifyAsync("class Outer { public static int Value; public class Inner { public int {|FEX0004:Value|}; } }");

    [Fact]
    public Task Reports_a_property_a_method_and_an_event_that_hide_static_members() =>
        VerifyAsync("""
            using System;
            class Outer
            {
                public static int Count { get; set; }
                public static void Run() { }
                public static event EventHandler? Changed;
                class Inner
                {
                    public int {|FEX0004:Count|} { get; set; }
                    public void {|FEX0004:Run|}() { }
                    public event EventHandler? {|FEX0004:Changed|};
                }
            }
            """);

    [Fact]
    public Task Reports_a_nested_type_and_a_member_of_a_deeper_nested_type() =>
        VerifyAsync("""
            class Outer
            {
                public const int Limit = 3;
                public static string Name = "";
                class Inner
                {
                    public class {|FEX0004:Limit|} { }
                    public class Deepest { public string {|FEX0004:Name|}; }
                }
            }
            """);

    [Fact]
    public Task Reports_a_member_of_a_nested_struct_and_names_a_static_method_of_the_nearest_outer_type() =>
        VerifyAsync("""
            class Outer
            {
                public static void Build() { }
                struct Inner { public int {|FEX0004:Build|}; }
            }
            """);

    [Fact]
    public Task Ignores_an_outer_member_that_is_not_static() =>
        VerifyAsync("class Outer { public int Value; public void Run() { } class Inner { public int Value; public void Run() { } } }");

    [Fact]
    public Task Ignores_a_different_name() =>
        VerifyAsync("class Outer { public static int Value; class Inner { public int Other; } }");

    [Fact]
    public Task Ignores_a_member_of_a_top_level_type_and_a_nested_type_itself() =>
        VerifyAsync("class Outer { public static int Value; public class Value2 { } } class Other { public int Value; }");

    [Fact]
    public Task Ignores_overrides_enum_members_and_explicit_implementations() =>
        VerifyAsync("""
            interface IHasName { string Name { get; } }
            class Base { public virtual int Value => 0; }
            class Outer
            {
                public static int Value;
                public static string Name = "";
                public static int Red;
                class Inner : Base, IHasName
                {
                    public override int Value => 1;
                    string IHasName.Name => "";
                }
                enum Color { Red }
            }
            """);

    [Fact]
    public Task Ignores_accessors_indexers_constructors_and_a_static_nested_type_named_alike() =>
        VerifyAsync("""
            class Outer
            {
                public static int Item;
                public static int get_Value() => 0;
                static class Helper { }
                class Inner
                {
                    public int Value { get; set; }
                    public int this[int i] => i;
                    public Inner() { }
                    public int Helper;
                }
            }
            """);
}
