using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0036: an explicit <c>: base()</c> with no arguments, which the compiler adds on its own.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantBaseConstructorCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0036";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant base constructor call",
        "The call to 'base()' is redundant: a constructor without an initializer calls the parameterless base constructor anyway",
        "Redundancy",
        DiagnosticSeverity.Warning,
        true,
        "A constructor, or a primary constructor's base type, with no initializer already calls base(). Replaces the ReSharper inspection RedundantBaseConstructorCall.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInitializer, SyntaxKind.BaseConstructorInitializer);
        context.RegisterSyntaxNodeAction(AnalyzePrimaryBase, SyntaxKind.PrimaryConstructorBaseType);
    }

    private static void AnalyzeInitializer(SyntaxNodeAnalysisContext context)
    {
        var initializer = (ConstructorInitializerSyntax)context.Node;
        if (initializer.ArgumentList.Arguments.Count == 0)
            context.ReportDiagnostic(Diagnostic.Create(Rule, initializer.GetLocation()));
    }

    private static void AnalyzePrimaryBase(SyntaxNodeAnalysisContext context)
    {
        var baseType = (PrimaryConstructorBaseTypeSyntax)context.Node;
        if (baseType.ArgumentList.Arguments.Count == 0)
            context.ReportDiagnostic(Diagnostic.Create(Rule, baseType.ArgumentList.GetLocation()));
    }
}
