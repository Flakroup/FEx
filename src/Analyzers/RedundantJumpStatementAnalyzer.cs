using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0019: a <c>return;</c>, <c>continue;</c>, <c>break;</c>, <c>yield break;</c> or <c>goto</c> that leaves where control would go anyway.</summary>
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
        "A return or yield break at the end of a function, a continue at the end of a loop body, a goto to the label that follows it, or a jump that the same jump follows changes nothing. A yield break that is the only yield of the function is kept, since it is what makes the function an iterator. Replaces the ReSharper inspection RedundantJumpStatement.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.ReturnStatement,
            SyntaxKind.ContinueStatement,
            SyntaxKind.BreakStatement,
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
        BreakStatementSyntax => ("break", null),
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
                        return IsLabel(next, label) || IsSameJump(next, kind);
                    node = block;
                    break;
                case SwitchSectionSyntax section:
                    return Following(section.Statements, (StatementSyntax)node) is { } following && (IsLabel(following, label) || IsSameJump(following, kind));
                case IfStatementSyntax or ElseClauseSyntax or TryStatementSyntax or CatchClauseSyntax
                    or LockStatementSyntax or UsingStatementSyntax or FixedStatementSyntax or CheckedStatementSyntax
                    or UnsafeStatementSyntax or LabeledStatementSyntax:
                    node = node.Parent!;
                    break;
                case ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax:
                    return kind == SyntaxKind.ContinueStatement;
                case BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax:
                    return kind == SyntaxKind.ReturnStatement || kind == SyntaxKind.YieldBreakStatement && HasYieldReturn(node.Parent!);
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

    // Without a 'yield return' the function is an iterator only because of its 'yield break', so that one cannot go.
    private static bool HasYieldReturn(SyntaxNode function) =>
        function.DescendantNodes(node => node is not LocalFunctionStatementSyntax || node == function)
            .Any(node => node.IsKind(SyntaxKind.YieldReturnStatement));

    // Climbing never leaves a loop or a switch, so a following jump of the same kind has the same target and the first one is idle.
    // A goto names its own label, so it is judged by IsLabel alone.
    private static bool IsSameJump(StatementSyntax statement, SyntaxKind kind) =>
        kind != SyntaxKind.GotoStatement && statement.IsKind(kind);

    // A goto is redundant when what follows is the label it names.
    private static bool IsLabel(StatementSyntax statement, string? label) =>
        label is not null && statement is LabeledStatementSyntax labeled && labeled.Identifier.ValueText == label;
}
