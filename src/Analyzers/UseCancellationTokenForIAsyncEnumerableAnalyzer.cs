using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0053: an <c>await foreach</c> over an <c>IAsyncEnumerable</c> ignores the <c>CancellationToken</c> that is in scope.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCancellationTokenForIAsyncEnumerableAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0053";

    private const string AsyncEnumerableMetadataName = "System.Collections.Generic.IAsyncEnumerable`1";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use a cancellation token for IAsyncEnumerable",
        "The iteration ignores the CancellationToken in scope; pass it with 'WithCancellation'",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Cancelling the token then does not stop the enumeration. Replaces the ReSharper inspection UseCancellationTokenForIAsyncEnumerable.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.GetTypeByMetadataName(CancellationScope.TokenMetadataName) is { } token
                && start.Compilation.GetTypeByMetadataName(AsyncEnumerableMetadataName) is { } asyncEnumerable)
                start.RegisterOperationAction(operation => Analyze(operation, token, asyncEnumerable), OperationKind.Loop);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol token, INamedTypeSymbol asyncEnumerable)
    {
        if (context.Operation is not IForEachLoopOperation loop)
            return;

        // `ConfigureAwait` keeps the sequence, only a `WithCancellation` result carries a token.
        var source = Unwrap(loop.Collection);
        if (!SymbolEqualityComparer.Default.Equals((source.Type as INamedTypeSymbol)?.OriginalDefinition, asyncEnumerable)
            || source is IInvocationOperation call && PassesToken(call, token)
            || IsLocalFromCall(source, context.CancellationToken)
            || !CancellationScope.HasTokenInScope(loop, context.ContainingSymbol, token))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, loop.Collection.Syntax.GetLocation()));
    }

    private static IOperation Unwrap(IOperation collection)
    {
        var source = WithoutConversions(collection);
        if (source is not IInvocationOperation { TargetMethod: { IsExtensionMethod: true, Name: "ConfigureAwait", ContainingType.Name: "TaskAsyncEnumerableExtensions" } } call)
            return source;

        return WithoutConversions(call.Arguments[0].Value);
    }

    // A sequence kept in a local was made by the call that initialized it, where the token belonged; ReSharper looks no further than that.
    private static bool IsLocalFromCall(IOperation source, CancellationToken cancellationToken) =>
        source is ILocalReferenceOperation { Local.DeclaringSyntaxReferences: [{ } declaration] }
        && declaration.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Initializer.Value: InvocationExpressionSyntax };

    private static IOperation WithoutConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;

        return operation;
    }

    // A token handed to the producing call, whichever it is, or a parameter the compiler fills with the one passed to GetAsyncEnumerator.
    private static bool PassesToken(IInvocationOperation call, INamedTypeSymbol token) =>
        call.Arguments.Any(argument => argument.ArgumentKind == ArgumentKind.Explicit && CancellationScope.IsToken(argument.Value.Type!, token))
        || call.TargetMethod.Parameters.Any(parameter => parameter.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "EnumeratorCancellationAttribute"));
}
