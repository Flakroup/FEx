using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        "In a section that also has default, a case label adds nothing, unless it carries a 'when' filter, declares a variable with 'var', or is the target of a 'goto case'. Replaces the ReSharper inspection RedundantCaseLabel.");

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

    private static bool IsRedundant(SyntaxNodeAnalysisContext context, SwitchStatementSyntax @switch, SwitchLabelSyntax label)
    {
        if (label is DefaultSwitchLabelSyntax or CasePatternSwitchLabelSyntax { WhenClause: not null } or CasePatternSwitchLabelSyntax { Pattern: VarPatternSyntax })
            return false;

        var value = (label as CaseSwitchLabelSyntax)?.Value;

        return value is null || !IsGotoCaseTarget(context, @switch, value);
    }

    // 'goto case 1 + 1;' reaches 'case 2:', so the label is what the jump needs. A goto inside a nested switch targets that switch.
    private static bool IsGotoCaseTarget(SyntaxNodeAnalysisContext context, SwitchStatementSyntax @switch, ExpressionSyntax value)
    {
        var constant = context.SemanticModel.GetConstantValue(value, context.CancellationToken);

        return @switch.DescendantNodes()
            .OfType<GotoStatementSyntax>()
            .Where(statement => statement.Expression is not null)
            .Where(statement => statement.Ancestors().OfType<SwitchStatementSyntax>().First() == @switch)
            .Select(statement => context.SemanticModel.GetConstantValue(statement.Expression!, context.CancellationToken))
            .Any(target => target.HasValue && Equals(target.Value, constant.Value));
    }
}
