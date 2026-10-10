using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0017: a <c>default: break;</c> section, which falls out of the switch just as no section would.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEmptySwitchSectionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0017";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant empty default switch section",
        "This 'default' section only breaks, so it is redundant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A default section that holds nothing but 'break;' behaves as a switch without one, unless a 'goto default;' targets it. Replaces the ReSharper inspection RedundantEmptySwitchSection.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SwitchSection);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var section = (SwitchSectionSyntax)context.Node;
        if (section is not { Labels: [DefaultSwitchLabelSyntax], Parent: SwitchStatementSyntax @switch } || !IsOnlyBreak(section.Statements))
            return;

        var targeted = @switch.DescendantNodes()
            .Where(node => node.IsKind(SyntaxKind.GotoDefaultStatement))
            .Any(node => node.Ancestors().OfType<SwitchStatementSyntax>().First() == @switch);
        if (!targeted)
            context.ReportDiagnostic(Diagnostic.Create(Rule, section.GetLocation()));
    }

    // Nested blocks around the break change nothing.
    private static bool IsOnlyBreak(SyntaxList<StatementSyntax> statements)
    {
        while (statements is [BlockSyntax block])
            statements = block.Statements;

        return statements is [BreakStatementSyntax];
    }
}
