using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0023: a <c>ToString()</c> call on a value type, or a type that may be one, inside a string concatenation.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantToStringCallForValueTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0023";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant ToString call on a value type",
        "The call to 'ToString()' is redundant: the string concatenation already converts the value to a string",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "Concatenating a string with a value calls ToString on it, and on current runtimes without boxing the value first. Replaces the ReSharper inspection RedundantToStringCallForValueType.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var call = (InvocationExpressionSyntax)context.Node;
        if (ToStringCalls.IsRedundant(call, context.SemanticModel, valueTypeReceiver: true))
            context.ReportDiagnostic(Diagnostic.Create(Rule, call.GetLocation()));
    }
}
