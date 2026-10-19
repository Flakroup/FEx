using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0018: a <c>case</c> label that shares its section with <c>default</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantCaseLabelAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0018";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant case label",
        "This case label is redundant: the section also has a 'default' label",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "In a section that also has default, a case label only repeats what default already matches, unless it carries a 'when' filter, declares a variable with 'var', or is the target of a 'goto case'. Like ReSharper, it is also reported where the label sits ahead of a later section that overlaps it, so removing it can change which section runs: check that before deleting it. Replaces the ReSharper inspection RedundantCaseLabel.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

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
        if (!section.Labels.Any(label => label is DefaultSwitchLabelSyntax) || section.Parent is not SwitchStatementSyntax @switch)
            return;

        // ReSharper leaves the labels of an enum switch alone: there they list the members the default also covers.
        if (IsEnum(context.SemanticModel.GetTypeInfo(@switch.Expression, context.CancellationToken).Type))
            return;

        foreach (var label in section.Labels.Where(label => IsRedundant(context, @switch, label)))
            context.ReportDiagnostic(Diagnostic.Create(Rule, label.GetLocation()));
    }

    private static bool IsEnum(ITypeSymbol? type) =>
        type is { TypeKind: TypeKind.Enum }
        || type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ TypeKind: TypeKind.Enum }] };

    private static bool IsRedundant(SyntaxNodeAnalysisContext context, SwitchStatementSyntax @switch, SwitchLabelSyntax label) =>
        label is not (DefaultSwitchLabelSyntax or CasePatternSwitchLabelSyntax { WhenClause: not null } or CasePatternSwitchLabelSyntax { Pattern: VarPatternSyntax })
        && !IsGotoCaseTarget(context, @switch, label);

    // 'goto case 1 + 1;' reaches 'case 2:', and 'goto case 1;' reaches 'case 1L:' in a switch on long: the jump's target label is what
    // counts, not its spelling. A goto inside a nested switch targets that switch's labels, so it never matches one of this switch.
    private static bool IsGotoCaseTarget(SyntaxNodeAnalysisContext context, SwitchStatementSyntax @switch, SwitchLabelSyntax label)
    {
        var declared = context.SemanticModel.GetDeclaredSymbol(label, context.CancellationToken);

        return @switch.DescendantNodes()
            .Where(node => node.IsKind(SyntaxKind.GotoCaseStatement))
            .Select(statement => context.SemanticModel.GetOperation(statement, context.CancellationToken))
            .OfType<IBranchOperation>()
            .Any(branch => SymbolEqualityComparer.Default.Equals(branch.Target, declared));
    }
}
