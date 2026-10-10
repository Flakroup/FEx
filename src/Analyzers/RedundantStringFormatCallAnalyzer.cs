using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0024: <c>string.Format</c> called with a string literal and nothing to format.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantStringFormatCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0024";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant string.Format call",
        "The call to 'string.Format' is redundant: the literal has no placeholder to format",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "string.Format with a literal that has no placeholder and no arguments returns the literal, with its doubled braces collapsed. Replaces the ReSharper inspection RedundantStringFormatCall.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var call = (InvocationExpressionSyntax)context.Node;
        if (call.ArgumentList.Arguments is not { Count: 1 } arguments
            || arguments[0].Expression is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal
            || context.SemanticModel.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "Format", ContainingType.SpecialType: SpecialType.System_String })
            return;

        // A doubled brace is the escape for a single one; any other brace is a placeholder or a format error.
        if (literal.Token.ValueText.Replace("{{", "").Replace("}}", "").IndexOfAny(['{', '}']) < 0)
            context.ReportDiagnostic(Diagnostic.Create(Rule, call.GetLocation()));
    }
}
