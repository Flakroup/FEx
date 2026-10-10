using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0022: a <c>ToString()</c> call on a reference type, or on a string, where the surrounding code converts the receiver to a string itself.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantToStringCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0022";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant ToString call",
        "The call to 'ToString()' is redundant: the surrounding code already converts the receiver to a string",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "String concatenation, an interpolation hole and the arguments of string.Format call ToString themselves, and string.ToString returns the string it was called on. Replaces the ReSharper inspection RedundantToStringCall.");

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
        if (ToStringCalls.IsRedundant(call, context.SemanticModel, valueTypeReceiver: false))
            context.ReportDiagnostic(Diagnostic.Create(Rule, call.GetLocation()));
    }
}
