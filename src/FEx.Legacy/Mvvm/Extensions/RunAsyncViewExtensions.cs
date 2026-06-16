using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.Extensions;

public static class RunAsyncViewExtensions
{
    public static Task RunAsync(this IRunAsyncView runner, Action action) => runner.RunAsync(action, null, null);

    public static Task<TResult> RunFuncAsync<TResult>(this IRunAsyncView runner, Func<TResult> function) =>
        runner.RunFuncAsync(function, null, null);

    public static Task RunTaskAsync(this IRunAsyncView runner, Func<Task> function) =>
        runner.RunTaskAsync(function, null, null);

    public static Task<TResult> RunTaskAsync<TResult>(this IRunAsyncView runner, Func<Task<TResult>> function) =>
        runner.RunTaskAsync(function, null, null);
}