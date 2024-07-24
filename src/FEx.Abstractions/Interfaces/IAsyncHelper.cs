using FEx.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface IAsyncHelper
{
    ITaskWrapper FireAndForget(Action action,
                               AsyncMode asyncMode = AsyncMode.Default,
                               CancellationToken cancellationToken = default);

    ITaskWrapper<T> FireAndForget<T>(Func<T> func,
                                     AsyncMode asyncMode = AsyncMode.Default,
                                     CancellationToken cancellationToken = default);

    ITaskWrapper FireTaskAndForget(Func<Task> task, AsyncMode asyncMode = AsyncMode.Default);

    ITaskWrapper<T> FireTaskAndForget<T>(Func<Task<T>> task, AsyncMode asyncMode = AsyncMode.Default);

    IReadOnlyList<ITaskWrapper> FireTasksAndForget(IEnumerable<Func<Task>> tasks,
                                                   AsyncMode asyncMode = AsyncMode.Default);

    IReadOnlyList<ITaskWrapper<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                         AsyncMode asyncMode = AsyncMode.Default);

    Task ExecuteDeferredTaskOnMainThreadAsync(Func<Action> func,
                                              AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart);

    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func,
                                                    AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart);

    Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> func,
                                              AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart);

    Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> func,
                                                    AsyncHelperOptions options = AsyncHelperOptions.ImmediateStart);
}