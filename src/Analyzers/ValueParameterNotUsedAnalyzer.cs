using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0050: a <c>set</c>, <c>init</c>, <c>add</c> or <c>remove</c> accessor with a body that never reads <c>value</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueParameterNotUsedAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0050";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Value parameter is not used",
        "The 'value' of the '{0}' accessor is never used",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Whatever the caller assigns is silently dropped. Overrides, interface implementations and accessors that only throw are exempt. Replaces the ReSharper inspection ValueParameterNotUsed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var accessor = (AccessorDeclarationSyntax)context.Node;
        if (accessor.Body is null && accessor.ExpressionBody is null
            || accessor.ExpressionBody?.Expression is ThrowExpressionSyntax
            || accessor.Body is { Statements: [ThrowStatementSyntax] }
            || UsesValue(context, accessor))
            return;

        var member = context.SemanticModel.GetDeclaredSymbol(accessor.Parent!.Parent!, context.CancellationToken)!;
        // An override or an implementation may leave the value alone on purpose, but only by saying nothing at all.
        if (accessor.Body is not { Statements.Count: 0 } || !member.IsOverride && !AnalyzerHelpers.ImplementsInterfaceMember(member))
            context.ReportDiagnostic(Diagnostic.Create(Rule, accessor.Keyword.GetLocation(), accessor.Keyword.ValueText));
    }

    private static bool UsesValue(SyntaxNodeAnalysisContext context, AccessorDeclarationSyntax accessor) =>
        accessor.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier =>
            context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol is IParameterSymbol { IsImplicitlyDeclared: true });
}
