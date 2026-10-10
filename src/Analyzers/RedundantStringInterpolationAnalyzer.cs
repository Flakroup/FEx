using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0025: an interpolated string without a single interpolation hole.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantStringInterpolationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0025";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant string interpolation",
        "The string interpolation is redundant: the string has no interpolation hole",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A $\"...\" string without a {hole} is a plain string literal. Replaces the ReSharper inspection RedundantStringInterpolation.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InterpolatedStringExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var interpolated = (InterpolatedStringExpressionSyntax)context.Node;
        if (interpolated.Contents.OfType<InterpolationSyntax>().Any() || IsPieceOfInterpolatedConcatenation(interpolated))
            return;

        // A FormattableString or IFormattable target needs the interpolation, whatever it holds.
        var target = context.SemanticModel.GetTypeInfo(interpolated).ConvertedType?.ToDisplayString();
        if (target is not ("System.FormattableString" or "System.IFormattable"))
            context.ReportDiagnostic(Diagnostic.Create(Rule, interpolated.GetLocation()));
    }

    // A line of a $"..." + $"..." chain keeps its prefix for the lines that do have a hole; ReSharper leaves it alone.
    private static bool IsPieceOfInterpolatedConcatenation(ExpressionSyntax interpolated)
    {
        var node = interpolated;
        while (node.Parent is ParenthesizedExpressionSyntax parenthesized)
            node = parenthesized;

        return node.Parent is BinaryExpressionSyntax binary
            && binary.IsKind(SyntaxKind.AddExpression)
            && IsInterpolatedConcatenation(binary.Left == node ? binary.Right : binary.Left);
    }

    private static bool IsInterpolatedConcatenation(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression is InterpolatedStringExpressionSyntax
            || expression is BinaryExpressionSyntax binary
            && binary.IsKind(SyntaxKind.AddExpression)
            && IsInterpolatedConcatenation(binary.Left)
            && IsInterpolatedConcatenation(binary.Right);
    }
}
