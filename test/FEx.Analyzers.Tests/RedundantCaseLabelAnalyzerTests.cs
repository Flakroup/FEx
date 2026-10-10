using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantCaseLabelAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantCaseLabelAnalyzer>(source);

    [Fact]
    public Task Reports_a_case_label_beside_default_in_either_order() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i) { switch (i) { {|FEX0018:case 2:|} default: Console.Write(1); break; } }
                void B(int i) { switch (i) { default: {|FEX0018:case 2:|} Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Reports_every_case_label_of_the_section() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i) { switch (i) { {|FEX0018:case 1:|} {|FEX0018:case 2:|} default: Console.Write(1); break; } }
                void B(int i) { switch (i) { {|FEX0018:case 1:|} default: {|FEX0018:case 2:|} Console.Write(1); break; } }
                void D(string s) { switch (s) { {|FEX0018:case "a":|} {|FEX0018:case "b":|} default: Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Reports_pattern_labels_without_a_filter() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(object o) { switch (o) { {|FEX0018:case int n:|} default: Console.Write(1); break; } }
                void B(object o) { switch (o) { {|FEX0018:case string:|} default: Console.Write(1); break; } }
                void D(object o) { switch (o) { {|FEX0018:case null:|} default: Console.Write(1); break; } }
                void E(int i) { switch (i) { {|FEX0018:case > 5:|} default: Console.Write(1); break; } }
                void F(object o) { switch (o) { {|FEX0018:case int:|} {|FEX0018:case string:|} default: Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_label_with_a_when_filter() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i) { switch (i) { case 1 when i > 0: {|FEX0018:case 2:|} default: Console.Write(1); break; } }
                void B(object o) { switch (o) { case int n when n > 0: default: Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_var_pattern_label() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(object o) { switch (o) { case var x: default: Console.Write(1); break; } }
            }
            """);

    [Fact]
    public Task Keeps_a_label_that_a_goto_case_targets() =>
        VerifyAsync("""
            using System;
            enum E { A, B }
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); goto case 2;
                        case 2: default: Console.Write(2); break;
                    }
                }

                void B(int i)
                {
                    switch (i)
                    {
                        case 1: goto case 1 + 1;
                        case 2: default: break;
                    }
                }

                void D(E e)
                {
                    switch (e)
                    {
                        case E.A: goto case E.B;
                        case E.B: default: break;
                    }
                }

                void F(int i)
                {
                    switch (i)
                    {
                        case 1: goto case 2;
                        case (2): default: break;
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_label_when_the_goto_case_names_another_value() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        case 1: Console.Write(1); goto case 3;
                        {|FEX0018:case 2:|} default: Console.Write(2); break;
                        case 3: break;
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_label_that_only_a_nested_switch_jumps_to() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i, int j)
                {
                    switch (i)
                    {
                        case 1:
                            switch (j)
                            {
                                case 1: goto case 2;
                                case 2: break;
                            }
                            break;
                        {|FEX0018:case 2:|} default: Console.Write(2); break;
                    }
                }
            }
            """);

    [Fact]
    public Task Reports_a_label_whose_neighbour_is_a_goto_default() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i)
                {
                    switch (i)
                    {
                        {|FEX0018:case 2:|} default: Console.Write(1); goto default;
                    }
                }
            }
            """);

    [Fact]
    public Task Keeps_the_labels_of_an_enum_switch() =>
        VerifyAsync("""
            using System;
            enum Mode { None, A, B }
            class C
            {
                void A(Mode m) { switch (m) { case Mode.A: Console.Write(1); break; case Mode.None: default: Console.Write(2); break; } }
                void B(Mode m) { switch (m) { default: case Mode.None: Console.Write(2); break; } }
                void D(Mode m) { switch (m) { case Mode.A: case Mode.B: default: Console.Write(2); break; } }
                void E(Mode? m) { switch (m) { case Mode.A: default: Console.Write(2); break; } }
                void F(Mode m) { switch (m) { case 0: default: Console.Write(2); break; } }
            }
            """);

    [Fact]
    public Task Reports_labels_of_other_constant_types() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(char c) { switch (c) { case 'a': Console.Write(1); break; {|FEX0018:case 'b':|} default: Console.Write(2); break; } }
                void B(bool b) { switch (b) { {|FEX0018:case true:|} default: Console.Write(2); break; } }
            }
            """);

    [Fact]
    public Task Ignores_sections_without_both_kinds_of_label() =>
        VerifyAsync("""
            using System;
            class C
            {
                void A(int i) { switch (i) { default: Console.Write(1); break; } }
                void B(int i) { switch (i) { case 1: case 2: Console.Write(1); break; default: Console.Write(2); break; } }
                void D(int i) { switch (i) { case 1: Console.Write(1); break; } }
            }
            """);
}
