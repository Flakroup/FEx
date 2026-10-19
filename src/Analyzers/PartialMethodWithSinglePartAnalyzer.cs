using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>FEX0044: a partial method is declared but never implemented.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialMethodWithSinglePartAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0044";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Partial method has a single part",
        "Partial method '{0}' has no implementing part, so every call to it is removed",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "Without an implementation the declaration and its calls compile to nothing. Replaces the ReSharper inspection PartialMethodWithSinglePart.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: null } method)
            return;

        // The unfinished accessor of a partial property, typed in an editor, is a partial definition too.
        if (method.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) is not MethodDeclarationSyntax syntax
            || AnalyzerHelpers.IsNamedInDisabledCode(context.Compilation, method.Name, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)).GetLocation(), method.Name));
    }
}
