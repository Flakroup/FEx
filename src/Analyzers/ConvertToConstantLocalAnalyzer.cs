using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0051: a local, or a field no other type can reach, is initialized with a compile-time constant, read, and never written again.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConvertToConstantLocalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0051";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Convert to constant (local)",
        "'{0}' is initialized with a constant and never changes; it can be a constant",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A constant states that the value cannot change and is folded by the compiler. Replaces the ReSharper inspection ConvertToConstant.Local.",
        null,
        WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(AnalyzeLocals);
        context.RegisterCompilationStartAction(Start);
    }

    private static void AnalyzeLocals(OperationBlockAnalysisContext context)
    {
        var operations = context.OperationBlocks.SelectMany(block => block.DescendantsAndSelf()).ToList();
        var references = operations
            .OfType<ILocalReferenceOperation>()
            .Where(reference => !AnalyzerHelpers.IsInsideNameOf(reference))
            .ToLookup(reference => reference.Local, SymbolEqualityComparer.Default);

        foreach (var declarator in operations.OfType<IVariableDeclaratorOperation>())
        {
            // A for-loop or using variable is declared by a statement that is not a plain local declaration.
            if (declarator.Syntax is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax, Initializer: { } initializer } syntax
                || declarator.Symbol is not { IsConst: false } local
                || !AnalyzerHelpers.IsConstantType(local.Type)
                || !IsConstantInitializer(declarator.Initializer!.Value, initializer.Value))
                continue;

            var uses = references[local].ToList();
            if (uses.Count > 0 && !uses.Any(AnalyzerHelpers.IsWrite))
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Identifier.GetLocation(), local.Name));
        }
    }

    private static void Start(CompilationStartAnalysisContext context)
    {
        // References and initializers arrive in any order, and those in nested types never reach an action registered for
        // their outer type, so the state of a field is kept for the whole compilation.
        var fields = new ConcurrentDictionary<IFieldSymbol, FieldState>(SymbolEqualityComparer.Default);
        context.RegisterOperationAction(
            operation =>
            {
                var initializer = (IFieldInitializerOperation)operation.Operation;
                foreach (var field in initializer.InitializedFields.Where(IsCandidate))
                {
                    var state = fields.GetOrAdd(field, _ => new FieldState());
                    lock (state)
                    {
                        state.Declarator = (VariableDeclaratorSyntax)initializer.Syntax.Parent!;
                        state.IsConstant = IsConstantInitializer(initializer.Value, ((EqualsValueClauseSyntax)initializer.Syntax).Value);
                    }
                }
            },
            OperationKind.FieldInitializer);
        context.RegisterOperationAction(
            operation =>
            {
                var reference = (IFieldReferenceOperation)operation.Operation;
                var state = fields.GetOrAdd(reference.Field.OriginalDefinition, _ => new FieldState());
                lock (state)
                {
                    state.IsReferenced = true;
                    state.IsBlocked |= AnalyzerHelpers.IsWrite(reference);
                }
            },
            OperationKind.FieldReference);
        context.RegisterOperationAction(
            operation =>
            {
                // nameof(_f) is a use ReSharper does not turn into a constant.
                var argument = ((INameOfOperation)operation.Operation).Argument.Syntax;
                if (operation.Operation.SemanticModel!.GetSymbolInfo(argument, operation.CancellationToken).Symbol is not IFieldSymbol named)
                    return;

                var state = fields.GetOrAdd(named.OriginalDefinition, _ => new FieldState());
                lock (state)
                    state.IsBlocked = true;
            },
            OperationKind.NameOf);
        context.RegisterCompilationEndAction(end =>
        {
            foreach (var state in fields.Values.Where(state => state is { IsConstant: true, IsReferenced: true, IsBlocked: false }))
                end.ReportDiagnostic(Diagnostic.Create(Rule, state.Declarator!.Identifier.GetLocation(), state.Declarator.Identifier.ValueText));
        });
    }

    private static bool IsCandidate(IFieldSymbol field) =>
        field is { IsConst: false } && AnalyzerHelpers.IsConstantType(field.Type) && AnalyzerHelpers.IsEffectivelyPrivate(field);

    // `null` is not worth a constant, `default` is.
    private static bool IsConstantInitializer(IOperation value, ExpressionSyntax expression) =>
        value.ConstantValue.Value is not null || expression.IsKind(SyntaxKind.DefaultLiteralExpression) || expression is DefaultExpressionSyntax;

    private sealed class FieldState
    {
        public VariableDeclaratorSyntax? Declarator { get; set; }

        public bool IsConstant { get; set; }

        public bool IsReferenced { get; set; }

        public bool IsBlocked { get; set; }
    }
}
