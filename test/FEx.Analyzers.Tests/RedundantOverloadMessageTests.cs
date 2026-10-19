using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantOverloadMessageTests
{
    [Fact]
    public Task Global_names_the_class_for_a_constructor() =>
        AnalyzerTestHelper.VerifyAsync<RedundantOverloadGlobalAnalyzer>(
            """
            public class Box { public Box(int a, int b = 0) { System.Console.Write(a + b); } public {|#0:Box|}(int a) : this(a, 0) { } }
            """,
            expected: [new DiagnosticResult("FEX0039", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Box")]);

    [Fact]
    public Task Global_names_the_method() =>
        AnalyzerTestHelper.VerifyAsync<RedundantOverloadGlobalAnalyzer>(
            """
            public class Box { public void Put(int a, int b = 0) { System.Console.Write(a + b); } public void {|#0:Put|}(int a) { Put(a, 0); } }
            """,
            expected: [new DiagnosticResult("FEX0039", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Put")]);

    [Fact]
    public Task Local_names_the_class_for_a_constructor() =>
        AnalyzerTestHelper.VerifyAsync<RedundantOverloadLocalAnalyzer>(
            """
            public class Box { private Box(int a, int b = 0) { System.Console.Write(a + b); } private {|#0:Box|}(int a) : this(a, 0) { } }
            """,
            expected: [new DiagnosticResult("FEX0040", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Box")]);

    [Fact]
    public Task Local_names_the_method() =>
        AnalyzerTestHelper.VerifyAsync<RedundantOverloadLocalAnalyzer>(
            """
            public class Box { private void Put(int a, int b = 0) { System.Console.Write(a + b); } private void {|#0:Put|}(int a) { Put(a, 0); } }
            """,
            expected: [new DiagnosticResult("FEX0040", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Put")]);
}
