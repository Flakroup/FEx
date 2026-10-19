using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FEx.Analyzers;

/// <summary>FEX0046: the implementing part of a partial method never reads or writes one of its parameters.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedParameterInPartialMethodAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0046";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Unused parameter in partial method",
        "Parameter '{0}' of the partial method implementation is never used",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "The other part decides the signature, so the parameter cannot simply be dropped - which is why the compiler's own unused-parameter check stays silent. Replaces the ReSharper inspection UnusedParameterInPartialMethod.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(Analyze);
    }

    private static void Analyze(OperationBlockAnalysisContext context)
    {
        // An extern implementation has no body to use a parameter in.
        if (context.OwningSymbol is not IMethodSymbol { MethodKind: MethodKind.Ordinary, PartialDefinitionPart: not null, IsExtern: false } method)
            return;

        // nameof(a) names the parameter without using it.
        var used = new HashSet<ISymbol>(
            context.OperationBlocks
                .SelectMany(block => block.DescendantsAndSelf())
                .OfType<IParameterReferenceOperation>()
                .Where(reference => !AnalyzerHelpers.IsInsideNameOf(reference))
                .Select(reference => reference.Parameter),
            SymbolEqualityComparer.Default);

        foreach (var parameter in method.Parameters.Where(parameter => !used.Contains(parameter) && !IsDiscard(parameter.Name)))
            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name));
    }

    // `_`, `_1`: the name says the value is not wanted.
    private static bool IsDiscard(string name) => name.Length > 0 && name[0] == '_' && name.Skip(1).All(char.IsDigit);
}
