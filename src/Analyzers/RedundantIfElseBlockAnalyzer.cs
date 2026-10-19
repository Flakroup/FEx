using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0020: an <c>else</c> after a branch that never completes, so the else body could follow the <c>if</c> instead.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantIfElseBlockAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0020";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant else",
        "This 'else' is redundant: the 'if' branch never completes normally",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "When the if branch always returns, throws or jumps, the else body runs exactly when it would without the else keyword. Replaces the ReSharper inspection RedundantIfElseBlock.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        if (statement.Else is { } @else && Exits(context.SemanticModel, statement.Statement) && InStatementList(context.SemanticModel, statement))
            context.ReportDiagnostic(Diagnostic.Create(Rule, @else.ElseKeyword.GetLocation()));
    }

    private static bool Exits(SemanticModel model, StatementSyntax branch) =>
        model.AnalyzeControlFlow(branch) is { EndPointIsReachable: false };

    // Dropping the else moves its body to after the if, which only reads the same where the if sits in a statement list: not as the body
    // of a loop, using or lock, nor as the then-branch of another if. Inside an else-if chain every earlier branch must exit as well, or
    // control would reach the moved body from them. ReSharper stays silent on all of those.
    private static bool InStatementList(SemanticModel model, IfStatementSyntax statement)
    {
        while (true)
        {
            switch (statement.Parent)
            {
                case BlockSyntax or SwitchSectionSyntax or LabeledStatementSyntax or GlobalStatementSyntax:
                    return true;
                case ElseClauseSyntax { Parent: IfStatementSyntax outer } when Exits(model, outer.Statement):
                    statement = outer;
                    break;
                default:
                    return false;
            }
        }
    }
}
