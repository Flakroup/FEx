# FEx.Analyzers

Roslyn analyzers for correctness defects and redundant code that the .NET SDK analyzers do not report. Each rule replaces a ReSharper inspection and is specified by a golden corpus recorded on ReSharper, so it fires on the same code ReSharper flags.

## Using the package

Reference the package from a project that should be analyzed. It is a development dependency: it adds nothing to the output and does not flow to consumers.

```xml
<PackageReference Include="FEx.Analyzers" Version="x.y.z" PrivateAssets="all" />
```

Every rule is a warning, enabled by default. Change a severity or switch a rule off in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.FEX0001.severity = suggestion
```

## Rules

### FEX0001

**Non-atomic compound operator on a volatile field**. volatile orders accesses but does not make a compound assignment, ??= or ++/-- atomic; another thread can write between the read and the write. Use Interlocked or a lock. Replaces the ReSharper inspection NonAtomicCompoundOperator.

### FEX0002

**Loop variable is never changed inside the loop**. A condition made only of locals, parameters and constants, none of which the loop writes (directly, by ref, or through a closure), cannot change, so the loop runs zero times or forever. Fields, properties and calls in the condition are not examined. Replaces the ReSharper inspection LoopVariableIsNeverChangedInsideLoop.

### FEX0003

**Variable hides outer variable**. C# allows a lambda or local function to reuse the name of an enclosing local or parameter, so a later edit can read or write the wrong variable without a compiler error. Replaces the ReSharper inspection VariableHidesOuterVariable.

### FEX0004

**Member hides static member from outer class**. Inside the nested type a bare name binds to the nested member, so code written for the outer static member silently binds to the wrong one. Replaces the ReSharper inspection MemberHidesStaticFromOuterClass.

### FEX0005

**Base member has params**. A call through the derived type then needs an explicit array while the same call through the base type does not. Replaces the ReSharper inspection BaseMemberHasParams.

### FEX0006

**Redundant verbatim prefix on an identifier**. The @ prefix only matters when the name is a reserved keyword (or a contextual one the position would otherwise read as a keyword). Replaces the ReSharper inspection RedundantVerbatimPrefix.

### FEX0007

**Redundant verbatim string prefix**. A verbatim string only differs from a regular one when its text holds a backslash, a quote, a line break or another character that needs an escape. Replaces the ReSharper inspection RedundantVerbatimStringPrefix.

### FEX0008

**Redundant parentheses around a lambda parameter**. A lambda with exactly one untyped parameter, no modifier, no attribute and no explicit return type does not need parentheses. Replaces the ReSharper inspection RedundantLambdaSignatureParentheses.

### FEX0009

**Redundant parentheses on an attribute**. An attribute with no arguments does not need empty parentheses. Replaces the ReSharper inspection RedundantAttributeParentheses.

### FEX0010

**Redundant semicolon after a declaration**. A type or namespace body is complete at its closing brace. An empty type body is left alone, as ReSharper does. Replaces the ReSharper inspection RedundantDeclarationSemicolon.

### FEX0011

**Empty statement**. A semicolon that is a statement of its own does nothing. An empty body such as 'if (b);' is the compiler's warning (CS0642) and is left to it. Replaces the ReSharper inspection EmptyStatement.

### FEX0012

**Empty for statement**. A for loop with an empty body, a condition, and a header that only reads values and advances its own counters has no effect. A header that calls a method, creates an object or writes anything else is left alone. Replaces the ReSharper inspection EmptyForStatement.

### FEX0013

**Empty namespace**. A namespace with no member, whatever usings or comments it holds, declares nothing. Replaces the ReSharper inspection EmptyNamespace.

### FEX0014

**Redundant braces around a collection initializer element**. An element written as '{ x }' calls Add(x) exactly as 'x' does. Braces around an assignment (plain or compound) or a collection expression are kept, since without them the element would mean something else or not compile. Replaces the ReSharper inspection RedundantCollectionInitializerElementBraces.

### FEX0015

**Redundant 'ascending' in a query orderby**. An orderby ordering sorts ascending unless it says descending. Replaces the ReSharper inspection RedundantQueryOrderByAscendingKeyword.

### FEX0016

**Redundant empty finally block**. A finally block that runs no statement does nothing. Replaces the ReSharper inspection RedundantEmptyFinallyBlock.

### FEX0017

**Redundant empty default switch section**. A default section that holds nothing but 'break;' behaves as a switch without one, unless a 'goto default;' targets it or conditional-compilation code sits inside it. Replaces the ReSharper inspection RedundantEmptySwitchSection.

### FEX0018

**Redundant case label**. In a section that also has default, a case label only repeats what default already matches, unless it carries a 'when' filter, declares a variable with 'var', or is the target of a 'goto case'. Like ReSharper, it is also reported where the label sits ahead of a later section that overlaps it, so removing it can change which section runs: check that before deleting it. Replaces the ReSharper inspection RedundantCaseLabel.

### FEX0019

**Redundant jump statement**. A return or yield break at the end of a function, a continue at the end of a loop body, a goto to the label that follows it, or a jump that the same jump follows changes nothing. A yield break that is the only yield of the function is kept, since it is what makes the function an iterator. Replaces the ReSharper inspection RedundantJumpStatement.

### FEX0020

**Redundant else**. When the if branch always returns, throws or jumps, the else body runs exactly when it would without the else keyword. Replaces the ReSharper inspection RedundantIfElseBlock.

### FEX0021

**Redundant range bound**. A range starts at 0 and ends at ^0 when it says nothing, so '0..x' means '..x' and 'x..^0' means 'x..'. A constant that folds to 0 counts, and so does new Index(0). Replaces the ReSharper inspection RedundantRangeBound.

### FEX0022

**Redundant ToString call**. String concatenation, an interpolation hole and the arguments of string.Format call ToString themselves, and string.ToString returns the string it was called on. Replaces the ReSharper inspection RedundantToStringCall.

### FEX0023

**Redundant ToString call on a value type**. Concatenating a string with a value calls ToString on it, and on current runtimes without boxing the value first. Replaces the ReSharper inspection RedundantToStringCallForValueType.

### FEX0024

**Redundant string.Format call**. string.Format with a literal that has no placeholder and no arguments returns the literal, with its doubled braces collapsed. Replaces the ReSharper inspection RedundantStringFormatCall.

### FEX0025

**Redundant string interpolation**. A $"..." string without a {hole} is a plain string literal. Replaces the ReSharper inspection RedundantStringInterpolation.

### FEX0026

**Redundant string.ToCharArray call**. A foreach over a string visits its characters, so copying them into an array first only allocates. Replaces the ReSharper inspection RedundantStringToCharArrayCall.

### FEX0027

**Redundant Enumerable cast call**. Cast<T> on a sequence of T returns its elements unchanged. Replaces the ReSharper inspection RedundantEnumerableCastCall.

### FEX0028

**Redundant delegate creation**. A method group, lambda or compatible delegate converts to a delegate type without 'new'. Replaces the ReSharper inspection RedundantDelegateCreation.

### FEX0029

**Redundant argument default value**. A trailing optional argument that repeats the parameter's default can be left out. Replaces the ReSharper inspection RedundantArgumentDefaultValue.

### FEX0030

**Redundant explicit nullable creation**. A value converts to its nullable type without 'new T?(value)' wherever the target type is known. Replaces the ReSharper inspection RedundantExplicitNullableCreation.

### FEX0031

**Redundant property pattern clause**. 'x is string { }' tests exactly what 'x is string' does. Replaces the ReSharper inspection RedundantPropertyPatternClause.

### FEX0032

**Redundant logical conditional expression operand**. 'a && true' and 'a || false' are 'a'. Replaces the ReSharper inspection RedundantLogicalConditionalExpressionOperand.

### FEX0033

**Redundant or-assignment of false**. 'flag |= false;' assigns the value flag already has. Replaces the ReSharper inspection RemoveRedundantOrStatement.False.

### FEX0034

**Redundant check before assignment**. 'if (x != y) x = y;' leaves x equal to y whether or not the check runs. Replaces the ReSharper inspection RedundantCheckBeforeAssignment.

### FEX0035

**Redundant catch clause**. A catch clause that does nothing but 'throw;' behaves as if absent, unless it keeps a later clause from seeing the exception. Replaces the ReSharper inspection RedundantCatchClause.

### FEX0036

**Redundant base constructor call**. A constructor, or a primary constructor's base type, with no initializer already calls base(). Replaces the ReSharper inspection RedundantBaseConstructorCall.

### FEX0037

**Parameter hides member**. Inside the body the plain name means the parameter, so a read or write meant for the member silently goes to the parameter. Constructor parameters are exempt, as in ReSharper. Replaces the ReSharper inspection ParameterHidesMember.

### FEX0038

**Local variable hides member**. Inside the scope the plain name means the local, so a read or write meant for the member silently goes to the local. Replaces the ReSharper inspection LocalVariableHidesMember.

### FEX0039

**Redundant overload (Global)**. Callers of the sibling get the same call without the extra overload, which only adds a second candidate to overload resolution. Replaces the ReSharper inspection RedundantOverload.Global.

### FEX0040

**Redundant overload (Local)**. Callers of the sibling get the same call without the extra overload, which only adds a second candidate to overload resolution. Replaces the ReSharper inspection RedundantOverload.Local.

### FEX0041

**Method with optional parameter is hidden by overload**. Overload resolution prefers the candidate that needs no default argument, so the optional parameter is never used by a call that leaves it out. Replaces the ReSharper inspection MethodOverloadWithOptionalParameter.

### FEX0042

**Optional parameter default differs in the hierarchy**. The compiler takes the default from the static type of the receiver, so the same call through the base type and through the derived type passes different arguments. Replaces the ReSharper inspection OptionalParameterHierarchyMismatch.

### FEX0043

**Optional parameter is ref or out**. C# callers must still pass a variable, so the attribute promises an optional argument the language never honors. Replaces the ReSharper inspection OptionalParameterRefOut.

### FEX0044

**Partial method has a single part**. Without an implementation the declaration and its calls compile to nothing. Replaces the ReSharper inspection PartialMethodWithSinglePart.

### FEX0045

**Partial type has a single part**. The modifier promises a second declaration that does not exist, which hides whether generated code is missing. Replaces the ReSharper inspection PartialTypeWithSinglePart.

### FEX0046

**Unused parameter in partial method**. The other part decides the signature, so the parameter cannot simply be dropped - which is why the compiler's own unused-parameter check stays silent. Replaces the ReSharper inspection UnusedParameterInPartialMethod.

### FEX0047

**Redundant overridden member**. The override only repeats the base member. It is not reported where removing it would still change something: a record's synthesized members, a default value or a parameter attribute that differs, an interface the type implements again, or an Equals/GetHashCode pair next to a hand-written counterpart or an == operator. Replaces the ReSharper inspection RedundantOverriddenMember.

### FEX0048

**Unused type parameter**. Callers have to spell out or infer an argument that changes nothing. A type parameter an interface, a base class or a documentation comment mentions counts as used. Replaces the ReSharper inspection UnusedTypeParameter.

### FEX0049

**Unused method return value (local)**. Only calls in the declaring type can exist, so nobody could ever read the result. Replaces the ReSharper inspection UnusedMethodReturnValue.Local.

### FEX0050

**Value parameter is not used**. Whatever the caller assigns is silently dropped. Overrides, interface implementations and accessors that only throw are exempt. Replaces the ReSharper inspection ValueParameterNotUsed.

### FEX0051

**Convert to constant (local)**. A constant states that the value cannot change and is folded by the compiler. Replaces the ReSharper inspection ConvertToConstant.Local.

### FEX0052

**Method has async overload with cancellation**. The synchronous call can neither be cancelled nor give the thread back while it waits. Replaces the ReSharper inspection MethodHasAsyncOverloadWithCancellation.

### FEX0053

**Use a cancellation token for IAsyncEnumerable**. Cancelling the token then does not stop the enumeration. Replaces the ReSharper inspection UseCancellationTokenForIAsyncEnumerable.

### FEX0054

**Redundant empty argument list in object creation**. 'new T { ... }' calls the same parameterless constructor as 'new T() { ... }'. Replaces the ReSharper inspection RedundantEmptyObjectCreationArgumentList.

### FEX0055

**Redundant empty object or collection initializer**. An initializer with no member or element assigns nothing, so the creation behaves the same without it. Replaces the ReSharper inspection RedundantEmptyObjectOrCollectionInitializer.

### FEX0056

**Invoke as extension member**. Calling an extension method through its class reads as a utility call and hides the chain it belongs to; 'list.Count()' says the same. Replaces the ReSharper inspection InvokeAsExtensionMember.

### FEX0057

**Parameter is only used for a precondition check (Global)**. A parameter that is validated and then never read has no effect on what the body does, so the body either misses a use or the parameter is dead. Replaces the ReSharper inspection ParameterOnlyUsedForPreconditionCheck.Global.

### FEX0058

**Parameter is only used for a precondition check (Local)**. A parameter that is validated and then never read has no effect on what the body does, so the body either misses a use or the parameter is dead. Replaces the ReSharper inspection ParameterOnlyUsedForPreconditionCheck.Local.
