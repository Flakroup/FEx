using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.Extensions;

public static class RunAsyncExtensions
{
    public static Task RunAsync(this IRunAsync runner, Action action) =>
        runner.RunAsync(action, null, null, null);

    public static Task<TResult> RunFuncAsync<TResult>(this IRunAsync runner, Func<TResult> function) =>
        runner.RunFuncAsync(function, null, null, null);

    public static Task RunTaskAsync(this IRunAsync runner, Func<Task> function) =>
        runner.RunTaskAsync(function, null, null, null);

    public static Task<TResult> RunTaskAsync<TResult>(this IRunAsync runner, Func<Task<TResult>> function) =>
        runner.RunTaskAsync(function, null, null, null);
}
