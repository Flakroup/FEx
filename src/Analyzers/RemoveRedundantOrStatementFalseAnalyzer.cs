using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0033: the statement <c>flag |= false;</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveRedundantOrStatementFalseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0033";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant or-assignment of false",
        "The statement is redundant: or-ing a boolean with 'false' leaves it unchanged",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "'flag |= false;' assigns the value flag already has. Replaces the ReSharper inspection RemoveRedundantOrStatement.False.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ExpressionStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var statement = (ExpressionStatementSyntax)context.Node;

        // Only a plain bool is reported; ReSharper stays silent on bool?.
        if (statement.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.OrAssignmentExpression, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.FalseLiteralExpression } } assignment
            && context.SemanticModel.GetTypeInfo(assignment.Left).Type?.SpecialType == SpecialType.System_Boolean)
            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
    }
}
