using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0001: a compound assignment or increment/decrement on a <c>volatile</c> field.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonAtomicCompoundOperatorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Non-atomic compound operator on a volatile field",
        "'{0}' is a volatile field, so this read-modify-write is not atomic",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "volatile orders accesses but does not make a compound assignment or ++/-- atomic; another thread can write between the read and the write. Use Interlocked or a lock. Replaces the ReSharper inspection NonAtomicCompoundOperator.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(
            c => Check(c, ((ICompoundAssignmentOperation)c.Operation).Target),
            OperationKind.CompoundAssignment);
        context.RegisterOperationAction(
            c => Check(c, ((IIncrementOrDecrementOperation)c.Operation).Target),
            OperationKind.Increment,
            OperationKind.Decrement);
    }

    private static void Check(OperationAnalysisContext context, IOperation target)
    {
        if (target is IFieldReferenceOperation { Field.IsVolatile: true } field)
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), field.Field.Name));
    }
}
