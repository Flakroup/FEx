using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0049: a method no other type can call returns a value, and every call in its type throws that value away.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedMethodReturnValueLocalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0049";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Unused method return value (local)",
        "The value returned by '{0}' is never used; the method can return void",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Only calls in the declaring type can exist, so nobody could ever read the result. Replaces the ReSharper inspection UnusedMethodReturnValue.Local.",
        null,
        WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    private static void Start(CompilationStartAnalysisContext context)
    {
        // Operations of nested types do not reach an action registered for their outer type, so the whole compilation is one scope.
        var usages = new ConcurrentDictionary<IMethodSymbol, Usage>(SymbolEqualityComparer.Default);
        context.RegisterOperationAction(
            operation =>
            {
                var call = (IInvocationOperation)operation.Operation;
                Record(usages, call.TargetMethod, !IsIgnored(call));
            },
            OperationKind.Invocation);
        context.RegisterOperationAction(operation => Record(usages, ((IMethodReferenceOperation)operation.Operation).Method, true), OperationKind.MethodReference);
        context.RegisterOperationAction(
            operation =>
            {
                // nameof(R) uses the method without calling it.
                var argument = ((INameOfOperation)operation.Operation).Argument.Syntax;
                var info = operation.Operation.SemanticModel!.GetSymbolInfo(argument, operation.CancellationToken);
                foreach (var method in info.CandidateSymbols.OfType<IMethodSymbol>())
                    Record(usages, method, true);
            },
            OperationKind.NameOf);
        context.RegisterCompilationEndAction(end =>
        {
            foreach (var pair in usages.Where(pair => !pair.Value.Used))
            {
                // Both parts of a partial method are declarations of it.
                foreach (var syntax in new[] { pair.Key, pair.Key.PartialImplementationPart }.OfType<IMethodSymbol>().SelectMany(part => part.DeclaringSyntaxReferences))
                    end.ReportDiagnostic(Diagnostic.Create(Rule, ((MethodDeclarationSyntax)syntax.GetSyntax(end.CancellationToken)).ReturnType.GetLocation(), pair.Key.Name));
            }
        });
    }

    // A method of a source type that no other type can call, whose signature no interface or base class dictates and
    // that is not an extern import, whose return type the native side fixes.
    private static bool IsCandidate(IMethodSymbol method) =>
        method is { ReturnsVoid: false, IsOverride: false, IsVirtual: false, IsAbstract: false, IsExtern: false }
        && method.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is MethodDeclarationSyntax)
        && AnalyzerHelpers.IsEffectivelyPrivate(method)
        && !AnalyzerHelpers.ImplementsInterfaceMember(method);

    private static void Record(ConcurrentDictionary<IMethodSymbol, Usage> usages, IMethodSymbol target, bool used)
    {
        var method = target.OriginalDefinition;
        if (!IsCandidate(method))
            return;

        var usage = usages.GetOrAdd(method, _ => new Usage());
        lock (usage)
            usage.Used |= used;
    }

    // A call is ignored when nothing reads its result: a statement of its own, the step of a for loop, the result of an
    // awaited generic task, or a `?.` call on a statement.
    private static bool IsIgnored(IInvocationOperation invocation)
    {
        IOperation current = invocation;
        while (current.Parent is IInvocationOperation { TargetMethod.Name: "ConfigureAwait" })
            current = current.Parent;

        // The operation tree has no parentheses; ReSharper reads a parenthesized call as a value in use.
        if (current.Syntax.Parent is ParenthesizedExpressionSyntax)
            return false;

        if (current.Parent is IAwaitOperation await)
            return invocation.TargetMethod.ReturnType is INamedTypeSymbol { IsGenericType: true } && await.Parent is IExpressionStatementOperation;

        if (current.Parent is IConditionalAccessOperation access && access.WhenNotNull == current)
            current = access;

        return current.Parent is IExpressionStatementOperation;
    }

    private sealed class Usage
    {
        public bool Used { get; set; }
    }
}
