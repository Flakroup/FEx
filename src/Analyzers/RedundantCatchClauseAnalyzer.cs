using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0035: a <c>catch</c> clause whose whole body is <c>throw;</c> and that no later clause depends on being shadowed.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantCatchClauseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0035";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant catch clause",
        "The catch clause is redundant: it only rethrows, and no later clause would catch what it lets through",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A catch clause that does nothing but 'throw;' behaves as if absent, unless it keeps a later clause from seeing the exception. Replaces the ReSharper inspection RedundantCatchClause.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TryStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var catches = ((TryStatementSyntax)context.Node).Catches;
        for (var i = 0; i < catches.Count; i++)
        {
            if (IsPlainRethrow(catches[i]) && !ShadowsLaterClause(catches, i, context.SemanticModel))
                context.ReportDiagnostic(Diagnostic.Create(Rule, catches[i].GetLocation()));
        }
    }

    private static bool IsPlainRethrow(CatchClauseSyntax clause) =>
        clause.Filter is null && clause.Block.Statements is [ThrowStatementSyntax { Expression: null }];

    /// <summary>Whether a clause after <paramref name="index"/> would catch what this one catches, and so only sees it because this one rethrows.</summary>
    private static bool ShadowsLaterClause(SyntaxList<CatchClauseSyntax> catches, int index, SemanticModel model)
    {
        var caught = CaughtType(catches[index], model);
        for (var i = index + 1; i < catches.Count; i++)
        {
            // A later rethrow is redundant itself, so it takes nothing over.
            if (IsPlainRethrow(catches[i]))
                continue;

            var later = CaughtType(catches[i], model);
            if (caught is null || later is null || IsSameOrDerived(caught, later))
                return true;
        }

        return false;
    }

    /// <summary>The caught type, or <c>null</c> for a bare <c>catch</c> or one that cannot be resolved: either is taken to catch anything.</summary>
    private static ITypeSymbol? CaughtType(CatchClauseSyntax clause, SemanticModel model) =>
        clause.Declaration is { } declaration && model.GetTypeInfo(declaration.Type).Type is { TypeKind: not TypeKind.Error } type ? type : null;

    private static bool IsSameOrDerived(ITypeSymbol type, ITypeSymbol candidateBase)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, candidateBase))
                return true;
        }

        return false;
    }
}
