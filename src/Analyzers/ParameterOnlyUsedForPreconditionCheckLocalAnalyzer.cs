using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0058: a parameter of a private member, a constructor, a lambda or a local function that only the precondition checks at the top of its body read.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterOnlyUsedForPreconditionCheckLocalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0058";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Parameter is only used for a precondition check (Local)",
        "Parameter '{0}' is only used for precondition checks",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A parameter that is validated and then never read has no effect on what the body does, so the body either misses a use or the parameter is dead. Replaces the ReSharper inspection ParameterOnlyUsedForPreconditionCheck.Local.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(block => PreconditionCheckAnalysis.Analyze(block, Rule, true));
    }
}
