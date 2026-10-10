using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0013: a namespace that declares no type and no nested namespace.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyNamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0013";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Empty namespace",
        "Namespace '{0}' is empty",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A namespace with no member, whatever usings or comments it holds, declares nothing. Replaces the ReSharper inspection EmptyNamespace.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var declaration = (BaseNamespaceDeclarationSyntax)context.Node;
        if (declaration.Members.Count == 0)
            context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.Name.GetLocation(), declaration.Name.ToString()));
    }
}
