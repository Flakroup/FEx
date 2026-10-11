using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class UnusedParameterInPartialMethodAnalyzerTests
{
    private static Task VerifyAsync(string source, LanguageVersion? languageVersion = null, string[]? otherFiles = null) =>
        AnalyzerTestHelper.VerifyAsync<UnusedParameterInPartialMethodAnalyzer>(source, languageVersion: languageVersion, otherFiles: otherFiles);

    [Fact]
    public Task Ignores_M33_partial_constructor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class M33 { partial M33(int a); }
            public partial class M33 { partial M33(int a) { } }
            """, languageVersion: LanguageVersion.Preview);

    [Fact]
    public Task Reports_U01_oracle_unused_param_in_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U01 { partial void P(int a); }
            public partial class U01 { partial void P(int {|FEX0046:a|}) { } public void G() => P(1); }
            """);

    [Fact]
    public Task Ignores_U02_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U02 { partial void P(int a); }
            public partial class U02 { partial void P(int a) { Console.Write(a); } public void G() => P(1); }
            """);

    [Fact]
    public Task Ignores_U03_decl_only_unused_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U03 { partial void P(int a); public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_U04_differently_named_impl_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U04 { partial void P(int a); }
            public partial class U04 { partial void P(int {|FEX0046:b|}) { } public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_U05_two_params_one_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U05 { partial void P(int a, int b); }
            public partial class U05 { partial void P(int a, int {|FEX0046:b|}) { Console.Write(a); } public void G() => P(1, 2); }
            """);

    [Fact]
    public Task Reports_U06_extended_partial_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U06 { public partial int P(int a); }
            public partial class U06 { public partial int P(int {|FEX0046:a|}) => 1; }
            """);

    [Fact]
    public Task Reports_U07_nameof_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U07 { partial void P(int a); }
            public partial class U07 { partial void P(int {|FEX0046:a|}) { Console.Write(nameof(a)); } public void G() => P(1); }
            """);

    [Fact]
    public Task Ignores_U08_used_in_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U08 { partial void P(int a); }
            public partial class U08 { partial void P(int a) { Action x = () => Console.Write(a); x(); } public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_U09_impl_never_called_unused_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U09 { partial void P(int a); }
            public partial class U09 { partial void P(int {|FEX0046:a|}) { } }
            """);

    [Fact]
    public Task Ignores_U10_out_parameter_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U10 { private partial void P(out int a); }
            public partial class U10 { private partial void P(out int a) { a = 1; } }
            """);

    [Fact]
    public Task Ignores_U11_non_partial_method_unused_param_UnusedParameter_not() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class U11 { public void P(int a) { } }
            """);

    [Fact]
    public Task Reports_U12_unused_param_in_partial_method_of() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial struct U12 { partial void P(int a); }
            public partial struct U12 { partial void P(int {|FEX0046:a|}) { } public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_U13_unused_params_on_partial_no_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U13 { private partial void P(int a, string s); }
            public partial class U13 { private partial void P(int a, string {|FEX0046:s|}) { Console.Write(a); } public void G() => P(1, ""); }
            """);

    [Fact]
    public Task Reports_U14_partial_method_impl_with_expression_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U14 { public partial int P(int a); }
            public partial class U14 { public partial int P(int {|FEX0046:a|}) => 5; }
            """);

    [Fact]
    public Task Ignores_U16_impl_param_reassigned_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U16 { partial void P(int a); }
            public partial class U16 { partial void P(int a) { a = 1; } public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_U17_impl_throws() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class U17 { public partial void P(int a); }
            public partial class U17 { public partial void P(int {|FEX0046:a|}) { throw new NotImplementedException(); } }
            """);

    [Fact]
    public Task Reports_UA1_ref_param_unused() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class UA1 { partial void P(ref int a); }
            public partial class UA1 { partial void P(ref int {|FEX0046:a|}) { } public void G() { int x = 0; P(ref x); } }
            """);

    [Fact]
    public Task Reports_UA3_partial_with_generic_type_param() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class UA3 { partial void P<T>(T a); }
            public partial class UA3 { partial void P<T>(T {|FEX0046:a|}) { } public void G() => P(1); }
            """);

    [Fact]
    public Task Ignores_UA4_param_used_in_expression_body() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class UA4 { public partial int P(int a); }
            public partial class UA4 { public partial int P(int a) => a; }
            """);

    [Fact]
    public Task Reports_UA5_param_used_only_as_nameof_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class UA5 { partial void P(int a); }
            public partial class UA5 { partial void P(int {|FEX0046:a|}) { Console.Write(1); } public void G() => P(1); }
            """);

    [Fact]
    public Task Reports_UA7_param_named_like_field() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class UA7 { private int a; partial void P(int a); public int G() => a; }
            public partial class UA7 { partial void P(int {|FEX0046:a|}) { Console.Write(this.a); } }
            """);

    [Fact]
    public Task Ignores_extern_implementation() =>
        VerifyAsync("""
            using System.Runtime.InteropServices;
            public partial class E { private static partial int Beep(int f); [DllImport("kernel32")] private static extern partial int Beep(int f); public int G() => Beep(1); }
            """);

    [Fact]
    public Task Ignores_discard_named_parameters() =>
        VerifyAsync("""
            public partial class Q { partial void A(int _); partial void A(int _) { } partial void B(int _1); partial void B(int _1) { } public void G() { A(1); B(1); } }
            """);
}
