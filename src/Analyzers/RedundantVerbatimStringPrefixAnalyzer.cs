using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace FEx.Analyzers;

/// <summary>FEX0007: a verbatim string (<c>@"..."</c>, <c>$@"..."</c>) whose text has nothing the prefix changes.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantVerbatimStringPrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0007";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant verbatim string prefix",
        "The '@' prefix is redundant: the string has no backslash, quote or special character",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A verbatim string only differs from a regular one when its text holds a backslash, a quote, a line break or another character that needs an escape. Replaces the ReSharper inspection RedundantVerbatimStringPrefix.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.StringLiteralExpression, SyntaxKind.Utf8StringLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolated, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeLiteral(SyntaxNodeAnalysisContext context)
    {
        var token = ((LiteralExpressionSyntax)context.Node).Token;
        var text = token.Text;
        if (!text.StartsWith("@\"", StringComparison.Ordinal))
            return;

        // The raw text is compared, so a doubled quote stays a quote and keeps the prefix; drop the opening @", the closing quote and a u8 suffix.
        var end = text.Length - (context.Node.IsKind(SyntaxKind.Utf8StringLiteralExpression) ? 3 : 1);
        if (end >= 2 && text[end] == '"' && text.Substring(2, end - 2).All(IsPlain))
            Report(context, token.SpanStart);
    }

    private static void AnalyzeInterpolated(SyntaxNodeAnalysisContext context)
    {
        var interpolated = (InterpolatedStringExpressionSyntax)context.Node;
        var start = interpolated.StringStartToken;
        var at = start.Text.IndexOf('@');
        if (at < 0)
            return;

        // Hole expressions and alignments are code, not text; only the literal text and the format clauses are strings.
        var texts = interpolated.Contents.SelectMany(content => content switch
        {
            InterpolatedStringTextSyntax text => new[] { text.TextToken.Text },
            InterpolationSyntax { FormatClause: { } format } => new[] { format.FormatStringToken.Text },
            _ => Array.Empty<string>(),
        });
        if (texts.All(text => text.All(IsPlain)))
            Report(context, start.SpanStart + at);
    }

    private static bool IsPlain(char c)
    {
        if (c is '\\' or '"')
            return false;

        return c is >= ' ' and <= '\u007f' || char.IsLetter(c) || char.IsDigit(c) || char.IsPunctuation(c);
    }

    private static void Report(SyntaxNodeAnalysisContext context, int position) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(context.Node.SyntaxTree, new TextSpan(position, 1))));
}
