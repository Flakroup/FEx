using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0054: the empty <c>()</c> of <c>new T() { ... }</c>, which an object or collection initializer makes optional.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEmptyObjectCreationArgumentListAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0054";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant empty argument list in object creation",
        "The empty argument list '()' is redundant before the initializer",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "'new T { ... }' calls the same parameterless constructor as 'new T() { ... }'. Replaces the ReSharper inspection RedundantEmptyObjectCreationArgumentList.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;

        // A comment between the parentheses is something the author wrote on purpose.
        if (creation is { Initializer: not null, ArgumentList: { Arguments.Count: 0 } list }
            && !EmptyInitializers.HasComment(list.OpenParenToken.TrailingTrivia)
            && !EmptyInitializers.HasComment(list.CloseParenToken.LeadingTrivia))
            context.ReportDiagnostic(Diagnostic.Create(Rule, list.GetLocation()));
    }
}
