using FEx.Basics;
using FEx.Extensions.Collections.Lists;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace FEx.Fundamentals.StackTraces;

public class StackTraceGenerator : IStackTraceProvider
{
    private readonly IStackTraceFilter[] _stackTraceFilters;
    private IStackTraceCache _stackTraceCache;

    public bool DotNotFixDynamicProxyStackTrace { get; set; }

    public StackTraceGenerator(params IStackTraceFilter[] stackTraceFilters)
    {
        string str;
        _stackTraceFilters = stackTraceFilters ?? new IStackTraceFilter[] { };
        try
        {
            CheatClrAndUseDynamicMethodsToGetStackTraceFast();
            _stackTraceCache.GetStackTrace();
        }
        catch (Exception exception1)
        {
            Exception exception = exception1;
            if (exception is not null)
                str = exception.ToString();
            else
                str = null;

            Trace.WriteLine(string.Concat(
                "Could not create fast stack trace cache, falling back to old supported way, failure because: ", str));
            SlowAndSafeApproachToGetStackTrace();
        }
    }

    public StackTraceInfo GetStackTraceInfo(string logger = null, string message = null)
    {
        if (!RequiresStackTrace(logger, message))
            return null;

        var stackTraceFrames = new List<StackTraceFrame>();
        StackFrame[] frames = GetStackTrace().GetFrames();
        if (frames is null)
            return null;

        StackFrame[] stackFrameArray = frames;
        for (var i = 0; i < stackFrameArray.Length; i++)
        {
            StackFrame stackFrame = stackFrameArray[i];
            Type declaringType = stackFrame.GetMethod().DeclaringType;
            if (declaringType is not null)
                stackTraceFrames.Add(new()
                {
                    Column = stackFrame.GetFileColumnNumber(),
                    Line = stackFrame.GetFileLineNumber(),
                    FullFilename = stackFrame.GetFileName(),
                    Method = stackFrame.GetMethod().Name,
                    Type = declaringType.FullName,
                    Namespace = declaringType.Namespace ?? ""
                });
        }

        return new()
        {
            Frames = stackTraceFrames.ToArray()
        };
    }

    public StackTrace GetStackTrace() => _stackTraceCache.GetStackTrace();

    private void CheatClrAndUseDynamicMethodsToGetStackTraceFast()
    {
        Type type = typeof(object).Assembly.GetType("System.Diagnostics.StackFrameHelper");
        FieldInfo field = type.GetField("rgMethodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo fieldInfo = type.GetField("rgiILOffset", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo method = Type.GetType("System.Diagnostics.StackTrace, mscorlib")
            .GetMethod("GetStackFramesInternal", BindingFlags.Static | BindingFlags.NonPublic);
        var dynamicMethod = new DynamicMethod("GetStackTraceFast", typeof(MethodHandleAndILOffset[]), Type.EmptyTypes,
            type, true);
        ConstructorInfo constructors = type.GetConstructors()[0];
        bool length = constructors.GetParameters().Length == 2;
        ILGenerator lGenerator = dynamicMethod.GetILGenerator();
        lGenerator.DeclareLocal(type);
        if (length)
            lGenerator.Emit(OpCodes.Ldc_I4_0);

        lGenerator.Emit(OpCodes.Ldnull);
        lGenerator.Emit(OpCodes.Newobj, constructors);
        lGenerator.Emit(OpCodes.Stloc_0);
        lGenerator.Emit(OpCodes.Ldloc_0);
        lGenerator.Emit(OpCodes.Ldc_I4_0);
        if (!length)
            lGenerator.Emit(OpCodes.Ldc_I4_0);

        lGenerator.Emit(OpCodes.Ldnull);
        lGenerator.Emit(OpCodes.Call, method);
        lGenerator.Emit(OpCodes.Ldloc_0);
        lGenerator.Emit(OpCodes.Ldfld, field);
        lGenerator.Emit(OpCodes.Ldloc_0);
        lGenerator.Emit(OpCodes.Ldfld, fieldInfo);
        lGenerator.Emit(OpCodes.Call, typeof(MethodHandleAndILOffset).GetMethod("Create"));
        lGenerator.Emit(OpCodes.Ret);
        var getMethodRuntimeHandle =
            (GetMethodRuntimeHandles)dynamicMethod.CreateDelegate(typeof(GetMethodRuntimeHandles));
        _stackTraceCache = new StackTraceCache(() => new(getMethodRuntimeHandle()));
    }

    private bool RequiresStackTrace(string logger, string message)
    {
        IStackTraceFilter[] stackTraceFilterArray = _stackTraceFilters;

        if (stackTraceFilterArray.IsNullOrEmptyList())
            return true;

        for (var i = 0; i < stackTraceFilterArray.Length; i++)
        {
            if (stackTraceFilterArray[i].Applies(logger, message))
                return true;
        }

        return false;
    }

    private void SlowAndSafeApproachToGetStackTrace()
    {
        _stackTraceCache = new StackTraceCache(() =>
        {
            StackFrame[] frames = new StackTrace(false).GetFrames();
            if (frames is null)
                return new(new MethodHandleAndILOffset[0]);

            var methodHandleAndIlOffset = new MethodHandleAndILOffset[frames.Length];
            for (var i = 0; i < methodHandleAndIlOffset.Length; i++)
                methodHandleAndIlOffset[i] = new(frames[i].GetMethod().MethodHandle.Value, frames[i].GetILOffset());

            return new(methodHandleAndIlOffset);
        });
    }

    private delegate Key GetKey();

    private delegate MethodHandleAndILOffset[] GetMethodRuntimeHandles();

    private interface IStackTraceCache
    {
        StackTrace GetStackTrace();
    }

    private class Key
    {
        private readonly MethodHandleAndILOffset[] _items;

        public Key(MethodHandleAndILOffset[] items)
        {
            _items = items;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            return this == obj || Equals(obj as Key);
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            for (var i = 0; i < _items.Length; i++)
                hashCode = _items[i].GetHashCode() * 397 ^ hashCode;

            return hashCode;
        }

        private bool Equals(Key other)
        {
            if (other is null)
                return false;

            if (this == other)
                return true;

            if (other._items.Length != _items.Length)
                return false;

            for (var i = 0; i < _items.Length; i++)
            {
                if (!other._items[i].Equals(_items[i]))
                    return false;
            }

            return true;
        }
    }

    private class MethodHandleAndILOffset
    {
        // ReSharper disable UnusedMember.Local
        public static MethodHandleAndILOffset[] Create(IntPtr[] methods, int[] offsets)
        // ReSharper restore UnusedMember.Local
        {
            var methodHandleAndILOffset = new MethodHandleAndILOffset[methods.Length];
            for (var i = 0; i < methods.Length; i++)
                methodHandleAndILOffset[i] = new(methods[i], offsets[i]);

            return methodHandleAndILOffset;
        }

        private readonly IntPtr _methodHandle;

        private readonly int _offset;

        public MethodHandleAndILOffset(IntPtr methodHandle, int offset)
        {
            _methodHandle = methodHandle;
            _offset = offset;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            return this == obj || Equals(obj as MethodHandleAndILOffset);
        }

        public override int GetHashCode()
        {
            IntPtr intPtr = _methodHandle;
            return intPtr.GetHashCode() * 397 ^ _offset;
        }

        public bool Equals(MethodHandleAndILOffset other)
        {
            if (other is null)
                return false;

            if (this == other)
                return true;

            return other._methodHandle.Equals(_methodHandle) && other._offset == _offset;
        }
    }

    private class StackTraceCache : IStackTraceCache
    {
        private readonly GetKey _createKey;

        public StackTraceCache(GetKey createKey)
        {
            _createKey = createKey;
            _rwLock = new();
            _cachedTraces = new ConcurrentDictionary<Key, StackTrace>();
        }

        public StackTrace GetStackTrace()
        {
            StackTrace stackTrace;
            StackTrace stackTrace1;
            Key key = _createKey();
            _rwLock.EnterReadLock();
            try
            {
                if (_cachedTraces.TryGetValue(key, out stackTrace))
                {
                    stackTrace1 = stackTrace;
                    return stackTrace1;
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            _rwLock.EnterWriteLock();
            try
            {
                IDictionary<Key, StackTrace> keys = _cachedTraces;
                var stackTrace2 = new StackTrace(true);
                StackTrace stackTrace3 = stackTrace2;
                keys[key] = stackTrace2;
                stackTrace1 = stackTrace3;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }

            return stackTrace1;
        }
#pragma warning disable CS0649
        private readonly ReaderWriterLockSlim _rwLock;

        private readonly IDictionary<Key, StackTrace> _cachedTraces;
#pragma warning restore CS0649
    }
}