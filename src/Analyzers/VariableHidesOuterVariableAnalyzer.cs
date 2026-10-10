using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FEx.Analyzers;

/// <summary>FEX0003: a lambda or local function declares a parameter or local named like a variable of an enclosing scope.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VariableHidesOuterVariableAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Variable hides outer variable",
        "'{0}' hides a variable of the same name from an enclosing scope",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "C# allows a lambda or local function to reuse the name of an enclosing local or parameter, so a later edit can read or write the wrong variable without a compiler error. Replaces the ReSharper inspection VariableHidesOuterVariable.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.LocalFunctionStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var function = context.Node;
        var declarations = function.DescendantNodes(node => node == function || !IsFunction(node)).Select(GetDeclaredIdentifier);

        foreach (var identifier in declarations)
        {
            if (identifier.Text is not { Length: > 0 } name || name == "_")
                continue;

            var outer = context.SemanticModel
                .LookupSymbols(function.SpanStart, name: name)
                .FirstOrDefault(symbol => IsCapturable(symbol) && symbol.Locations[0].SourceSpan.End <= function.SpanStart);
            if (outer is not null && !IsBeingInitialized(function, outer) && !IsOutOfReach(function, outer))
                context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), name));
        }
    }

    // ReSharper does not count a variable no lambda could capture as an outer one: a ref, in or out parameter, a ref
    // local, or a local of a ref struct type (a ref struct parameter is still counted).
    private static bool IsCapturable(ISymbol symbol) =>
        symbol is ILocalSymbol { RefKind: RefKind.None, Type.IsRefLikeType: false } or IParameterSymbol { RefKind: RefKind.None };

    private static bool IsFunction(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

    private static SyntaxToken GetDeclaredIdentifier(SyntaxNode node) =>
        node switch
        {
            ParameterSyntax parameter => parameter.Identifier,
            VariableDeclaratorSyntax declarator => declarator.Identifier,
            SingleVariableDesignationSyntax designation => designation.Identifier,
            ForEachStatementSyntax forEach => forEach.Identifier,
            CatchDeclarationSyntax catchDeclaration => catchDeclaration.Identifier,
            _ => default,
        };

    // A function inside the initializer of the declaration that declares the variable (`var x = F(() => { var x = 1; })`,
    // `var (a, b) = F(a => ...)`) is not hiding anything for ReSharper; a sibling declarator of the same statement still is.
    private static bool IsBeingInitialized(SyntaxNode function, ISymbol outer)
    {
        var declaration = outer.DeclaringSyntaxReferences[0].GetSyntax();
        return declaration switch
        {
            VariableDeclaratorSyntax declarator => declarator.Initializer?.Span.Contains(function.Span) == true,
            SingleVariableDesignationSyntax designation => designation.Ancestors().OfType<DeclarationExpressionSyntax>().FirstOrDefault()
                is { Parent: AssignmentExpressionSyntax assignment } && assignment.Right.Span.Contains(function.Span),
            _ => false,
        };
    }

    // A static lambda or local function cannot see the variable, so reusing its name is harmless. The walk stops at the
    // function that declares the variable; static functions further out do not matter.
    private static bool IsOutOfReach(SyntaxNode function, ISymbol outer)
    {
        for (var node = function; node is not null; node = node.Parent?.AncestorsAndSelf().FirstOrDefault(IsFunction))
        {
            if (node.Span.Contains(outer.Locations[0].SourceSpan))
                return false;

            var modifiers = node is AnonymousFunctionExpressionSyntax anonymous
                ? anonymous.Modifiers
                : ((LocalFunctionStatementSyntax)node).Modifiers;
            if (modifiers.Any(SyntaxKind.StaticKeyword))
                return true;
        }

        return false;
    }
}
