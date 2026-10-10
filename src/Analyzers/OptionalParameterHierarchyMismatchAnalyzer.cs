using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0042: an override of a method or indexer gives a parameter another default value than the member it overrides, or none.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionalParameterHierarchyMismatchAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0042";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Optional parameter default differs in the hierarchy",
        "Parameter '{0}' of '{1}' declares another default than the overridden member",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "The compiler takes the default from the static type of the receiver, so the same call through the base type and through the derived type passes different arguments. Replaces the ReSharper inspection OptionalParameterHierarchyMismatch.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Method, SymbolKind.Property);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        switch (context.Symbol)
        {
            case IMethodSymbol { OverriddenMethod: { } baseMethod } method:
                Check(context, method, method.Parameters, baseMethod.Parameters);
                break;
            case IPropertySymbol { OverriddenProperty: { } baseProperty } property:
                Check(context, property, property.Parameters, baseProperty.Parameters);
                break;
        }
    }

    private static void Check(SymbolAnalysisContext context, ISymbol member, ImmutableArray<IParameterSymbol> parameters, ImmutableArray<IParameterSymbol> baseParameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!AnalyzerHelpers.DefaultsEqual(parameters[i], baseParameters[i]))
                context.ReportDiagnostic(Diagnostic.Create(Rule, parameters[i].Locations[0], parameters[i].Name, member.ToDisplayString()));
        }
    }
}
