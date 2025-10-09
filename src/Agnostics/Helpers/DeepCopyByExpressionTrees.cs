// Made by Frantisek Konopecky, Prague, 2014 - 2016
//
// Code comes under MIT licence - Can be used without 
// limitations for both personal and commercial purposes.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FEx.Agnostics.Helpers;

/// <summary>
///     Superfast deep copier class, which uses Expression trees.
/// </summary>
public static class DeepCopyByExpressionTrees
{
    private static readonly object IsStructTypeToDeepCopyDictionaryLocker = new();

    private static readonly object CompiledCopyFunctionsDictionaryLocker = new();

    private static readonly Type ObjectType = typeof(object);
    private static readonly Type ObjectDictionaryType = typeof(Dictionary<object, object>);

    private static readonly Type FieldInfoType = typeof(FieldInfo);

    private static readonly MethodInfo SetValueMethod = FieldInfoType.GetMethod("SetValue", [ObjectType, ObjectType]);

    private static readonly Type ThisType = typeof(DeepCopyByExpressionTrees);

    private static readonly MethodInfo DeepCopyByExpressionTreeObjMethod =
        ThisType.GetMethod("DeepCopyByExpressionTreeObj", BindingFlags.NonPublic | BindingFlags.Static);

    private static Dictionary<Type, bool> _isStructTypeToDeepCopyDictionary = [];

    private static Dictionary<Type, Func<object, Dictionary<object, object>, object>> _compiledCopyFunctionsDictionary =
        [];

    /// <summary>
    ///     Creates a deep copy of an object.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="original">Object to copy.</param>
    /// <param name="copiedReferencesDict">Dictionary of already copied objects (Keys: original objects, Values: their copies).</param>
    /// <returns></returns>
    public static T
        DeepCopyByExpressionTree<T>(this T original, Dictionary<object, object> copiedReferencesDict = null) =>
        (T)DeepCopyByExpressionTreeObj(original,
            false,
            copiedReferencesDict ?? new Dictionary<object, object>(new ReferenceEqualityComparer()));

    private static object DeepCopyByExpressionTreeObj(object original,
                                                      bool forceDeepCopy,
                                                      Dictionary<object, object> copiedReferencesDict)
    {
        if (original is null)
            return null;

        Type type = original.GetType();

        if (IsDelegate(type))
            return null;

        if (!forceDeepCopy
            && !IsTypeToDeepCopy(type))
            return original;

        if (copiedReferencesDict.TryGetValue(original, out object alreadyCopiedObject))
            return alreadyCopiedObject;

        if (type == ObjectType)
            return new();

        Func<object, Dictionary<object, object>, object> compiledCopyFunction =
            GetOrCreateCompiledLambdaCopyFunction(type);

        return compiledCopyFunction(original, copiedReferencesDict);
    }

    private static Func<object, Dictionary<object, object>, object> GetOrCreateCompiledLambdaCopyFunction(Type type)
    {
        // The following structure ensures that multiple threads can use the dictionary
        // even while dictionary is locked and being updated by other thread.
        // That is why we do not modify the old dictionary instance but
        // we replace it with a new instance everytime.

        if (!_compiledCopyFunctionsDictionary.TryGetValue(type,
                out Func<object, Dictionary<object, object>, object> compiledCopyFunction))
            lock (CompiledCopyFunctionsDictionaryLocker)
            {
                if (!_compiledCopyFunctionsDictionary.TryGetValue(type, out compiledCopyFunction))
                {
                    Expression<Func<object, Dictionary<object, object>, object>> uncompiledCopyFunction =
                        CreateCompiledLambdaCopyFunctionForType(type);

                    compiledCopyFunction = uncompiledCopyFunction.Compile();

                    var dictionaryCopy =
                        _compiledCopyFunctionsDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

                    dictionaryCopy.Add(type, compiledCopyFunction);

                    _compiledCopyFunctionsDictionary = dictionaryCopy;
                }
            }

        return compiledCopyFunction;
    }

    private static Expression<Func<object, Dictionary<object, object>, object>>
        CreateCompiledLambdaCopyFunctionForType(Type type)
    {
        ///// INITIALIZATION OF EXPRESSIONS AND VARIABLES

        InitializeExpressions(type,
            out ParameterExpression inputParameter,
            out ParameterExpression inputDictionary,
            out ParameterExpression outputVariable,
            out ParameterExpression boxingVariable,
            out LabelTarget endLabel,
            out List<ParameterExpression> variables,
            out List<Expression> expressions);

        ///// RETURN NULL IF ORIGINAL IS NULL

        IfNullThenReturnNullExpression(inputParameter, endLabel, expressions);

        ///// MEMBERWISE CLONE ORIGINAL OBJECT

        MemberwiseCloneInputToOutputExpression(type, inputParameter, outputVariable, expressions);

        ///// STORE COPIED OBJECT TO REFERENCES DICTIONARY

        if (IsClassOtherThanString(type))
            StoreReferencesIntoDictionaryExpression(inputParameter, inputDictionary, outputVariable, expressions);

        ///// COPY ALL NONVALUE OR NONPRIMITIVE FIELDS

        FieldsCopyExpressions(type, inputParameter, inputDictionary, outputVariable, boxingVariable, expressions);

        ///// COPY ELEMENTS OF ARRAY

        if (IsArray(type)
            && IsTypeToDeepCopy(type.GetElementType()))
            CreateArrayCopyLoopExpression(type,
                inputParameter,
                inputDictionary,
                outputVariable,
                variables,
                expressions);

        ///// COMBINE ALL EXPRESSIONS INTO LAMBDA FUNCTION

        return CombineAllIntoLambdaFunctionExpression(inputParameter,
            inputDictionary,
            outputVariable,
            endLabel,
            variables,
            expressions);
    }

    private static void InitializeExpressions(Type type,
                                              out ParameterExpression inputParameter,
                                              out ParameterExpression inputDictionary,
                                              out ParameterExpression outputVariable,
                                              out ParameterExpression boxingVariable,
                                              out LabelTarget endLabel,
                                              out List<ParameterExpression> variables,
                                              out List<Expression> expressions)
    {
        inputParameter = Expression.Parameter(ObjectType);

        inputDictionary = Expression.Parameter(ObjectDictionaryType);

        outputVariable = Expression.Variable(type);

        boxingVariable = Expression.Variable(ObjectType);

        endLabel = Expression.Label();

        variables = [];

        expressions = [];

        variables.Add(outputVariable);
        variables.Add(boxingVariable);
    }

    private static void IfNullThenReturnNullExpression(ParameterExpression inputParameter,
                                                       LabelTarget endLabel,
                                                       List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// if (input is null)
        ///// {
        /////     return null;
        ///// }

        ConditionalExpression ifNullThenReturnNullExpression = Expression.IfThen(
            Expression.Equal(inputParameter, Expression.Constant(null, ObjectType)),
            Expression.Return(endLabel));

        expressions.Add(ifNullThenReturnNullExpression);
    }

    private static void MemberwiseCloneInputToOutputExpression(Type type,
                                                               ParameterExpression inputParameter,
                                                               ParameterExpression outputVariable,
                                                               List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// var output = (<type>)input.MemberwiseClone();

        MethodInfo memberwiseCloneMethod =
            ObjectType.GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        BinaryExpression memberwiseCloneInputExpression = Expression.Assign(outputVariable,
            Expression.Convert(Expression.Call(inputParameter, memberwiseCloneMethod), type));

        expressions.Add(memberwiseCloneInputExpression);
    }

    private static void StoreReferencesIntoDictionaryExpression(ParameterExpression inputParameter,
                                                                ParameterExpression inputDictionary,
                                                                ParameterExpression outputVariable,
                                                                List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// inputDictionary[(Object)input] = (Object)output;

        BinaryExpression storeReferencesExpression = Expression.Assign(
            Expression.Property(inputDictionary, ObjectDictionaryType.GetProperty("Item"), inputParameter),
            Expression.Convert(outputVariable, ObjectType));

        expressions.Add(storeReferencesExpression);
    }

    private static Expression<Func<object, Dictionary<object, object>, object>> CombineAllIntoLambdaFunctionExpression(
        ParameterExpression inputParameter,
        ParameterExpression inputDictionary,
        ParameterExpression outputVariable,
        LabelTarget endLabel,
        List<ParameterExpression> variables,
        List<Expression> expressions)
    {
        expressions.Add(Expression.Label(endLabel));

        expressions.Add(Expression.Convert(outputVariable, ObjectType));

        BlockExpression finalBody = Expression.Block(variables, expressions);

        return Expression.Lambda<Func<object, Dictionary<object, object>, object>>(finalBody,
            inputParameter,
            inputDictionary);
    }

    private static void CreateArrayCopyLoopExpression(Type type,
                                                      ParameterExpression inputParameter,
                                                      ParameterExpression inputDictionary,
                                                      ParameterExpression outputVariable,
                                                      List<ParameterExpression> variables,
                                                      List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// int i1, i2, ..., in; 
        ///// 
        ///// int length1 = inputarray.GetLength(0); 
        ///// i1 = 0; 
        ///// while (true)
        ///// {
        /////     if (i1 >= length1)
        /////     {
        /////         goto ENDLABELFORLOOP1;
        /////     }
        /////     int length2 = inputarray.GetLength(1); 
        /////     i2 = 0; 
        /////     while (true)
        /////     {
        /////         if (i2 >= length2)
        /////         {
        /////             goto ENDLABELFORLOOP2;
        /////         }
        /////         ...
        /////         ...
        /////         ...
        /////         int lengthn = inputarray.GetLength(n); 
        /////         in = 0; 
        /////         while (true)
        /////         {
        /////             if (in >= lengthn)
        /////             {
        /////                 goto ENDLABELFORLOOPn;
        /////             }
        /////             outputarray[i1, i2, ..., in] 
        /////                 = (<elementType>)DeepCopyByExpressionTreeObj(
        /////                        (Object)inputarray[i1, i2, ..., in])
        /////             in++; 
        /////         }
        /////         ENDLABELFORLOOPn:
        /////         ...
        /////         ...  
        /////         ...
        /////         i2++; 
        /////     }
        /////     ENDLABELFORLOOP2:
        /////     i1++; 
        ///// }
        ///// ENDLABELFORLOOP1:

        int rank = type.GetArrayRank();

        List<ParameterExpression> indices = GenerateIndices(rank);

        variables.AddRange(indices);

        Type elementType = type.GetElementType();

        Expression forExpression = ArrayFieldToArrayFieldAssignExpression(inputParameter,
            inputDictionary,
            outputVariable,
            elementType,
            type,
            indices);

        for (var dimension = 0; dimension < rank; dimension++)
        {
            ParameterExpression indexVariable = indices[dimension];

            forExpression = LoopIntoLoopExpression(inputParameter, indexVariable, forExpression, dimension);
        }

        expressions.Add(forExpression);
    }

    private static List<ParameterExpression> GenerateIndices(int arrayRank)
    {
        ///// Intended code:
        /////
        ///// int i1, i2, ..., in; 

        var indices = new List<ParameterExpression>();

        for (var i = 0; i < arrayRank; i++)
        {
            ParameterExpression indexVariable = Expression.Variable(typeof(int));

            indices.Add(indexVariable);
        }

        return indices;
    }

    private static BinaryExpression ArrayFieldToArrayFieldAssignExpression(
        ParameterExpression inputParameter,
        ParameterExpression inputDictionary,
        ParameterExpression outputVariable,
        Type elementType,
        Type arrayType,
        List<ParameterExpression> indices)
    {
        ///// Intended code:
        /////
        ///// outputarray[i1, i2, ..., in] 
        /////     = (<elementType>)DeepCopyByExpressionTreeObj(
        /////            (Object)inputarray[i1, i2, ..., in]);

        IndexExpression indexTo = Expression.ArrayAccess(outputVariable, indices);

        MethodCallExpression indexFrom = Expression.ArrayIndex(Expression.Convert(inputParameter, arrayType), indices);

        bool forceDeepCopy = elementType != ObjectType;

        UnaryExpression rightSide = Expression.Convert(Expression.Call(DeepCopyByExpressionTreeObjMethod,
                Expression.Convert(indexFrom, ObjectType),
                Expression.Constant(forceDeepCopy, typeof(bool)),
                inputDictionary),
            elementType);

        return Expression.Assign(indexTo, rightSide);
    }

    private static BlockExpression LoopIntoLoopExpression(ParameterExpression inputParameter,
                                                          ParameterExpression indexVariable,
                                                          Expression loopToEncapsulate,
                                                          int dimension)
    {
        ///// Intended code:
        /////
        ///// int length = inputarray.GetLength(dimension); 
        ///// i = 0; 
        ///// while (true)
        ///// {
        /////     if (i >= length)
        /////     {
        /////         goto ENDLABELFORLOOP;
        /////     }
        /////     loopToEncapsulate;
        /////     i++; 
        ///// }
        ///// ENDLABELFORLOOP:

        ParameterExpression lengthVariable = Expression.Variable(typeof(int));

        LabelTarget endLabelForThisLoop = Expression.Label();

        LoopExpression newLoop = Expression.Loop(Expression.Block([],
                Expression.IfThen(Expression.GreaterThanOrEqual(indexVariable, lengthVariable),
                    Expression.Break(endLabelForThisLoop)),
                loopToEncapsulate,
                Expression.PostIncrementAssign(indexVariable)),
            endLabelForThisLoop);

        BinaryExpression lengthAssignment = GetLengthForDimensionExpression(lengthVariable, inputParameter, dimension);

        BinaryExpression indexAssignment = Expression.Assign(indexVariable, Expression.Constant(0));

        return Expression.Block([lengthVariable], lengthAssignment, indexAssignment, newLoop);
    }

    private static BinaryExpression GetLengthForDimensionExpression(ParameterExpression lengthVariable,
                                                                    ParameterExpression inputParameter,
                                                                    int i)
    {
        ///// Intended code:
        /////
        ///// length = ((Array)input).GetLength(i); 

        MethodInfo getLengthMethod = typeof(Array).GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance);

        ConstantExpression dimensionConstant = Expression.Constant(i);

        return Expression.Assign(lengthVariable,
            Expression.Call(Expression.Convert(inputParameter, typeof(Array)), getLengthMethod, dimensionConstant));
    }

    private static void FieldsCopyExpressions(Type type,
                                              ParameterExpression inputParameter,
                                              ParameterExpression inputDictionary,
                                              ParameterExpression outputVariable,
                                              ParameterExpression boxingVariable,
                                              List<Expression> expressions)
    {
        FieldInfo[] fields = GetAllRelevantFields(type);

        var readonlyFields = fields.Where(f => f.IsInitOnly).ToList();
        var writableFields = fields.Where(f => !f.IsInitOnly).ToList();

        ///// READONLY FIELDS COPY (with boxing)

        bool shouldUseBoxing = readonlyFields.Count > 0;

        if (shouldUseBoxing)
        {
            BinaryExpression boxingExpression =
                Expression.Assign(boxingVariable, Expression.Convert(outputVariable, ObjectType));

            expressions.Add(boxingExpression);
        }

        foreach (FieldInfo field in readonlyFields)
        {
            if (IsDelegate(field.FieldType))
                ReadonlyFieldToNullExpression(field, boxingVariable, expressions);
            else
                ReadonlyFieldCopyExpression(type, field, inputParameter, inputDictionary, boxingVariable, expressions);
        }

        if (shouldUseBoxing)
        {
            BinaryExpression unboxingExpression =
                Expression.Assign(outputVariable, Expression.Convert(boxingVariable, type));

            expressions.Add(unboxingExpression);
        }

        ///// NOT-READONLY FIELDS COPY

        foreach (FieldInfo field in writableFields)
        {
            if (IsDelegate(field.FieldType))
                WritableFieldToNullExpression(field, outputVariable, expressions);
            else
                WritableFieldCopyExpression(type, field, inputParameter, inputDictionary, outputVariable, expressions);
        }
    }

    private static FieldInfo[] GetAllRelevantFields(Type type, bool forceAllFields = false)
    {
        var fieldsList = new List<FieldInfo>();

        Type typeCache = type;

        while (typeCache is not null)
        {
            fieldsList.AddRange(typeCache
                .GetFields(BindingFlags.Instance
                           | BindingFlags.Public
                           | BindingFlags.NonPublic
                           | BindingFlags.FlattenHierarchy)
                .Where(field => forceAllFields || IsTypeToDeepCopy(field.FieldType)));

            typeCache = typeCache.BaseType;
        }

        return [.. fieldsList];
    }

    private static FieldInfo[] GetAllFields(Type type) => GetAllRelevantFields(type, true);

    private static void ReadonlyFieldToNullExpression(FieldInfo field,
                                                      ParameterExpression boxingVariable,
                                                      List<Expression> expressions)
    {
        // This option must be implemented by Reflection because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        /////
        ///// fieldInfo.SetValue(boxing, <fieldtype>null);

        MethodCallExpression fieldToNullExpression = Expression.Call(Expression.Constant(field),
            SetValueMethod,
            boxingVariable,
            Expression.Constant(null, field.FieldType));

        expressions.Add(fieldToNullExpression);
    }

    private static void ReadonlyFieldCopyExpression(Type type,
                                                    FieldInfo field,
                                                    ParameterExpression inputParameter,
                                                    ParameterExpression inputDictionary,
                                                    ParameterExpression boxingVariable,
                                                    List<Expression> expressions)
    {
        // This option must be implemented by Reflection (SetValueMethod) because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        /////
        ///// fieldInfo.SetValue(boxing, DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>))

        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);

        bool forceDeepCopy = field.FieldType != ObjectType;

        MethodCallExpression fieldDeepCopyExpression = Expression.Call(Expression.Constant(field, FieldInfoType),
            SetValueMethod,
            boxingVariable,
            Expression.Call(DeepCopyByExpressionTreeObjMethod,
                Expression.Convert(fieldFrom, ObjectType),
                Expression.Constant(forceDeepCopy, typeof(bool)),
                inputDictionary));

        expressions.Add(fieldDeepCopyExpression);
    }

    private static void WritableFieldToNullExpression(FieldInfo field,
                                                      ParameterExpression outputVariable,
                                                      List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// output.<field> = (<type>)null;

        MemberExpression fieldTo = Expression.Field(outputVariable, field);

        BinaryExpression fieldToNullExpression = Expression.Assign(fieldTo, Expression.Constant(null, field.FieldType));

        expressions.Add(fieldToNullExpression);
    }

    private static void WritableFieldCopyExpression(Type type,
                                                    FieldInfo field,
                                                    ParameterExpression inputParameter,
                                                    ParameterExpression inputDictionary,
                                                    ParameterExpression outputVariable,
                                                    List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// output.<field> = (<fieldType>)DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>);

        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);

        Type fieldType = field.FieldType;

        MemberExpression fieldTo = Expression.Field(outputVariable, field);

        bool forceDeepCopy = field.FieldType != ObjectType;

        BinaryExpression fieldDeepCopyExpression = Expression.Assign(fieldTo,
            Expression.Convert(Expression.Call(DeepCopyByExpressionTreeObjMethod,
                    Expression.Convert(fieldFrom, ObjectType),
                    Expression.Constant(forceDeepCopy, typeof(bool)),
                    inputDictionary),
                fieldType));

        expressions.Add(fieldDeepCopyExpression);
    }

    private static bool IsArray(Type type) => type.IsArray;

    private static bool IsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type);

    private static bool IsTypeToDeepCopy(Type type) => IsClassOtherThanString(type) || IsStructWhichNeedsDeepCopy(type);

    private static bool IsClassOtherThanString(Type type) => !type.IsValueType && type != typeof(string);

    private static bool IsStructWhichNeedsDeepCopy(Type type)
    {
        // The following structure ensures that multiple threads can use the dictionary
        // even while dictionary is locked and being updated by other thread.
        // That is why we do not modify the old dictionary instance but
        // we replace it with a new instance everytime.

        if (!_isStructTypeToDeepCopyDictionary.TryGetValue(type, out bool isStructTypeToDeepCopy))
            lock (IsStructTypeToDeepCopyDictionaryLocker)
            {
                if (!_isStructTypeToDeepCopyDictionary.TryGetValue(type, out isStructTypeToDeepCopy))
                {
                    isStructTypeToDeepCopy = IsStructWhichNeedsDeepCopy_NoDictionaryUsed(type);

                    var newDictionary =
                        _isStructTypeToDeepCopyDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

                    newDictionary[type] = isStructTypeToDeepCopy;

                    _isStructTypeToDeepCopyDictionary = newDictionary;
                }
            }

        return isStructTypeToDeepCopy;
    }

    private static bool IsStructWhichNeedsDeepCopy_NoDictionaryUsed(Type type) =>
        IsStructOtherThanBasicValueTypes(type) && HasInItsHierarchyFieldsWithClasses(type);

    private static bool IsStructOtherThanBasicValueTypes(Type type) =>
        type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);

    private static bool HasInItsHierarchyFieldsWithClasses(Type type, HashSet<Type> alreadyCheckedTypes = null)
    {
        alreadyCheckedTypes = alreadyCheckedTypes ?? [];

        alreadyCheckedTypes.Add(type);

        FieldInfo[] allFields = GetAllFields(type);

        var allFieldTypes = allFields.Select(f => f.FieldType).Distinct().ToList();

        bool hasFieldsWithClasses = allFieldTypes.Any(IsClassOtherThanString);

        if (hasFieldsWithClasses)
            return true;

        var notBasicStructsTypes = allFieldTypes.Where(IsStructOtherThanBasicValueTypes).ToList();

        foreach (Type typeToCheck in notBasicStructsTypes.Where(t => !alreadyCheckedTypes.Contains(t)).ToList())
        {
            if (HasInItsHierarchyFieldsWithClasses(typeToCheck, alreadyCheckedTypes))
                return true;
        }

        return false;
    }
}

internal class ReferenceEqualityComparer : EqualityComparer<object>
{
    public override bool Equals(object x, object y) => ReferenceEquals(x, y);

    public override int GetHashCode(object obj) =>
        obj is null
            ? 0
            : obj.GetHashCode();
}