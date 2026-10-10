using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0019: a <c>return;</c>, <c>continue;</c>, <c>yield break;</c> or <c>goto</c> that leaves where control would go anyway.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantJumpStatementAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0019";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant jump statement",
        "This '{0}' is redundant: control continues there without it",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A return or yield break at the end of a function, a continue at the end of a loop body, or a goto to the label that follows it changes nothing. Replaces the ReSharper inspection RedundantJumpStatement.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.ReturnStatement,
            SyntaxKind.ContinueStatement,
            SyntaxKind.YieldBreakStatement,
            SyntaxKind.GotoStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var jump = (StatementSyntax)context.Node;
        var (keyword, label) = Describe(jump);
        if (keyword is not null && IsRedundant(jump, jump.Kind(), label))
            context.ReportDiagnostic(Diagnostic.Create(Rule, jump.GetLocation(), keyword));
    }

    private static (string? Keyword, string? Label) Describe(StatementSyntax jump) => jump switch
    {
        ReturnStatementSyntax { Expression: null } => ("return", null),
        ContinueStatementSyntax => ("continue", null),
        YieldStatementSyntax => ("yield break", null),
        GotoStatementSyntax { Expression: IdentifierNameSyntax target } => ("goto", target.Identifier.ValueText),
        _ => (null, null),
    };

    // Climbs out of every construct the jump is the last thing in, up to the place control lands without it.
    private static bool IsRedundant(StatementSyntax jump, SyntaxKind kind, string? label)
    {
        SyntaxNode node = jump;
        while (true)
        {
            switch (node.Parent)
            {
                case BlockSyntax block:
                    if (Following(block.Statements, (StatementSyntax)node) is { } next)
                        return IsLabel(next, label);
                    node = block;
                    break;
                case SwitchSectionSyntax section:
                    return Following(section.Statements, (StatementSyntax)node) is { } following && IsLabel(following, label);
                case IfStatementSyntax or ElseClauseSyntax or TryStatementSyntax or CatchClauseSyntax
                    or LockStatementSyntax or UsingStatementSyntax or FixedStatementSyntax or CheckedStatementSyntax
                    or UnsafeStatementSyntax or LabeledStatementSyntax:
                    node = node.Parent!;
                    break;
                case ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax:
                    return kind == SyntaxKind.ContinueStatement;
                case BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax:
                    return kind is SyntaxKind.ReturnStatement or SyntaxKind.YieldBreakStatement;
                default:
                    return false;
            }
        }
    }

    private static StatementSyntax? Following(SyntaxList<StatementSyntax> statements, StatementSyntax statement)
    {
        var index = statements.IndexOf(statement);

        return index + 1 < statements.Count ? statements[index + 1] : null;
    }

    // Only a goto can be redundant when something follows: when that something is the label it names.
    private static bool IsLabel(StatementSyntax statement, string? label) =>
        label is not null && statement is LabeledStatementSyntax labeled && labeled.Identifier.ValueText == label;
}
