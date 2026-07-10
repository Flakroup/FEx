using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IRunAsyncView
{
    Task RunAsync(Action action, object? sender, JobSpecs? specs);
    Task<TResult> RunFuncAsync<TResult>(Func<TResult> function, object? sender, JobSpecs? specs);
    Task RunTaskAsync(Func<Task> function, object? sender, JobSpecs? specs);
    Task<TResult> RunTaskAsync<TResult>(Func<Task<TResult>> function, object? sender, JobSpecs? specs);
}