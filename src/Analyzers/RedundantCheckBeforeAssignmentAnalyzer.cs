using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0034: <c>if (x != y) x = y;</c>, where the assignment is as cheap as the comparison guarding it.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantCheckBeforeAssignmentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0034";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant check before assignment",
        "The check is redundant: assigning the value gives the same result as comparing it first",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "'if (x != y) x = y;' leaves x equal to y whether or not the check runs. Replaces the ReSharper inspection RedundantCheckBeforeAssignment.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var statement = (IfStatementSyntax)context.Node;

        if (statement.Else is not null
            || statement.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression } condition
            || SingleAssignment(statement.Statement) is not { } assignment
            || !MatchesEitherWay(condition, assignment))
            return;

        // A property setter may do more than store, so skipping it is a behaviour change; a field, a local and an indexer cannot.
        if (context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol { IsIndexer: false })
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
    }

    private static AssignmentExpressionSyntax? SingleAssignment(StatementSyntax body)
    {
        if (body is BlockSyntax { Statements: [var only] })
            body = only;

        return body is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment }
            ? assignment
            : null;
    }

    private static bool MatchesEitherWay(BinaryExpressionSyntax condition, AssignmentExpressionSyntax assignment) =>
        SyntaxFactory.AreEquivalent(condition.Left, assignment.Left) && SyntaxFactory.AreEquivalent(condition.Right, assignment.Right)
        || SyntaxFactory.AreEquivalent(condition.Right, assignment.Left) && SyntaxFactory.AreEquivalent(condition.Left, assignment.Right);
}
