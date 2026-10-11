using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class OptionalParameterRefOutAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<OptionalParameterRefOutAnalyzer>(source);

    [Fact]
    public Task Reports_R01_oracle_optional_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R01 { public void M([{|FEX0043:Optional|}] ref int a) { a++; } }
            """);

    [Fact]
    public Task Reports_R02_optional_out() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R02 { public void M([{|FEX0043:Optional|}] out int a) { a = 1; } }
            """);

    [Fact]
    public Task Ignores_R03_optional_in() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R03 { public void M([Optional] in int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_R04_optional_default_value_attribute_on() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R04 { public void M([{|FEX0043:Optional|}, DefaultParameterValue(0)] ref int a) { a++; } }
            """);

    [Fact]
    public Task Ignores_R05_optional_by_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R05 { public void M([Optional] int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_R06_default_value_attribute_on_ref_without() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R06 { public void M([DefaultParameterValue(1)] ref int a) { a++; } }
            """);

    [Fact]
    public Task Reports_R08_interface_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public interface R08 { void M([{|FEX0043:Optional|}] ref int a); }
            """);

    [Fact]
    public Task Reports_R10_ctor() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R10 { public R10([{|FEX0043:Optional|}] ref int a) { a++; } }
            """);

    [Fact]
    public Task Ignores_R11_ref_readonly() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R11 { public void M([Optional] ref readonly int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_R12_fully_qualified_attribute() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R12 { public void M([{|FEX0043:System.Runtime.InteropServices.Optional|}] ref int a) { a++; } }
            """);

    [Fact]
    public Task Reports_R13_optional_ref_param_second() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R13 { public void M(int x, [{|FEX0043:Optional|}] ref int a) { a += x; } }
            """);

    [Fact]
    public Task Reports_R14_OptionalAttribute_spelled_with_suffix() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R14 { public void M([{|FEX0043:OptionalAttribute|}] out int a) { a = 1; } }
            """);

    [Fact]
    public Task Ignores_R15_in_with_default_literal() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R15 { public void M(in int a = 1) { Console.Write(a); } }
            """);

    [Fact]
    public Task Reports_R16_delegate_with_optional_ref() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R16 { public delegate void D([{|FEX0043:Optional|}] ref int a); }
            """);

    [Fact]
    public Task Reports_R17_ref_with_default_not_allowed() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R17 { public void M() { void L([{|FEX0043:Optional|}] ref int a) { a++; } int x = 0; L(ref x); } }
            """);

    [Fact]
    public Task Reports_R18_lambda() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class R18 { public void M() { var f = ([{|FEX0043:Optional|}] ref int a) => { a++; }; int x = 0; f(ref x); } }
            """);

    [Fact]
    public Task Reports_RA1_Optional_on_property_indexer() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RA1 { public void M([{|FEX0043:Optional|}] ref int i) { Console.Write(i); } }
            """);

    [Fact]
    public Task Ignores_RA2_DefaultParameterValue_with_Optional_by_value() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RA2 { public void M([Optional, DefaultParameterValue(1)] int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_RA3_Optional_with_params() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RA3 { public void M([Optional] params int[] a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_RA4_Optional_in_partial() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class RA4 { public void M([Optional] ref readonly int a) { Console.Write(a); } }
            """);
}
