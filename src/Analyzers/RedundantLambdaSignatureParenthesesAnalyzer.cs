using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0008: <c>(x) =&gt; ...</c>, where <c>x =&gt; ...</c> says the same.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantLambdaSignatureParenthesesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0008";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant parentheses around a lambda parameter",
        "The parentheses around the lambda's single parameter are redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A lambda with exactly one untyped parameter, no modifier, no attribute and no explicit return type does not need parentheses. Replaces the ReSharper inspection RedundantLambdaSignatureParentheses.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var lambda = (ParenthesizedLambdaExpressionSyntax)context.Node;
        var parameters = lambda.ParameterList.Parameters;
        if (parameters.Count != 1 || lambda.AttributeLists.Count > 0 || lambda.ReturnType is not null)
            return;

        if (parameters[0] is { Type: null, Default: null, Modifiers.Count: 0, AttributeLists.Count: 0 })
            context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.ParameterList.OpenParenToken.GetLocation()));
    }
}
