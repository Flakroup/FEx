using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0026: <c>foreach (char c in s.ToCharArray())</c>, where the string can be enumerated directly.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantStringToCharArrayCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0026";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant string.ToCharArray call",
        "The call to 'ToCharArray()' is redundant: a string can be enumerated directly",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A foreach over a string visits its characters, so copying them into an array first only allocates. Replaces the ReSharper inspection RedundantStringToCharArrayCall.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForEachStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var loop = (ForEachStatementSyntax)context.Node;

        // ReSharper stays silent for 'var' and for loop variables that convert the element, so only an explicit 'char' is reported.
        if (loop.Type.IsVar
            || loop.Expression is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } call
            || context.SemanticModel.GetTypeInfo(loop.Type).Type?.SpecialType != SpecialType.System_Char
            || context.SemanticModel.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "ToCharArray", ContainingType.SpecialType: SpecialType.System_String })
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, call.GetLocation()));
    }
}
