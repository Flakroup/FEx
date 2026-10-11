using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FEx.Analyzers.Tests;

/// <summary>What each rule of batch 3 puts into its message: the marker tests only prove where it reports, not what it says.</summary>
public class AnalyzerMessageTests
{
    private static Task Verify<TAnalyzer>(string id, string source, params string[] arguments)
        where TAnalyzer : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer, new() =>
        AnalyzerTestHelper.VerifyAsync<TAnalyzer>(
            source,
            expected: [new DiagnosticResult(id, DiagnosticSeverity.Warning).WithLocation(0).WithArguments(arguments)]);

    [Fact]
    public Task FEX0037_names_the_parameter() =>
        Verify<ParameterHidesMemberAnalyzer>("FEX0037", "public class C { private int value; public void M(int {|#0:value|}) { this.value = value; } }", "value");

    [Fact]
    public Task FEX0038_names_the_local() =>
        Verify<LocalVariableHidesMemberAnalyzer>("FEX0038", "public class C { private int count; public int M() { int {|#0:count|} = 1; return count; } public int G() => this.count; }", "count");

    [Fact]
    public Task FEX0041_names_the_method_and_the_optional_parameter() =>
        Verify<MethodOverloadWithOptionalParameterAnalyzer>("FEX0041", "public class C { public void M(int a, {|#0:int b = 0|}) { System.Console.Write(a + b); } public void M(int a) { M(a, 0); } }", "M", "b");

    [Fact]
    public Task FEX0042_names_the_parameter_and_the_member() =>
        Verify<OptionalParameterHierarchyMismatchAnalyzer>("FEX0042", "public class B { public virtual void M(int a = 1) { } } public class D : B { public override void M(int {|#0:a|} = 2) { } }", "a", "D.M(int)");

    [Fact]
    public Task FEX0043_names_the_parameter() =>
        Verify<OptionalParameterRefOutAnalyzer>("FEX0043", "using System.Runtime.InteropServices; public class C { public void M([{|#0:Optional|}] ref int a) { a++; } }", "a");

    [Fact]
    public Task FEX0044_names_the_method() =>
        Verify<PartialMethodWithSinglePartAnalyzer>("FEX0044", "public partial class C { {|#0:partial|} void Only(); }", "Only");

    [Fact]
    public Task FEX0045_names_the_type() =>
        Verify<PartialTypeWithSinglePartAnalyzer>("FEX0045", "public partial class C { {|#0:partial|} class Nested { } }", "Nested");

    [Fact]
    public Task FEX0046_names_the_parameter() =>
        Verify<UnusedParameterInPartialMethodAnalyzer>("FEX0046", "public partial class C { partial void P(int a); } public partial class C { partial void P(int {|#0:a|}) { } public void G() => P(1); }", "a");

    [Fact]
    public Task FEX0047_names_the_member() =>
        Verify<RedundantOverriddenMemberAnalyzer>("FEX0047", "public class B { public virtual void M() { } } public class D : B { public override void {|#0:M|}() { base.M(); } }", "M");

    [Fact]
    public Task FEX0048_names_the_type_parameter() =>
        Verify<UnusedTypeParameterAnalyzer>("FEX0048", "public class C<{|#0:T|}> { }", "T");

    [Fact]
    public Task FEX0049_names_the_method_and_void() =>
        Verify<UnusedMethodReturnValueLocalAnalyzer>("FEX0049", "public class C { private {|#0:int|} Get() => 1; public void M() { Get(); } }", "Get", "void");

    [Fact]
    public Task FEX0049_names_Task_for_an_awaited_Task_of_T() =>
        Verify<UnusedMethodReturnValueLocalAnalyzer>("FEX0049", "using System.Threading.Tasks; public class C { private async {|#0:Task<int>|} Get() { await Task.Yield(); return 1; } public async Task M() { await Get(); } }", "Get", "Task");

    [Fact]
    public Task FEX0049_names_ValueTask_for_an_awaited_ValueTask_of_T() =>
        Verify<UnusedMethodReturnValueLocalAnalyzer>("FEX0049", "using System.Threading.Tasks; public class C { private {|#0:ValueTask<int>|} Get() => new ValueTask<int>(1); public async Task M() { await Get(); } }", "Get", "ValueTask");

    [Fact]
    public Task FEX0050_names_the_accessor() =>
        Verify<ValueParameterNotUsedAnalyzer>("FEX0050", "public class C { private int _p; public int P { get => _p; {|#0:set|} { } } }", "set");

    [Fact]
    public Task FEX0051_names_the_local() =>
        Verify<ConvertToConstantLocalAnalyzer>("FEX0051", "public class C { public int M() { int {|#0:size|} = 5; return size; } }", "size");

    [Fact]
    public Task FEX0052_names_the_sync_and_the_async_method() =>
        Verify<MethodHasAsyncOverloadWithCancellationAnalyzer>("FEX0052", "using System.Threading; using System.Threading.Tasks; public class C { private static void Do() { } private static Task DoAsync(CancellationToken ct) => Task.CompletedTask; public async Task M(CancellationToken ct) { {|#0:Do()|}; await Task.Yield(); System.Console.Write(ct); } }", "Do", "DoAsync");
}
