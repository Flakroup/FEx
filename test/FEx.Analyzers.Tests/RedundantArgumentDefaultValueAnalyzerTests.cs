using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantArgumentDefaultValueAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantArgumentDefaultValueAnalyzer>(source);

    [Fact]
    public Task Reports_a_trailing_positional_argument_that_equals_the_default() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2) { }
                void Run() { M(1, {|FEX0029:2|}); }
            }
            """);

    [Fact]
    public Task Reports_every_trailing_default_but_not_the_ones_before_a_real_value() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2, int c = 3) { }
                void Both() { M(1, {|FEX0029:2|}, {|FEX0029:3|}); }
                void Last() { M(1, 5, {|FEX0029:3|}); }
                void Middle() { M(1, 2, 5); }
                void First() { M(1, 2, 3 + 1); }
            }
            """);

    [Fact]
    public Task Reports_numeric_constants_that_equal_the_default_in_another_type() =>
        VerifyAsync("""
            class C
            {
                static void Double(double d = 1) { }
                static void Long(long l = 5) { }
                static void Float(float f = 1.5f) { }
                static void Decimal(decimal m = 2) { }
                static void Byte(byte b = 7) { }
                void Run()
                {
                    Double({|FEX0029:1|});
                    Double({|FEX0029:1.0|});
                    Long({|FEX0029:5|});
                    Float({|FEX0029:1.5f|});
                    Decimal({|FEX0029:2|});
                    Byte({|FEX0029:7|});
                }
            }
            """);

    [Fact]
    public Task Reports_a_constant_expression_and_a_named_constant() =>
        VerifyAsync("""
            class C
            {
                private const int Five = 5;
                static void M(int a = 5) { }
                void Run()
                {
                    M({|FEX0029:2 + 3|});
                    M({|FEX0029:Five|});
                }
            }
            """);

    [Fact]
    public Task Reports_strings_booleans_chars_and_enums() =>
        VerifyAsync("""
            enum Mode { A, B }
            class C
            {
                static void Text(string s = "x") { }
                static void Empty(string s = "") { }
                static void Flag(bool f = false) { }
                static void Yes(bool f = true) { }
                static void Letter(char c = 'a') { }
                static void Pick(Mode m = Mode.B) { }
                static void First(Mode m = Mode.A) { }
                static void Zero(Mode m = 0) { }
                void Run()
                {
                    Text({|FEX0029:"x"|});
                    Empty({|FEX0029:""|});
                    Flag({|FEX0029:false|});
                    Yes({|FEX0029:true|});
                    Letter({|FEX0029:'a'|});
                    Pick({|FEX0029:Mode.B|});
                    First({|FEX0029:Mode.A|});
                    Zero({|FEX0029:Mode.A|});
                    Zero({|FEX0029:0|});
                }
            }
            """);

    [Fact]
    public Task Reports_null_for_a_null_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Text(string? s = null) { }
                static void Object(object? o = null) { }
                static void Number(int? i = null) { }
                static void Func(Func<int>? f = null) { }
                void Run()
                {
                    Text({|FEX0029:null|});
                    Object({|FEX0029:null|});
                    Number({|FEX0029:null|});
                    Func({|FEX0029:null|});
                    Text({|FEX0029:default|});
                    Object({|FEX0029:default|});
                }
            }
            """);

    [Fact]
    public Task Reports_the_default_of_a_struct_however_it_is_spelled() =>
        VerifyAsync("""
            using System;
            using System.Threading;
            class C
            {
                static void Time(DateTime t = default) { }
                static void Token(CancellationToken t = default) { }
                static void Number(int n = default) { }
                void Run()
                {
                    Time({|FEX0029:default|});
                    Time({|FEX0029:default(DateTime)|});
                    Time({|FEX0029:new DateTime()|});
                    Token({|FEX0029:default|});
                    Token({|FEX0029:new CancellationToken()|});
                    Number({|FEX0029:default|});
                    Number({|FEX0029:0|});
                }
            }
            """);

    [Fact]
    public Task Reports_the_default_of_a_nullable_and_of_a_type_parameter() =>
        VerifyAsync("""
            class C
            {
                static void Nullable(int? i = null) { }
                static void Generic<T>(T t = default!) { }
                void Run()
                {
                    Nullable({|FEX0029:null|});
                    Generic<string>({|FEX0029:default|});
                }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_an_instance_a_static_and_an_extension_method() =>
        VerifyAsync("""
            static class Ext
            {
                public static void Twice(this int i, int times = 2) { }
            }
            class C
            {
                void Instance(int a = 1) { }
                static void Static(int a = 1) { }
                void Run(int value)
                {
                    Instance({|FEX0029:1|});
                    Static({|FEX0029:1|});
                    this.Instance({|FEX0029:1|});
                    value.Twice({|FEX0029:2|});
                    Ext.Twice(value, {|FEX0029:2|});
                }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_a_generic_method_a_local_function_and_a_lambda() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Generic<T>(T first, int times = 1) { }
                void Run()
                {
                    Generic("a", {|FEX0029:1|});
                    void Local(int a = 1) { }
                    Local({|FEX0029:1|});
                    var lambda = (int a = 1) => a;
                    lambda({|FEX0029:1|});
                }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_through_a_delegate_or_a_null_conditional_call() =>
        VerifyAsync("""
            class C
            {
                delegate void Callback(int a = 1);
                void Handler(int a = 1) { }
                void Run(C? other, Callback callback)
                {
                    callback({|FEX0029:1|});
                    other?.Handler({|FEX0029:1|});
                }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_a_constructor() =>
        VerifyAsync("""
            class C
            {
                public C(int a, int b = 2) { }
                static C Explicit() => new C(1, {|FEX0029:2|});
                static C Implicit() => new(1, {|FEX0029:2|});
                static C Typed() { C c = new(1, {|FEX0029:2|}); return c; }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_a_constructor_initializer() =>
        VerifyAsync("""
            class Base { protected Base(int a, int b = 2) { } }
            class C : Base
            {
                public C() : base(1, {|FEX0029:2|}) { }
                public C(string s) : this(1, {|FEX0029:2|}) { }
                public C(int a, int b = 2) : base(a, {|FEX0029:2|}) { }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_an_attribute() =>
        VerifyAsync("""
            using System;
            [AttributeUsage(AttributeTargets.All)]
            class MarkAttribute : Attribute
            {
                public MarkAttribute(int a, int b = 2) { }
                public string? Name { get; set; }
            }
            [Mark(1, {|FEX0029:2|})]
            class First { }
            [Mark(1, {|FEX0029:2|}, Name = "x")]
            class Second { }
            """);

    [Fact]
    public Task Reports_a_default_passed_to_an_indexer() =>
        VerifyAsync("""
            class C
            {
                public int this[int a, int b = 2] => a + b;
                int Run() => this[1, {|FEX0029:2|}];
            }
            """);

    [Fact]
    public Task Reports_a_default_before_params_when_the_params_are_left_out() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2, params int[] rest) { }
                void Run() { M(1, {|FEX0029:2|}); }
            }
            """);

    [Fact]
    public Task Reports_a_default_passed_before_a_named_argument() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2, int c = 3) { }
                void Run() { M(1, {|FEX0029:2|}, c: 5); }
            }
            """);

    [Fact]
    public Task Ignores_an_argument_that_differs_from_the_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Number(int a = 2) { }
                static void Text(string? s = "x") { }
                static void Time(DateTime t = default) { }
                static void Nullable(int? i = null) { }
                static void Flag(bool f = false) { }
                void Run()
                {
                    Number(3);
                    Text("y");
                    Text(null);
                    Time(DateTime.Now);
                    Nullable(1);
                    Flag(true);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_value_that_is_not_a_constant_even_if_it_happens_to_equal_the_default() =>
        VerifyAsync("""
            using System.Threading;
            class C
            {
                static void Number(int a = 2) { }
                static void Token(CancellationToken t = default) { }
                static void Nullable(int? i = null) { }
                void Run(int variable, CancellationToken token, int? maybe)
                {
                    Number(variable);
                    Token(token);
                    Token(CancellationToken.None);
                    Nullable(maybe);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_value_of_another_type_that_only_looks_like_the_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                static void Text(string? s = null) { }
                static void Time(DateTime t = default) { }
                static void Number(int? n = null) { }
                void Run()
                {
                    Time(new DateTime(1));
                    Number(0);
                    Number(default(int));
                }
            }
            """);

    [Fact]
    public Task Ignores_a_struct_creation_that_is_not_the_plain_default() =>
        VerifyAsync("""
            struct S { public int X; }
            class C
            {
                static void Take(S s = default) { }
                void Run()
                {
                    Take(new S { X = 1 });
                    Take(new S() { X = 1 });
                }
            }
            """);

    [Fact]
    public Task Reports_a_large_floating_point_default() =>
        VerifyAsync("""
            class C
            {
                static void Big(double d = 1e300) { }
                void Run() { Big({|FEX0029:1e300|}); }
            }
            """);

    [Fact]
    public Task Ignores_arguments_that_belong_to_the_params_array() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2, params int[] rest) { }
                void Run() { M(1, 2, 3); M(1, 2, 3, 4); }
            }
            """);

    [Fact]
    public Task Ignores_a_positional_argument_that_follows_a_named_one() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2) { }
                void Run() { M(a: 1, 2); }
            }
            """);

    [Fact]
    public Task Reports_a_default_next_to_a_dynamic_argument() =>
        VerifyAsync("""
            class C
            {
                static void M(object a, int b = 2) { }
                void Run(dynamic d) { M(d, {|FEX0029:2|}); }
            }
            """);

    [Fact]
    public Task Ignores_a_named_argument() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2) { }
                void Run() { M(1, b: 2); M(a: 1, b: 2); }
            }
            """);

    [Fact]
    public Task Ignores_a_default_that_a_later_argument_depends_on() =>
        VerifyAsync("""
            class C
            {
                static void M(int a = 1, int b = 2) { }
                static void N(int a, int b = 2, int c = 3) { }
                void Run() { M(1, 5); N(1, 2, 5); M(b: 5); }
            }
            """);

    [Fact]
    public Task Ignores_an_argument_after_a_named_one_even_when_it_stays_in_position() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b = 2, int c = 3) { }
                void Run() { M(a: 1, b: 2, c: 3); M(1, c: 3, b: 2); }
            }
            """);

    [Fact]
    public Task Ignores_a_parameter_the_callee_fills_from_the_call_site() =>
        VerifyAsync("""
            using System.Runtime.CompilerServices;
            class C
            {
                static void Member([CallerMemberName] string? name = null) { }
                static void File([CallerFilePath] string? path = null) { }
                static void Line([CallerLineNumber] int line = 0) { }
                static void Expression(int value, [CallerArgumentExpression("value")] string? text = null) { }
                void Run()
                {
                    Member(null);
                    File(null);
                    Line(0);
                    Expression(1, null);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_params_array() =>
        VerifyAsync("""
            class C
            {
                static void M(params int[]? rest) { }
                void Run() { M(null); }
            }
            """);

    [Fact]
    public Task Ignores_a_call_whose_overload_would_change_without_the_argument() =>
        VerifyAsync("""
            class C
            {
                static void M(int a) { }
                static void M(int a, int b = 2) { }
                void Run() { M(1, 2); }
            }
            """);

    [Fact]
    public Task Ignores_a_null_conditional_call_whose_overload_would_change_without_the_argument() =>
        VerifyAsync("""
            class C
            {
                void Instance(int a) { }
                void Instance(int a, int b = 2) { }
                void Run(C? other) { other?.Instance(1, 2); }
            }
            """);

    [Fact]
    public Task Ignores_a_null_conditional_call_that_something_is_chained_after() =>
        VerifyAsync("""
            class C
            {
                C Self(int a, int b = 2) => this;
                void Run(C? other) { other?.Self(1, 2).Self(1); }
            }
            """);

    [Fact]
    public Task Ignores_a_constructor_initializer_that_would_pick_another_constructor() =>
        VerifyAsync("""
            class Base
            {
                protected Base(int a) { }
                protected Base(int a, int b = 2) { }
            }
            class C : Base
            {
                public C() : base(1, 2) { }
            }
            """);

    [Fact]
    public Task Ignores_an_attribute_that_would_pick_another_constructor() =>
        VerifyAsync("""
            using System;
            class MarkAttribute : Attribute
            {
                public MarkAttribute(int a) { }
                public MarkAttribute(int a, int b = 2) { }
            }
            [Mark(1, 2)]
            class First { }
            """);

    [Fact]
    public Task Ignores_a_target_typed_creation_that_would_pick_another_constructor() =>
        VerifyAsync("""
            class C
            {
                public C(int a) { }
                public C(int a, int b = 2) { }
                static C M() => new(1, 2);
                static C N() => new C(1, 2);
            }
            """);

    [Fact]
    public Task Ignores_a_call_in_an_expression_tree() =>
        VerifyAsync("""
            using System;
            using System.Linq.Expressions;
            class C
            {
                static int M(int a, int b = 2) => a + b;
                Expression<Func<int>> Run() => () => M(1, 2);
                Expression<Func<int, int>> Nested() => x => M(x, 2);
            }
            """);

    [Fact]
    public Task Reports_a_call_in_a_lambda_that_is_not_an_expression_tree() =>
        VerifyAsync("""
            using System;
            class C
            {
                static int M(int a, int b = 2) => a + b;
                Func<int> Run() => () => M(1, {|FEX0029:2|});
            }
            """);

    [Fact]
    public Task Ignores_a_dynamic_argument_and_a_dynamic_call() =>
        VerifyAsync("""
            class C
            {
                static void M(object? a = null) { }
                static void N(int a, int b = 2) { }
                void Run(dynamic d)
                {
                    M(d);
                    N(1, d);
                    d.Anything(1, 2);
                }
            }
            """);

    [Fact]
    public Task Ignores_a_call_without_arguments_an_array_access_and_a_creation_without_a_list() =>
        VerifyAsync("""
            class C
            {
                public int X { get; set; }
                static void M(int a = 1) { }
                void Run(int[] array)
                {
                    M();
                    _ = array[0];
                    _ = new C { X = 1 };
                }
            }
            """);

    [Fact]
    public Task Ignores_an_attribute_without_an_argument_list() =>
        VerifyAsync("""
            using System;
            [Obsolete]
            class C { }
            """);

    [Fact]
    public Task Ignores_a_parameter_without_a_default() =>
        VerifyAsync("""
            class C
            {
                static void M(int a, int b) { }
                void Run() { M(1, 2); }
            }
            """);

    [Fact]
    public Task Ignores_a_decimal_that_differs_beyond_double_precision() =>
        VerifyAsync("""
            class C
            {
                static void Precise(decimal d = 0.1m) { }
                void Run() { Precise(0.10000000000000000000000001m); }
            }
            """);

    [Fact]
    public Task Ignores_a_nullable_creation_when_the_default_is_a_value() =>
        VerifyAsync("""
            class C
            {
                static void Number(int? n = 5) { }
                void Run() { Number(new int?()); }
            }
            """);

    [Fact]
    public Task Ignores_a_struct_of_another_type_that_converts_to_the_parameter() =>
        VerifyAsync("""
            struct Target { public int X; }
            struct Source { public static implicit operator Target(Source source) => default; }
            class C
            {
                static void Take(Target t = default) { }
                void Run() { Take(new Source()); }
            }
            """);

    [Fact]
    public Task Ignores_a_struct_creation_that_runs_a_declared_constructor() =>
        VerifyAsync("""
            struct Declared { public Declared() { } public int X; }
            class C
            {
                static void Take(Declared d = default) { }
                void Run() { Take(new Declared()); }
            }
            """);

    [Fact]
    public Task Ignores_an_indexer_call_whose_overload_would_change_without_the_argument() =>
        VerifyAsync("""
            class C
            {
                public int this[int a] => a;
                public int this[int a, int b = 2] => b;
                int Run() => this[1, 2];
            }
            """);
}
