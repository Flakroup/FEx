using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>FEX0002: a <c>while</c>, <c>do</c> or <c>for</c> loop whose condition depends on locals that the loop never writes.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoopVariableNeverChangedAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FEX0002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Loop variable is never changed inside the loop",
        "'{0}' is read by the loop condition but never changed inside the loop",
        "Correctness",
        DiagnosticSeverity.Warning,
        true,
        "A condition made only of locals, parameters and constants, none of which the loop writes (directly, by ref, or through a closure), cannot change, so the loop runs zero times or forever. Fields, properties and calls in the condition are not examined. Replaces the ReSharper inspection LoopVariableIsNeverChangedInsideLoop.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Loop);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        IReadOnlyCollection<IOperation> parts;
        IOperation? condition;
        switch (context.Operation)
        {
            case IWhileLoopOperation whileLoop:
                condition = whileLoop.Condition;
                parts = [whileLoop.Body];
                break;
            case IForLoopOperation forLoop:
                condition = forLoop.Condition;
                parts = forLoop.AtLoopBottom.Prepend(forLoop.Body).ToList();
                break;
            default:
                return;
        }

        var references = new List<IOperation>();
        if (condition is null || !CollectInvariantReferences(condition, references))
            return;

        // One variable that changes is enough to make the condition move: `i < n` with only `i++` is fine.
        var variables = references.GroupBy(GetVariable, SymbolEqualityComparer.Default).ToList();
        if (variables.Any(group => group.Key is not { } variable || IsWritten(parts, variable) || IsWrittenElsewhere(context.Operation, variable)))
            return;

        foreach (var group in variables)
            context.ReportDiagnostic(Diagnostic.Create(Rule, group.First().Syntax.GetLocation(), group.Key!.Name));
    }

    // True when the condition is built only from locals, parameters, constants and operators on them: the one shape
    // whose value is decided by the variables alone. A call, member access or assignment could change it by itself.
    private static bool CollectInvariantReferences(IOperation operation, List<IOperation> references)
    {
        switch (operation)
        {
            case { ConstantValue.HasValue: true }:
                return true;
            case ILocalReferenceOperation or IParameterReferenceOperation:
                references.Add(operation);
                return true;
            case IBinaryOperation or IUnaryOperation or IConversionOperation or IParenthesizedOperation or IConditionalOperation:
                return operation.ChildOperations.All(child => CollectInvariantReferences(child, references));
            default:
                return false;
        }
    }

    private static ISymbol? GetVariable(IOperation reference) =>
        reference switch
        {
            ILocalReferenceOperation { Local.RefKind: RefKind.None } local => local.Local,
            IParameterReferenceOperation { Parameter.RefKind: RefKind.None } parameter => parameter.Parameter,
            _ => null,
        };

    private static bool IsWritten(IReadOnlyCollection<IOperation> parts, ISymbol variable) =>
        parts.SelectMany(part => part.DescendantsAndSelf()).Any(operation => IsWriteTo(operation, variable));

    // Outside the loop a write can still reach it: a lambda or local function may run from inside the loop through a
    // delegate call, and a ref local or pointer taken earlier writes through an alias.
    private static bool IsWrittenElsewhere(IOperation loop, ISymbol variable)
    {
        var root = loop;
        while (root.Parent is { } parent)
            root = parent;

        return root.DescendantsAndSelf().Any(operation =>
            IsAlias(operation, variable)
            || (IsWriteTo(operation, variable)
                && GetEnclosingFunction(operation) is { } function
                && !SymbolEqualityComparer.Default.Equals(function, variable.ContainingSymbol)));
    }

    private static bool IsAlias(IOperation reference, ISymbol variable) =>
        SymbolEqualityComparer.Default.Equals(GetVariable(reference), variable)
        && (reference.Parent is IAddressOfOperation || reference.Syntax.Parent is RefExpressionSyntax);

    private static ISymbol? GetEnclosingFunction(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IAnonymousFunctionOperation anonymous:
                    return anonymous.Symbol;
                case ILocalFunctionOperation local:
                    return local.Symbol;
            }
        }

        return null;
    }

    private static bool IsWriteTo(IOperation reference, ISymbol variable)
    {
        if (!SymbolEqualityComparer.Default.Equals(GetVariable(reference), variable))
            return false;

        var target = reference;
        while (target.Parent is ITupleOperation tuple)
            target = tuple;

        return target.Parent switch
        {
            IAssignmentOperation assignment => assignment.Target == target,
            IIncrementOrDecrementOperation => true,
            IArgumentOperation argument => argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out,
            _ => false,
        };
    }
}
