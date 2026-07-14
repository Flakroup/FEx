using FEx.Agnostics.Abstractions.Enums;
using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Legacy.Asyncx.Abstractions.Interfaces;

public interface ITasksHandler
{
    Task RunAsync(Action task,
                  JobSpecs? specs,
                  Action<JobSpecs?>? pre,
                  Action<bool, JobSpecs?>? post,
                  AsyncMode asyncMode,
                  CancellationToken cancellationToken);

    Task<T> RunTaskAsync<T>(Func<Task<T>> task,
                            JobSpecs? specs,
                            Action<JobSpecs?>? pre,
                            Action<bool, JobSpecs?>? post,
                            AsyncMode asyncMode);

    Task RunTaskAsync(Func<Task> task,
                      JobSpecs? specs,
                      Action<JobSpecs?>? pre,
                      Action<bool, JobSpecs?>? post,
                      AsyncMode asyncMode);

    Task<T> RunFuncAsync<T>(Func<T> task,
                            JobSpecs? specs,
                            Action<JobSpecs?>? pre,
                            Action<bool, JobSpecs?>? post,
                            AsyncMode asyncMode,
                            CancellationToken cancellationToken);
}