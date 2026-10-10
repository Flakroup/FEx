using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0009: <c>[Attr()]</c>, where <c>[Attr]</c> says the same.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantAttributeParenthesesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0009";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant parentheses on an attribute",
        "The empty argument list of attribute '{0}' is redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "An attribute with no arguments does not need empty parentheses. Replaces the ReSharper inspection RedundantAttributeParentheses.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Attribute);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        if (attribute.ArgumentList is not { Arguments.Count: 0 } arguments)
            return;

        // An attribute whose type does not resolve is already an error; leave it to the compiler.
        var info = context.SemanticModel.GetSymbolInfo(attribute.Name, context.CancellationToken);
        if (info.Symbol is not null || !info.CandidateSymbols.IsEmpty)
            context.ReportDiagnostic(Diagnostic.Create(Rule, arguments.GetLocation(), attribute.Name.ToString()));
    }
}
