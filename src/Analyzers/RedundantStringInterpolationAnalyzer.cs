using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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

    private static readonly object Yes = new();
    private static readonly object No = new();
    private static readonly ConditionalWeakTable<BinaryExpressionSyntax, object> Settled = new();

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
        if (interpolated.Contents.OfType<InterpolationSyntax>().Any()
            || HasEscapedBrace(interpolated)
            || IsPieceOfInterpolatedConcatenation(interpolated))
            return;

        // A FormattableString or IFormattable target needs the interpolation, and so does a handler, which only an interpolated string converts to.
        if (context.SemanticModel.GetConversion(interpolated, context.CancellationToken) is { IsInterpolatedString: false, IsInterpolatedStringHandler: false })
            context.ReportDiagnostic(Diagnostic.Create(Rule, interpolated.GetLocation()));
    }

    // '$"{{x}}"' prints '{x}', so a plain literal in its place would have to spell the braces differently.
    private static bool HasEscapedBrace(InterpolatedStringExpressionSyntax interpolated) =>
        interpolated.Contents.OfType<InterpolatedStringTextSyntax>().Any(static text => text.TextToken.Text.IndexOfAny(['{', '}']) >= 0);

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

    // Whether the expression is a '+' of interpolated strings only. A generated chain can be tens of thousands of terms deep, so the walk keeps its own
    // stack, and it remembers every sum it has settled: each hole-free piece of a chain asks about the rest of it, which would otherwise be quadratic.
    private static bool IsInterpolatedConcatenation(ExpressionSyntax expression)
    {
        var sums = new List<BinaryExpressionSyntax>();
        var pending = new Stack<ExpressionSyntax>();
        pending.Push(expression);
        while (pending.Count > 0)
        {
            if (Unwrap(pending.Pop()) is BinaryExpressionSyntax sum && sum.IsKind(SyntaxKind.AddExpression) && !Settled.TryGetValue(sum, out _))
            {
                sums.Add(sum);
                pending.Push(sum.Right);
                pending.Push(sum.Left);
            }
        }

        // A sum comes before the sums inside it, so going back to front settles the inner ones first.
        for (var i = sums.Count - 1; i >= 0; i--)
        {
            var settled = IsInterpolatedOperand(sums[i].Left) && IsInterpolatedOperand(sums[i].Right);
            Settled.GetValue(sums[i], _ => settled ? Yes : No);
        }

        return IsInterpolatedOperand(expression);
    }

    // Only a '+' enters the walk, so a settled sum is one of those.
    private static bool IsInterpolatedOperand(ExpressionSyntax operand) => Unwrap(operand) switch
    {
        InterpolatedStringExpressionSyntax => true,
        BinaryExpressionSyntax sum when sum.IsKind(SyntaxKind.AddExpression) => Settled.TryGetValue(sum, out var verdict) && verdict == Yes,
        _ => false,
    };

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
