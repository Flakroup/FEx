using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FEx.Analyzers;

/// <summary>FEX0043: a <c>ref</c> or <c>out</c> parameter carries <c>[Optional]</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionalParameterRefOutAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0043";

    private const string OptionalAttributeName = "System.Runtime.InteropServices.OptionalAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Optional parameter is ref or out",
        "A 'ref' or 'out' parameter cannot be optional; '[Optional]' on '{0}' only helps COM callers",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "C# callers must still pass a variable, so the attribute promises an optional argument the language never honors. Replaces the ReSharper inspection OptionalParameterRefOut.",
        helpLinkUri: RuleDocumentation.HelpLink(DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Parameter);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;
        var modifiers = parameter.Modifiers;

        // `ref readonly` and `in` take a value, so they can carry a default.
        if (!modifiers.Any(SyntaxKind.OutKeyword) && !(modifiers.Any(SyntaxKind.RefKeyword) && !modifiers.Any(SyntaxKind.ReadOnlyKeyword)))
            return;

        foreach (var list in parameter.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol?.ContainingType.ToDisplayString() == OptionalAttributeName)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.Name.GetLocation(), parameter.Identifier.ValueText));
            }
        }
    }
}
