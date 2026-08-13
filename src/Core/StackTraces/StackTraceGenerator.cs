using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace FEx.Core.StackTraces;

public class StackTraceGenerator : IStackTraceProvider
{
    private readonly IStackTraceFilter[] _stackTraceFilters;

    // Assigned during construction via CheatClrAndUseDynamicMethodsToGetStackTraceFast or the Fallback path;
    // one of them always runs before the ctor returns.
    private StackTraceCache _stackTraceCache = null!;

    public bool DotNotFixDynamicProxyStackTrace { get; set; }

    public StackTraceGenerator(params IStackTraceFilter[] stackTraceFilters)
    {
        _stackTraceFilters = stackTraceFilters ?? [];

        try
        {
            CheatClrAndUseDynamicMethodsToGetStackTraceFast();

            if (_stackTraceCache is not null)
                _stackTraceCache.GetStackTrace();
            else
                Fallback("System.Diagnostics.StackFrameHelper could not be found");
        }
        catch (Exception exception1)
        {
            var exception = exception1;

            var str = exception.ToString();

            Fallback(str);
        }
    }

    public StackTrace GetStackTrace() => _stackTraceCache.GetStackTrace();

    public StackTraceInfo? GetStackTraceInfo(string? logger = null, string? message = null)
    {
        if (!RequiresStackTrace(logger, message))
            return null;

        var stackTraceFrames = new List<StackTraceFrame>();
        var frames = GetStackTrace().GetFrames();

        if (frames is null)
            return null;

        var stackFrameArray = frames;

        for (var i = 0; i < stackFrameArray.Length; i++)
        {
            var stackFrame = stackFrameArray[i];
            var method = stackFrame.GetMethod();
            var declaringType = method?.DeclaringType;

            if (declaringType is not null)
                stackTraceFrames.Add(new()
                {
                    Column = stackFrame.GetFileColumnNumber(),
                    Line = stackFrame.GetFileLineNumber(),
                    FullFilename = stackFrame.GetFileName(),
                    Method = method?.Name,
                    Type = declaringType.FullName,
                    Namespace = declaringType.Namespace ?? ""
                });
        }

        return new()
        {
            Frames = [.. stackTraceFrames]
        };
    }

    private void Fallback(string str)
    {
        Trace.WriteLine(string.Concat(
            "Could not create fast stack trace cache, falling back to old supported way, failure because: ",
            str));

        SlowAndSafeApproachToGetStackTrace();
    }

    private void CheatClrAndUseDynamicMethodsToGetStackTraceFast()
    {
        var stackTraceType = typeof(StackTrace);
        var stackTraceAssembly = stackTraceType.Assembly;
        var type = stackTraceAssembly.GetType("System.Diagnostics.StackFrameHelper");

        if (type is null)
            return;

        var field = type.GetField("rgMethodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
        var fieldInfo = type.GetField("rgiILOffset", BindingFlags.Instance | BindingFlags.NonPublic);

        var method = stackTraceType.GetMethod("GetStackFramesInternal", BindingFlags.Static | BindingFlags.NonPublic);

        var createMethod = typeof(MethodHandleAndILOffset).GetMethod(nameof(MethodHandleAndILOffset.Create),
            [typeof(nint[]), typeof(int[])]);

        // If any required reflection member is missing on the current runtime, bail out so the ctor
        // falls back to the slow-and-safe path (leaving _stackTraceCache unset here).
        if (field is null || fieldInfo is null || method is null || createMethod is null)
            return;

        var dynamicMethod = new DynamicMethod("GetStackTraceFast",
            typeof(MethodHandleAndILOffset[]),
            Type.EmptyTypes,
            type,
            true);

        var constructors = type.GetConstructors()[0];
        var length = constructors.GetParameters().Length == 2;
        var lGenerator = dynamicMethod.GetILGenerator();
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
        lGenerator.Emit(OpCodes.Call, createMethod);
        lGenerator.Emit(OpCodes.Ret);

        var getMethodRuntimeHandle =
            (GetMethodRuntimeHandles)dynamicMethod.CreateDelegate(typeof(GetMethodRuntimeHandles));

        _stackTraceCache = new(() => new(getMethodRuntimeHandle()));
    }

    private bool RequiresStackTrace(string? logger, string? message)
    {
        var stackTraceFilterArray = _stackTraceFilters;

        if (stackTraceFilterArray.IsNullOrEmptyList())
            return true;

        for (var i = 0; i < stackTraceFilterArray.Length; i++)
        {
            if (stackTraceFilterArray[i].Applies(logger, message))
                return true;
        }

        return false;
    }

    private void SlowAndSafeApproachToGetStackTrace() =>
        _stackTraceCache = new(() =>
        {
            var frames = new StackTrace(false).GetFrames();

            if (frames is null)
                return new([]);

            var methodHandleAndIlOffset = new MethodHandleAndILOffset[frames.Length];

            for (var i = 0; i < methodHandleAndIlOffset.Length; i++)
            {
                // A real stack frame's method is expected; fall back to a zero handle if the runtime omits it.
                var frameMethod = frames[i].GetMethod();
                methodHandleAndIlOffset[i] =
                    new(frameMethod?.MethodHandle.Value ?? IntPtr.Zero, frames[i].GetILOffset());
            }

            return new(methodHandleAndIlOffset);
        });

    private delegate Key GetKey();

    private delegate MethodHandleAndILOffset[] GetMethodRuntimeHandles();

    private class Key
    {
        private readonly MethodHandleAndILOffset[] _items;

        public Key(MethodHandleAndILOffset[] items)
        {
            _items = items;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Key key)
                return false;

            return this == obj || Equals(key);
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
        private readonly IntPtr _methodHandle;

        private readonly int _offset;

        public MethodHandleAndILOffset(IntPtr methodHandle, int offset)
        {
            _methodHandle = methodHandle;
            _offset = offset;
        }

        // ReSharper disable UnusedMember.Local
        public static MethodHandleAndILOffset[] Create(IntPtr[] methods, int[] offsets)
        // ReSharper restore UnusedMember.Local
        {
            var methodHandleAndILOffset = new MethodHandleAndILOffset[methods.Length];

            for (var i = 0; i < methods.Length; i++)
                methodHandleAndILOffset[i] = new(methods[i], offsets[i]);

            return methodHandleAndILOffset;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MethodHandleAndILOffset methodHandleAndILOffset)
                return false;

            return this == obj || Equals(methodHandleAndILOffset);
        }

        public override int GetHashCode()
        {
            var intPtr = _methodHandle;

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

    private class StackTraceCache
    {
        private readonly GetKey _createKey;

        public StackTraceCache(GetKey createKey)
        {
            _createKey = createKey;
            _rwLock = new();
            _cachedTraces = new();
        }

        public StackTrace GetStackTrace()
        {
            StackTrace? stackTrace;
            StackTrace stackTrace1;
            var key = _createKey();
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
                var stackTrace3 = stackTrace2;
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
#pragma warning disable IDISP006
        private readonly ReaderWriterLockSlim _rwLock;
#pragma warning restore IDISP006

        private readonly ConcurrentDictionary<Key, StackTrace> _cachedTraces;
#pragma warning restore CS0649
    }
}