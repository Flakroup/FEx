using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FEx.Analyzers;

/// <summary>FEX0055: an empty <c>{ }</c> initializer after <c>new T</c>, <c>new T()</c> or <c>new()</c>, or one nested in an object initializer.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEmptyObjectOrCollectionInitializerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0055";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant empty object or collection initializer",
        "The empty initializer '{ }' is redundant",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "An initializer with no member or element assigns nothing, so the creation behaves the same without it. Replaces the ReSharper inspection RedundantEmptyObjectOrCollectionInitializer.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectInitializerExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var initializer = (InitializerExpressionSyntax)context.Node;

        // An empty pair of braces is always an object initializer, whatever the type: the parser has no way to tell a collection's apart. An array
        // initializer is the array's only way to be sized and 'with { }' is a copy; neither has this kind.
        // A comment inside the braces is something the author wrote on purpose.
        if (initializer.Expressions.Count == 0
            && !EmptyInitializers.HasComment(initializer.OpenBraceToken.TrailingTrivia)
            && !EmptyInitializers.HasComment(initializer.CloseBraceToken.LeadingTrivia))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, initializer.GetLocation()));
        }
    }
}
