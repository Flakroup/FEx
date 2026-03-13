using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IRunAsync
{
    Task RunAsync(Action action, JobSpecs? specs = null, Action pre = null, Action<bool> post = null);

    Task<TResult> RunFuncAsync<TResult>(Func<TResult> function,
                                        JobSpecs? specs = null,
                                        Action pre = null,
                                        Action<bool> post = null);

    Task RunTaskAsync(Func<Task> function, JobSpecs? specs = null, Action pre = null, Action<bool> post = null);

    Task<TResult> RunTaskAsync<TResult>(Func<Task<TResult>> function,
                                        JobSpecs? specs = null,
                                        Action pre = null,
                                        Action<bool> post = null);
}