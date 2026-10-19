using Microsoft.VisualStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Helpers;

public class JoinableTaskFactoryHandler
{
    private readonly JoinableTaskFactory _factory;
    private readonly SemaphoreSlim _semaphore;
    public int ThreadId { get; }

    public JoinableTaskFactoryHandler(int threadId, JoinableTaskFactory jtf)
    {
        ThreadId = threadId;
        _factory = jtf;
        _semaphore = new(1, 1);
    }

    /// <inheritdoc
    ///     cref="Microsoft.VisualStudio.Threading.JoinableTaskFactory.Run{T}(System.Func{System.Threading.Tasks.Task{T}},Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions)" />
    public void Run(Func<Task> asyncMethod) => Run(asyncMethod, JoinableTaskCreationOptions.None);

    public void Run(Func<Task> asyncMethod, JoinableTaskCreationOptions creationOptions)
    {
        if (!_semaphore.Wait(TimeSpan.Zero))
            throw new InvalidOperationException("This operation will lead to deadlock");

        try
        {
            _factory.Run(asyncMethod, creationOptions);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public T Run<T>(Func<Task<T>> asyncMethod) => Run(asyncMethod, JoinableTaskCreationOptions.None);

    public T Run<T>(Func<Task<T>> asyncMethod, JoinableTaskCreationOptions creationOptions)
    {
        if (!_semaphore.Wait(TimeSpan.Zero))
            throw new InvalidOperationException("This operation will lead to deadlock");

        try
        {
            return _factory.Run(asyncMethod, creationOptions);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc
    ///     cref="Microsoft.VisualStudio.Threading.JoinableTaskFactory.RunAsync{T}(System.Func{System.Threading.Tasks.Task{T}},Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions)" />
    public Task<JoinableTask> RunAsync(Func<Task> asyncMethod) =>
        RunAsync(asyncMethod, null, JoinableTaskCreationOptions.None);

    public async Task<JoinableTask> RunAsync(Func<Task> asyncMethod,
                                             string? parentToken,
                                             JoinableTaskCreationOptions creationOptions)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero))
            throw new InvalidOperationException("This operation will lead to deadlock");

        try
        {
            return _factory.RunAsync(asyncMethod, parentToken, creationOptions);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<JoinableTask<T>> RunAsync<T>(Func<Task<T>> asyncMethod) =>
        RunAsync(asyncMethod, null, JoinableTaskCreationOptions.None);

    public async Task<JoinableTask<T>> RunAsync<T>(Func<Task<T>> asyncMethod,
                                                   string? parentToken,
                                                   JoinableTaskCreationOptions creationOptions)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero))
            throw new InvalidOperationException("This operation will lead to deadlock");

        try
        {
            return _factory.RunAsync(asyncMethod, parentToken, creationOptions);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}