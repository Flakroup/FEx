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
    ///     cref="M:Microsoft.VisualStudio.Threading.JoinableTaskFactory.Run``1(System.Func{System.Threading.Tasks.Task{``0}},Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions)" />
    public void Run(Func<Task> asyncMethod,
                    JoinableTaskCreationOptions creationOptions = JoinableTaskCreationOptions.None)
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

    public T Run<T>(Func<Task<T>> asyncMethod,
                    JoinableTaskCreationOptions creationOptions = JoinableTaskCreationOptions.None)
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
    ///     cref="M:Microsoft.VisualStudio.Threading.JoinableTaskFactory.RunAsync``1(System.Func{System.Threading.Tasks.Task{``0}},System.Boolean,System.String,Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions)" />
    public async Task<JoinableTask> RunAsync(Func<Task> asyncMethod,
                                             string parentToken = null,
                                             JoinableTaskCreationOptions creationOptions =
                                                 JoinableTaskCreationOptions.None)
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

    public async Task<JoinableTask<T>> RunAsync<T>(Func<Task<T>> asyncMethod,
                                                   string parentToken = null,
                                                   JoinableTaskCreationOptions creationOptions =
                                                       JoinableTaskCreationOptions.None)
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