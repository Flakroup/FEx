using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IRunAsync
{
    Task RunAsync(Action action, JobSpecs? specs, Action? pre, Action<bool>? post);

    Task<TResult> RunFuncAsync<TResult>(Func<TResult> function, JobSpecs? specs, Action? pre, Action<bool>? post);

    Task RunTaskAsync(Func<Task> function, JobSpecs? specs, Action? pre, Action<bool>? post);

    Task<TResult> RunTaskAsync<TResult>(Func<Task<TResult>> function, JobSpecs? specs, Action? pre, Action<bool>? post);
}