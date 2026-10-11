using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
        "The value returned by '{0}' is never used; the method can return {1}",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Only calls in the declaring type can exist, so nobody could ever read the result. Replaces the ReSharper inspection UnusedMethodReturnValue.Local.",
        null,
        WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
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
                Record(usages, call.TargetMethod, !IsIgnored(call), call, operation.CancellationToken);
            },
            OperationKind.Invocation);
        context.RegisterOperationAction(operation => Record(usages, ((IMethodReferenceOperation)operation.Operation).Method, true, operation.Operation, operation.CancellationToken), OperationKind.MethodReference);
        context.RegisterOperationAction(
            operation =>
            {
                // nameof(R) uses the method without calling it.
                var argument = ((INameOfOperation)operation.Operation).Argument.Syntax;
                var info = operation.Operation.SemanticModel!.GetSymbolInfo(argument, operation.CancellationToken);
                foreach (var method in info.CandidateSymbols.OfType<IMethodSymbol>())
                    Record(usages, method, true, operation.Operation, operation.CancellationToken);
            },
            OperationKind.NameOf);
        context.RegisterCompilationEndAction(end =>
        {
            var task = ReferencedType(end.Compilation, "System.Threading.Tasks.Task`1");
            var valueTask = ReferencedType(end.Compilation, "System.Threading.Tasks.ValueTask`1");
            foreach (var pair in usages.Where(pair => !pair.Value.Used))
            {
                // Both parts of a partial method are declarations of it. Calls in generated code count as uses, but nobody edits a generated declaration.
                foreach (var reference in new[] { pair.Key, pair.Key.PartialImplementationPart }.OfType<IMethodSymbol>().SelectMany(part => part.DeclaringSyntaxReferences)
                             .Where(reference => !AnalyzerHelpers.IsGenerated(reference.SyntaxTree, pair.Key, end.Options, end.CancellationToken)))
                {
                    var returnType = ((MethodDeclarationSyntax)reference.GetSyntax(end.CancellationToken)).ReturnType;
                    var replacement = ReplacementReturnType(pair.Key.ReturnType, pair.Value.Awaitable, task, valueTask);
                    if (replacement is not null)
                        end.ReportDiagnostic(Diagnostic.Create(Rule, returnType.GetLocation(), pair.Key.Name, replacement));
                }
            }
        });
    }

    // A type of the same name declared in the source being compiled is not the one the advice is about.
    private static INamedTypeSymbol? ReferencedType(Compilation compilation, string metadataName) =>
        compilation.GetTypesByMetadataName(metadataName).FirstOrDefault(type => !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly));

    // Dropping the value of an awaited Task<T> keeps the await: the non-generic task is the replacement, and void would not be awaitable.
    // Any other awaitable (a non-generic Task, a user type, ConfiguredTaskAwaitable<T>) has no replacement that is safe to name: void would
    // break the await or turn the method into an async void, so the rule stays silent.
    private static string? ReplacementReturnType(ITypeSymbol returnType, bool awaitable, INamedTypeSymbol? task, INamedTypeSymbol? valueTask)
    {
        var definition = returnType.OriginalDefinition;
        if (SymbolEqualityComparer.Default.Equals(definition, task))
            return "Task";

        if (SymbolEqualityComparer.Default.Equals(definition, valueTask))
            return "ValueTask";

        return awaitable ? null : "void";
    }

    // The compiler calls these by name (foreach, await, using, deconstruction, collection initializers, ranges, fixed), and what they return is what it needs.
    private static readonly HashSet<string> PatternMethods =
    [
        "GetEnumerator", "GetAsyncEnumerator", "MoveNext", "MoveNextAsync", "GetAwaiter", "GetResult", "Dispose", "DisposeAsync",
        "Deconstruct", "Add", "GetPinnableReference", "Slice",
    ];

    // A method of a source type that no other type can call, whose signature no interface or base class dictates and
    // that is not an extern import, whose return type the native side fixes.
    private static bool IsCandidate(IMethodSymbol method, CancellationToken cancellationToken) =>
        method is { ReturnsVoid: false, IsOverride: false, IsVirtual: false, IsAbstract: false, IsExtern: false }
        && !PatternMethods.Contains(method.Name)
        && method.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax)
        && AnalyzerHelpers.IsEffectivelyPrivate(method)
        && !AnalyzerHelpers.ImplementsInterfaceMember(method)
        && !AnalyzerHelpers.IsImplementedByDerivedType(method);

    private static void Record(ConcurrentDictionary<IMethodSymbol, Usage> usages, IMethodSymbol target, bool used, IOperation site, CancellationToken cancellationToken)
    {
        var method = target.OriginalDefinition;
        if (!IsCandidate(method, cancellationToken))
            return;

        var usage = usages.GetOrAdd(method, _ => new Usage(IsAwaitable(method.ReturnType, site)));
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

    // Awaitable means `await` binds to it: an instance GetAwaiter, or an extension one in scope at the call. Asked at a call because
    // the compilation-end action has no semantic model to look an extension up with.
    private static bool IsAwaitable(ITypeSymbol type, IOperation site) =>
        site.SemanticModel!.LookupSymbols(site.Syntax.SpanStart, type, "GetAwaiter", true).OfType<IMethodSymbol>().Any();

    private sealed class Usage
    {
        public Usage(bool awaitable) => Awaitable = awaitable;

        public bool Used { get; set; }

        public bool Awaitable { get; }
    }
}
