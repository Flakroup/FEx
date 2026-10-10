using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0045: a type is declared <c>partial</c> although the compilation holds one declaration of it.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialTypeWithSinglePartAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0045";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Partial type has a single part",
        "Type '{0}' is declared 'partial' but has no other part",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "The modifier promises a second declaration that does not exist, which hides whether generated code is missing. Replaces the ReSharper inspection PartialTypeWithSinglePart.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.DeclaringSyntaxReferences is not [{ } reference] || reference.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax syntax)
            return;

        // A declaration-only partial method needs the keyword on the type, and ReSharper leaves such a type alone - as it
        // does one that holds another partial type.
        if (syntax.Members.Any(member => member is MethodDeclarationSyntax or BaseTypeDeclarationSyntax && member.Modifiers.Any(SyntaxKind.PartialKeyword)))
            return;

        foreach (var modifier in syntax.Modifiers.Where(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            context.ReportDiagnostic(Diagnostic.Create(Rule, modifier.GetLocation(), type.Name));
    }
}
