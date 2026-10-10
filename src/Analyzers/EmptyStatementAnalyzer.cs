using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0011: a lone <c>;</c> that is a statement of its own in a block or a switch section.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyStatementAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0011";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Empty statement",
        "This empty statement is redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A semicolon that is a statement of its own does nothing. An empty body such as 'if (b);' is the compiler's warning (CS0642) and is left to it. Replaces the ReSharper inspection EmptyStatement.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.EmptyStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node.Parent is BlockSyntax or SwitchSectionSyntax)
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
    }
}
