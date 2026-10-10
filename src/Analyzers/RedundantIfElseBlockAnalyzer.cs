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
        "When the if branch always returns, throws or jumps, the else body runs exactly when it would without the else keyword. Replaces the ReSharper inspection RedundantIfElseBlock.");

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
        if (statement.Else is not { } @else)
            return;

        var flow = context.SemanticModel.AnalyzeControlFlow(statement.Statement);
        if (flow is { EndPointIsReachable: false })
            context.ReportDiagnostic(Diagnostic.Create(Rule, @else.ElseKeyword.GetLocation()));
    }
}
