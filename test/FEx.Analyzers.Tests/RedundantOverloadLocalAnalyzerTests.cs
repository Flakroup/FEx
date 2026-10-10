using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantOverloadLocalAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantOverloadLocalAnalyzer>(source);

    [Fact]
    public Task Reports_E10_private_nested_class_public_forwarder() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public class E10 { private class In { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0040:M|}(int a) { M(a, 0); } } public void G() => new In().M(1); }
            """);

    [Fact]
    public Task Ignores_M16_call_reaches_the_base_class_method() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M16B { public void M(int a, int b = 0) { Console.Write(a + b); } }
            public class M16 : M16B { public void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M17_async_void_forwarder() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M17 { public void M(int a, int b = 0) { Console.Write(a + b); } public async void M(int a) { M(a, 0); } }
            """);

    [Fact]
    public Task Ignores_M18_this_call_with_explicit_type_argument() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class M18 { public void M<T>(int a, int b = 0) { Console.Write(a + b); } public void M<T>(int a) { this.M<T>(a, 0); } }
            """);

    [Fact]
    public Task Reports_O10_private() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class O10 { private void M(int a, int b = 0) { Console.Write(a + b); } private void {|FEX0040:M|}(int a) { M(a, 0); } public void G() { M(1); M(1, 2); } }
            """);

    [Fact]
    public Task Reports_OA1_private_ctor_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA1 { private OA1(int a, int b = 0) { Console.Write(a + b); } private {|FEX0040:OA1|}(int a) : this(a, 0) { } public static OA1 Make() => new OA1(1); }
            """);

    [Fact]
    public Task Reports_OA23_private_public_mix_forward() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA23 { public void M(int a, int b = 0) { Console.Write(a + b); } private void {|FEX0040:M|}(int a) { M(a, 0); } public void G() => M(1); }
            """);

    [Fact]
    public Task Reports_OA25_private_in_private_nested_class_public() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class OA25 { private class In { public void M(int a, int b = 0) { Console.Write(a + b); } public void {|FEX0040:M|}(int a) { M(a, 0); } } public object G() => new In(); }
            """);
}
