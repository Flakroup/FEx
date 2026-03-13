using FEx.Agnostics.Abstractions.Enums;
using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Legacy.Asyncx.Abstractions.Interfaces;

public interface ITasksHandler
{
    Task RunAsync(Action task,
                  JobSpecs? specs = null,
                  Action<JobSpecs?> pre = null,
                  Action<bool, JobSpecs?> post = null,
                  AsyncMode asyncMode = AsyncMode.ThreadPool,
                  CancellationToken cancellationToken = default);

    Task<T> RunTaskAsync<T>(Func<Task<T>> task,
                            JobSpecs? specs = null,
                            Action<JobSpecs?> pre = null,
                            Action<bool, JobSpecs?> post = null,
                            AsyncMode asyncMode = AsyncMode.ThreadPool);

    Task RunTaskAsync(Func<Task> task,
                      JobSpecs? specs = null,
                      Action<JobSpecs?> pre = null,
                      Action<bool, JobSpecs?> post = null,
                      AsyncMode asyncMode = AsyncMode.ThreadPool);

    Task<T> RunFuncAsync<T>(Func<T> task,
                            JobSpecs? specs = null,
                            Action<JobSpecs?> pre = null,
                            Action<bool, JobSpecs?> post = null,
                            AsyncMode asyncMode = AsyncMode.ThreadPool,
                            CancellationToken cancellationToken = default);
}