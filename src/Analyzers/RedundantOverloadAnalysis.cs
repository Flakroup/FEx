using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FEx.Analyzers;

/// <summary>What FEX0039 and FEX0040 share: finding a method or constructor that does nothing but call a sibling with its own defaults filled in.</summary>
internal static class RedundantOverloadAnalysis
{
    /// <summary>
    /// Starts FEX0039 (<paramref name="local"/> false) or FEX0040 (true): the redundant overloads in the compilation, minus
    /// those some code uses as a method group, because <c>Action&lt;int&gt; f = r.M;</c> binds the forwarder and not the
    /// sibling, which takes two arguments. Generated code (Razor binds handlers that way) is read for those uses and never reported.
    /// </summary>
    public static void Start(CompilationStartAnalysisContext start, DiagnosticDescriptor rule, bool local)
    {
        var candidates = new ConcurrentDictionary<IMethodSymbol, Diagnostic>(SymbolEqualityComparer.Default);
        var methodGroups = new ConcurrentDictionary<IMethodSymbol, bool>(SymbolEqualityComparer.Default);
        start.RegisterSyntaxNodeAction(
            node =>
            {
                if (FindRedundant(node) is { } method
                    && AnalyzerHelpers.IsEffectivelyPrivate(method) == local
                    && !AnalyzerHelpers.IsGenerated(node.Node.SyntaxTree, method, node.Options, node.CancellationToken))
                    candidates[method] = Diagnostic.Create(rule, method.Locations[0], method.Name == ".ctor" ? method.ContainingType.Name : method.Name);
            },
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration);
        start.RegisterOperationAction(operation => methodGroups[((IMethodReferenceOperation)operation.Operation).Method.OriginalDefinition] = true, OperationKind.MethodReference);
        start.RegisterCompilationEndAction(end =>
        {
            foreach (var pair in candidates.Where(pair => !methodGroups.ContainsKey(pair.Key)))
                end.ReportDiagnostic(pair.Value);
        });
    }

    /// <summary>
    /// The method the node declares, when it is a block-bodied overload (or a constructor chaining to <c>this(...)</c>
    /// with an empty body) that forwards its parameters in order to a sibling and fills each remaining parameter of the
    /// sibling with exactly that parameter's default - a call the optional parameter makes unnecessary.
    /// The sibling must be the only way to call it with that many arguments: another overload that accepts as many would
    /// bind differently once this one is gone.
    /// </summary>
    public static IMethodSymbol? FindRedundant(SyntaxNodeAnalysisContext context)
    {
        CSharpSyntaxNode? call = context.Node switch
        {
            MethodDeclarationSyntax { ExpressionBody: null, Body.Statements: [var statement] } => statement switch
            {
                ExpressionStatementSyntax expression => expression.Expression,
                ReturnStatementSyntax @return => @return.Expression,
                _ => null,
            },
            ConstructorDeclarationSyntax { ExpressionBody: null, Body.Statements.Count: 0, Initializer: { } initializer } => initializer,
            _ => null,
        };

        if (call is null || !IsForwardingCall(call))
            return null;

        var method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)!;
        if (method is { IsVirtual: true } or { IsOverride: true } or { IsVararg: true }
            // 'params' callers expand into the forwarder, which the sibling's array parameter does not take.
            || method.Parameters.Any(parameter => parameter.IsParams)
            // 'new T()' and every serializer look a parameterless constructor up by its shape, not by what it can be called with.
            || method is { MethodKind: MethodKind.Constructor, Parameters.Length: 0 }
            || context.SemanticModel.GetOperation(call, context.CancellationToken) is not IInvocationOperation invocation)
            return null;

        var target = invocation.TargetMethod;
        if (!IsSibling(method, target) || !ForwardsWithDefaults(method, invocation) || AnalyzerHelpers.ImplementsInterfaceMember(method))
            return null;

        var siblings = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>();

        return siblings.Any(other => !SymbolEqualityComparer.Default.Equals(other, method) && !SymbolEqualityComparer.Default.Equals(other, target.OriginalDefinition) && CanBeCalledWith(other, method.Parameters.Length))
            ? null
            : method;
    }

    // `M(...)` or `this.M(...)` with no explicit type arguments; anything else (an extension call, another receiver) is a different call.
    private static bool IsForwardingCall(CSharpSyntaxNode call) =>
        call is ConstructorInitializerSyntax
        || call is InvocationExpressionSyntax
        {
            Expression: IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax },
        };

    private static bool IsSibling(IMethodSymbol method, IMethodSymbol target) =>
        method.Name == target.Name
        && (target.Arity == 0 || target.Arity == method.Arity)
        && method.IsStatic == target.IsStatic
        && !target.IsVararg
        && IsAsAccessible(target.DeclaredAccessibility, method.DeclaredAccessibility)
        && SymbolEqualityComparer.Default.Equals(method.ContainingType, target.ContainingType)
        && SymbolEqualityComparer.Default.Equals(method.ReturnType, target.ReturnType);

    // Callers of the forwarder have to reach the sibling too: 'public M(a)' next to 'private M(a, b = 0)' is no redundancy, nor is 'protected' next to 'internal'.
    private static bool IsAsAccessible(Accessibility sibling, Accessibility forwarder) =>
        sibling == forwarder
        || sibling == Accessibility.Public
        || sibling == Accessibility.ProtectedOrInternal && forwarder != Accessibility.Public
        || sibling is Accessibility.Protected or Accessibility.Internal && forwarder is Accessibility.ProtectedAndInternal or Accessibility.Private
        || sibling == Accessibility.ProtectedAndInternal && forwarder == Accessibility.Private;

    private static bool ForwardsWithDefaults(IMethodSymbol method, IInvocationOperation invocation)
    {
        var own = method.Parameters;
        var forwarded = invocation.TargetMethod.Parameters;
        if (forwarded.Length <= own.Length)
            return false;

        for (var i = 0; i < own.Length; i++)
        {
            // M(a, b) next to M(a, b = 0) is a different method, not a shorter spelling of the same one.
            if (own[i].IsOptional != forwarded[i].IsOptional
                || own[i].IsOptional && !AnalyzerHelpers.DefaultsEqual(own[i], forwarded[i])
                // By reference or dynamic, the sibling is no stand-in a caller could not tell from the forwarder.
                || own[i].RefKind != RefKind.None || forwarded[i].RefKind != RefKind.None
                || own[i].Type.TypeKind == TypeKind.Dynamic || forwarded[i].Type.TypeKind == TypeKind.Dynamic)
                return false;
        }

        // A caller-info parameter the forwarder fills with a constant would be filled with the call site's own once it is gone.
        return forwarded.Skip(own.Length).All(parameter => !IsCallerInfo(parameter))
               && invocation.Arguments.All(argument => IsForwardedOrDefault(argument, own, forwarded));
    }

    private static bool IsCallerInfo(IParameterSymbol parameter) =>
        parameter.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name is "CallerMemberNameAttribute" or "CallerFilePathAttribute" or "CallerLineNumberAttribute" or "CallerArgumentExpressionAttribute");

    private static bool IsForwardedOrDefault(IArgumentOperation argument, ImmutableArray<IParameterSymbol> own, ImmutableArray<IParameterSymbol> forwarded)
    {
        // Only the argument of a '__arglist' belongs to no parameter, and IsSibling has turned such a target away.
        var position = argument.Parameter!.Ordinal;

        // A default the call leaves out is the sibling's own, which is exactly what the forwarder must pass.
        return position < own.Length
            ? argument.Value is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, own[position])
            : argument.ArgumentKind == ArgumentKind.DefaultValue
              || argument is { ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Initializer.ElementValues.IsEmpty: true } }
              || argument.ArgumentKind == ArgumentKind.Explicit
              && forwarded[position].IsOptional
              && (argument.Value.ConstantValue is { HasValue: true } constant
                  ? forwarded[position].HasExplicitDefaultValue && AnalyzerHelpers.ValuesEqual(constant.Value, forwarded[position].ExplicitDefaultValue)
                  // 'default' for a struct has no constant form, and is what 'CancellationToken ct = default' spells; such a parameter has no explicit default value.
                  : WithoutImplicitConversions(argument.Value) is IDefaultValueOperation && (!forwarded[position].HasExplicitDefaultValue || forwarded[position].ExplicitDefaultValue is null));
    }

    private static IOperation WithoutImplicitConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;

        return operation;
    }

    private static bool CanBeCalledWith(IMethodSymbol method, int arguments)
    {
        var parameters = method.Parameters;

        return parameters.Count(parameter => !parameter.IsOptional) <= arguments && arguments <= parameters.Length;
    }
}
