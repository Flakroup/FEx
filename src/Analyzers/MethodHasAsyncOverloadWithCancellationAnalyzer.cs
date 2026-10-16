using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0052: an async function calls a synchronous method that has an <c>Async</c> twin taking a <c>CancellationToken</c>, and a token is at hand.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodHasAsyncOverloadWithCancellationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0052";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Method has async overload with cancellation",
        "'{0}' blocks the thread although '{1}' accepts the CancellationToken that is in scope",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "The synchronous call can neither be cancelled nor give the thread back while it waits. Replaces the ReSharper inspection MethodHasAsyncOverloadWithCancellation.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.GetTypeByMetadataName(CancellationScope.TokenMetadataName) is { } token)
                start.RegisterOperationAction(operation => Analyze(operation, token), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol token)
    {
        var invocation = (IInvocationOperation)context.Operation;
        // The generic definition, so that the type parameters of the two overloads can be told apart from their arguments.
        var sync = invocation.TargetMethod.ConstructedFrom;
        if (!IsInAsyncFunction(invocation, context.ContainingSymbol)
            || FindAsyncOverload(context, sync, token) is not { } overload
            || SymbolEqualityComparer.Default.Equals(context.ContainingSymbol, overload)
            || !CancellationScope.HasTokenInScope(invocation, context.ContainingSymbol, token))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), sync.Name, overload.Name));
    }

    // The function that runs the call, not the method around a lambda: a synchronous lambda in an async method is not async.
    private static bool IsInAsyncFunction(IOperation operation, ISymbol containingSymbol)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IAnonymousFunctionOperation function:
                    return function.Symbol.IsAsync;
                case ILocalFunctionOperation local:
                    return local.Symbol.IsAsync;
                // An await cannot stand in a lock.
                case ILockOperation:
                    return false;
            }
        }

        return containingSymbol is IMethodSymbol { IsAsync: true };
    }

    // Looked up on the type that declares the method, not on the type of the receiver nor on its base types.
    private static IMethodSymbol? FindAsyncOverload(OperationAnalysisContext context, IMethodSymbol sync, INamedTypeSymbol token) =>
        sync.ContainingType.GetMembers(sync.Name + "Async").OfType<IMethodSymbol>()
            .FirstOrDefault(candidate => AddsOnlyAToken(candidate, sync, token) && context.Compilation.IsSymbolAccessibleWithin(candidate, context.ContainingSymbol.ContainingType));

    private static bool AddsOnlyAToken(IMethodSymbol candidate, IMethodSymbol sync, INamedTypeSymbol token) =>
        candidate.Parameters.Length == sync.Parameters.Length + 1
        && ReturnsSameResult(sync, candidate)
        && Enumerable.Range(0, candidate.Parameters.Length).Any(index =>
            candidate.Parameters[index] is { RefKind: RefKind.None } parameter
            && CancellationScope.IsToken(parameter.Type, token)
            && candidate.Parameters.RemoveAt(index).Zip(sync.Parameters, (left, right) =>
                left.RefKind == right.RefKind && AnalyzerHelpers.SameType(left.Type, right.Type)).All(same => same));

    // What the async twin delivers once awaited - nothing for Task, ValueTask and void, T for Task<T> and ValueTask<T> - is what the sync method returns.
    private static bool ReturnsSameResult(IMethodSymbol sync, IMethodSymbol candidate) =>
        candidate.ReturnType switch
        {
            INamedTypeSymbol { ContainingNamespace: { Name: "Tasks", ContainingNamespace.Name: "Threading" }, Name: "Task" or "ValueTask", IsGenericType: false } => sync.ReturnsVoid,
            INamedTypeSymbol { ContainingNamespace: { Name: "Tasks", ContainingNamespace.Name: "Threading" }, Name: "Task" or "ValueTask", TypeArguments: [var result] } => SymbolEqualityComparer.Default.Equals(sync.ReturnType, result),
            var other => other.SpecialType == SpecialType.System_Void && sync.ReturnsVoid,
        };
}
