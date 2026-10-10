using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace FEx.Analyzers;

/// <summary>FEX0006: an identifier spelled with <c>@</c> although its name is not a reserved keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantVerbatimPrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0006";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Redundant verbatim prefix on an identifier",
        "The '@' prefix on '{0}' is redundant: it is not a reserved keyword here",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "The @ prefix only matters when the name is a reserved keyword (or a contextual one the position would otherwise read as a keyword). Replaces the ReSharper inspection RedundantVerbatimPrefix.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction(Analyze);
    }

    private static void Analyze(SemanticModelAnalysisContext context)
    {
        var tree = context.SemanticModel.SyntaxTree;
        foreach (var token in tree.GetRoot(context.CancellationToken).DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.Text.Length > 1 && token.Text[0] == '@' && IsRedundant(context.SemanticModel, token))
                context.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(tree, new TextSpan(token.SpanStart, 1)), token.ValueText));
        }
    }

    private static bool IsRedundant(SemanticModel model, SyntaxToken token)
    {
        var name = token.ValueText;
        if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
            return false;

        var parent = token.Parent!;
        var namesTypeDeclaration = parent is BaseTypeDeclarationSyntax type && type.Identifier == token
            || parent is DelegateDeclarationSyntax @delegate && @delegate.Identifier == token;
        if (namesTypeDeclaration && name is "record" or "scoped" or "file" or "extension")
            return false;

        // Without the prefix, '_' declares a discard rather than the variable, and 'when' ends the pattern of a case label or arm.
        if (parent is SingleVariableDesignationSyntax designation && (name == "_" || name == "when" && EndsInCasePattern(designation)))
            return false;

        // ReSharper keeps the prefix on every query keyword inside a query, and removing it can change how the query parses.
        if (SyntaxFacts.IsQueryContextualKeyword(SyntaxFacts.GetContextualKeywordKind(name)) && parent.Ancestors().Any(ancestor => ancestor is QueryExpressionSyntax))
            return false;

        return name switch
        {
            "field" => !InPropertyAccessor(parent),
            "await" => !InAsyncBody(parent),
            _ => !NeedsPrefixAsAttributeName(model, parent, name),
        };
    }

    // 'case int when:' and 'int when when ...' read 'when' as the start of a filter, so a designation of that name in the pattern needs its prefix.
    private static bool EndsInCasePattern(SingleVariableDesignationSyntax designation) =>
        designation.Ancestors().FirstOrDefault(ancestor => ancestor is CasePatternSwitchLabelSyntax or SwitchExpressionArmSyntax) switch
        {
            CasePatternSwitchLabelSyntax label => label.Pattern.Span.Contains(designation.Span),
            SwitchExpressionArmSyntax arm => arm.Pattern.Span.Contains(designation.Span),
            _ => false,
        };

    // Inside a property accessor or expression body 'field' is a keyword, so the prefix is what keeps it a name.
    private static bool InPropertyAccessor(SyntaxNode? node) =>
        node is not null && node.AncestorsAndSelf().Any(ancestor =>
            ancestor is AccessorDeclarationSyntax { Parent.Parent: PropertyDeclarationSyntax }
            || ancestor is ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax });

    // Inside the body of an async function 'await' is a keyword, and so are the parameters of an async lambda, anonymous method or local
    // function. A sync lambda or anonymous method, and the name and parameters of a sync local function, belong to the function around them.
    private static bool InAsyncBody(SyntaxNode start)
    {
        SyntaxNode? child = null;
        for (var node = start; ; child = node, node = node.Parent!)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method:
                    return method.Modifiers.Any(SyntaxKind.AsyncKeyword) && IsBody(child, method.Body, method.ExpressionBody);
                case LocalFunctionStatementSyntax local:
                    if (IsBody(child, local.Body, local.ExpressionBody))
                        return local.Modifiers.Any(SyntaxKind.AsyncKeyword);

                    if (child is ParameterListSyntax && local.Modifiers.Any(SyntaxKind.AsyncKeyword))
                        return true;

                    break;
                case AnonymousFunctionExpressionSyntax lambda when lambda.Modifiers.Any(SyntaxKind.AsyncKeyword):
                    return true;
                case GlobalStatementSyntax:
                    // Top-level statements become async the moment any of them awaits, so keep the prefix.
                    return true;
                case MemberDeclarationSyntax or AccessorDeclarationSyntax or CompilationUnitSyntax:
                    return false;
                default:
                    break;
            }
        }
    }

    private static bool IsBody(SyntaxNode? child, SyntaxNode? block, SyntaxNode? expression) =>
        child is not null && (child == block || child == expression);

    // [@Foo] means FooAttribute only when no type is named Foo exactly; the prefix is what asks for that exact name.
    private static bool NeedsPrefixAsAttributeName(SemanticModel model, SyntaxNode? node, string name)
    {
        if (node is not SimpleNameSyntax simple)
            return false;

        ExpressionSyntax attributeName = simple;
        ExpressionSyntax? qualifier = null;
        if (simple.Parent is QualifiedNameSyntax qualified && qualified.Right == simple)
        {
            attributeName = qualified;
            qualifier = qualified.Left;
        }
        else if (simple.Parent is AliasQualifiedNameSyntax)
        {
            return true;
        }

        if (attributeName.Parent is not AttributeSyntax)
            return false;

        var suffixed = name + "Attribute";
        if (qualifier is null)
            return !model.LookupNamespacesAndTypes(simple.SpanStart, null, suffixed).IsEmpty;

        return model.GetSymbolInfo(qualifier).Symbol is not INamespaceOrTypeSymbol container || !container.GetTypeMembers(suffixed).IsEmpty;
    }
}
