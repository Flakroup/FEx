using FEx.Abstractions.Interfaces;
using FEx.DI.Abstractions;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using FEx.Legacy.Asyncx.Enums;
using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using FEx.MVVM.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using SynchronizationContextExtensions = FEx.Common.Extensions.SynchronizationContextExtensions;

namespace FEx.Legacy.Mvvm.ViewModels;

public partial class ThreadingAwareViewModel : ViewModelBase, IThreadingAwareViewModel
{
    private readonly ITasksHandler _tasksHandler;
    private bool _isUiUnlocked;

    /// <summary>
    ///     Gets or sets a value indicating whether View instance related with this ViewModel is unlocked.
    /// </summary>
    /// <value>
    ///     <c>true</c> if related instance of View is unlocked; otherwise, <c>false</c>.
    /// </value>
    public bool IsUiUnlocked
    {
        get => _isUiUnlocked;
        set => SetProperty(ref _isUiUnlocked, value);
    }

    protected SynchronizationContext OriginSynchronizationContext { get; }

    public ThreadingAwareViewModel(params IAsyncInitializable[] dependencies)
    {
        _tasksHandler = FExServiceProvider.Get<ITasksHandler>();
        _logger = this.GetLogger();
        _initializationSemaphore = new(1, 1);
        _taskSemaphore = new(1, 1);
        Type instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName;
        _dependencies = new();

        foreach (IAsyncInitializable dependency in dependencies)
            AddDependency(dependency);

        OriginSynchronizationContext = SynchronizationContextExtensions.Get(true);

        IsUiUnlocked = true;
    }

    public async Task RunAsync(Action action, JobSpecs? specs = null, Action pre = null, Action<bool> post = null) =>
        await _tasksHandler.RunAsync(action, specs, s => Prefix(s, pre), (isSuccess, s) => Suffix(s, post, isSuccess));

    public async Task RunTaskAsync(Func<Task> function,
                                   JobSpecs? specs = null,
                                   Action pre = null,
                                   Action<bool> post = null) =>
        await _tasksHandler.RunTaskAsync(function,
            specs,
            s => Prefix(s, pre),
            (isSuccess, s) => Suffix(s, post, isSuccess));

    public async Task<TResult> RunTaskAsync<TResult>(Func<Task<TResult>> function,
                                                     JobSpecs? specs = null,
                                                     Action pre = null,
                                                     Action<bool> post = null) =>
        await _tasksHandler.RunTaskAsync(function,
            specs,
            s => Prefix(s, pre),
            (isSuccess, s) => Suffix(s, post, isSuccess));

    public async Task<TResult> RunFuncAsync<TResult>(Func<TResult> function,
                                                     JobSpecs? specs = null,
                                                     Action pre = null,
                                                     Action<bool> post = null) =>
        await _tasksHandler.RunFuncAsync(function,
            specs,
            s => Prefix(s, pre),
            (isSuccess, s) => Suffix(s, post, isSuccess));

    public virtual void PostMainJob(bool showTimeInfo = true)
    {
    }

    public virtual void PreMainJob()
    {
    }

    private void Suffix(JobSpecs? specs, Action<bool> post, bool isSuccess)
    {
        if (specs.HasFlagFast(JobSpecs.RunPreAndPostMain))
            PostMainJob(specs.HasFlagFast(JobSpecs.ShowTimeInfoAfterMain));

        post?.Invoke(isSuccess);
    }

    private void Prefix(JobSpecs? specs, Action pre)
    {
        pre?.Invoke();

        if (specs.HasFlagFast(JobSpecs.RunPreAndPostMain))
            PreMainJob();
    }
}