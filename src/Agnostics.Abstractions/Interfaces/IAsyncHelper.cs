using FEx.Agnostics.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IAsyncHelper
{
    ITaskWrapper FireAndForget(Action action,
                               AsyncMode asyncMode = AsyncMode.Default,
                               IExceptionHandlerOptions? options = null,
                               CancellationToken cancellationToken = default);

    ITaskWrapper<T> FireAndForget<T>(Func<T> func,
                                     AsyncMode asyncMode = AsyncMode.Default,
                                     IExceptionHandlerOptions? options = null,
                                     CancellationToken cancellationToken = default);

    ITaskWrapper FireTaskAndForget(Func<Task> task,
                                   AsyncMode asyncMode = AsyncMode.Default,
                                   IExceptionHandlerOptions? options = null);

    ITaskWrapper<T> FireTaskAndForget<T>(Func<Task<T>> task,
                                         AsyncMode asyncMode = AsyncMode.Default,
                                         IExceptionHandlerOptions? options = null);

    IReadOnlyList<ITaskWrapper> FireTasksAndForget(IEnumerable<Func<Task>> tasks,
                                                   AsyncMode asyncMode = AsyncMode.Default,
                                                   IExceptionHandlerOptions? options = null);

    IReadOnlyList<ITaskWrapper<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                         AsyncMode asyncMode = AsyncMode.Default,
                                                         IExceptionHandlerOptions? options = null);

    Task ExecuteDeferredTaskOnMainThreadAsync(Action func, AsyncOptions options = AsyncOptions.ImmediateStart);

    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func, AsyncOptions options = AsyncOptions.ImmediateStart);

    Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> func, AsyncOptions options = AsyncOptions.ImmediateStart);

    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> func,
                                                    AsyncOptions options = AsyncOptions.ImmediateStart);
}