using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0057: a parameter of a member other types can reach that only the precondition checks at the top of its body read.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterOnlyUsedForPreconditionCheckGlobalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0057";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Parameter is only used for a precondition check (Global)",
        "Parameter '{0}' is only used for precondition checks",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A parameter that is validated and then never read has no effect on what the body does, so the body either misses a use or the parameter is dead. Replaces the ReSharper inspection ParameterOnlyUsedForPreconditionCheck.Global.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(block => PreconditionCheckAnalysis.Analyze(block, Rule, false));
    }
}
