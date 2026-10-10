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

    // The tests below pin the verdicts of ReSharper 2026.2.3.1 (MemberHidesStaticFromOuterClass at ERROR), probed
    // one pairing per class.

    [Fact]
    public Task Reports_every_pairing_ReSharper_reports() =>
        VerifyAsync("""
            using System;
            using System.Collections.Generic;
            class Probes
            {
                class SameSignature { public static int Do(int a) => a; public class Inner { public int {|FEX0004:Do|}(int a) => 1; } }
                class OtherParameterName { public static int Do(int a) => a; public class Inner { public int {|FEX0004:Do|}(int b) => 1; } }
                class OtherReturnType { public static int Do(int a) => a; public class Inner { public string {|FEX0004:Do|}(int a) => ""; } }
                class VoidReturn { public static int Do(int a) => a; public class Inner { public void {|FEX0004:Do|}(int a) { } } }
                class BothStatic { public static int Do(int a) => a; public class Inner { public static int {|FEX0004:Do|}(int a) => 1; } }
                class BothGeneric { public static int Do<T>(T a) => 1; public class Inner { public int {|FEX0004:Do|}<U>(U a) => 1; } }
                class GenericOverConstructed { public static int Do<T>(List<T> a) => 1; public class Inner { public int {|FEX0004:Do|}<U>(List<U> a) => 1; } }
                class PropertyVsMethod { public static int Name(int a) => a; public class Inner { public int {|FEX0004:Name|} { get; set; } } }
                class FieldVsMethod { public static int Value() => 1; public class Inner { public int {|FEX0004:Value|}; } }
                class MethodVsField { public static int Value; public class Inner { public void {|FEX0004:Value|}() { } } }
                class StaticMethodVsField { public static int Value; public class Inner { public static int {|FEX0004:Value|}() => 1; } }
                class FieldVsProperty { public static int Value { get; set; } public class Inner { public int {|FEX0004:Value|}; } }
                class FieldVsEvent { public static event Action? Ev; public class Inner { public int {|FEX0004:Ev|}; } }
                class MethodVsDelegateField { public static readonly Func<int, int> Do = a => a; public class Inner { public int {|FEX0004:Do|}(int a) => 1; } }
                class TypeVsField { public static int Value; public class Inner { public class {|FEX0004:Value|} { } } }
                class ConstOuter { public const int Value = 1; public class Inner { public int {|FEX0004:Value|}; } }
                class PrivateOuter { private static int Value; public class Inner { public int {|FEX0004:Value|}; } }
            }
            """);

    [Fact]
    public Task Ignores_two_methods_whose_signatures_differ() =>
        VerifyAsync("""
            using System.Collections.Generic;
            class Probes
            {
                class MoreParameters { public static int Do(int a) => a; public class Inner { public void Do(int a, int b) { } } }
                class FewerParameters { public static int Do(int a, int b) => a; public class Inner { public void Do(string s) { } } }
                class OtherType { public static int Do(int a) => a; public class Inner { public int Do(string s) => 1; } }
                class WiderType { public static int Do(int a) => a; public class Inner { public int Do(long a) => 1; } }
                class ByRef { public static int Do(int a) => a; public class Inner { public void Do(ref int a) { } } }
                class BaseOverDerived { public static int Do(object a) => 1; public class Inner { public int Do(string a) => 1; } }
                class DerivedOverBase { public static int Do(string a) => 1; public class Inner { public int Do(object a) => 1; } }
                class OptionalParameter { public static int Do(int a, int b = 0) => a; public class Inner { public int Do(int a) => 1; } }
                class GenericOuter { public static int Do<T>(T a) => 1; public class Inner { public int Do(int a) => 1; } }
                class GenericNested { public static int Do(int a) => a; public class Inner { public void Do<T>(T a) { } } }
                class OtherTypeArguments { public static int Do<T>(List<T> a) => 1; public class Inner { public int Do<U>(List<int> a) => 1; } }
            }
            """);

    [Fact]
    public Task Reports_once_when_the_outer_type_has_two_static_overloads() =>
        VerifyAsync("""
            class Outer
            {
                public static void Do(int a) { }
                public static void Do(string s) { }
                class Inner { public void {|FEX0004:Do|}(int a) { } }
            }
            """);

    // ReSharper 2026.2.3.1 marks both halves of a partial method or property and only the first part of a partial type.
    [Fact]
    public Task Reports_both_parts_of_a_partial_method_and_property_and_the_first_part_of_a_partial_type() =>
        VerifyAsync("""
            class Outer
            {
                public static void Do() { }
                public static int Count { get; set; }
                public static int Twin;
                partial class Inner
                {
                    public static partial void {|FEX0004:Do|}();
                    public static partial int {|FEX0004:Count|} { get; set; }
                    public partial class {|FEX0004:Twin|} { }
                }
                partial class Inner
                {
                    public static partial void {|FEX0004:Do|}() { }
                    public static partial int {|FEX0004:Count|} { get => 0; set { } }
                    public partial class Twin { }
                }
            }
            """);

    [Fact]
    public Task Ignores_members_of_an_extension_block() =>
        AnalyzerTestHelper.VerifyAsync<MemberHidesStaticFromOuterClassAnalyzer>(
            """
            static class Ext
            {
                public static int Count;
                public static void Run() { }
                extension(string s)
                {
                    public int Count2 => s.Length;
                    public int Count => s.Length;
                    public void Run(int a) { }
                }
            }
            """,
            languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
}
