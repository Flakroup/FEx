using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using FEx.Legacy.Asyncx.Enums;
using FEx.Legacy.Mvvm.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using FEx.MVVM.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.ViewModels;

public partial class ThreadingAwareViewModel : ViewModelBase, IThreadingAwareViewModel
{
    private readonly ITasksHandler _tasksHandler;
    private bool _isUiUnlocked;

    /// <summary>
    /// Gets or sets a value indicating whether View instance related with this ViewModel is unlocked.
    /// </summary>
    /// <value>
    /// <c>true</c> if related instance of View is unlocked; otherwise, <c>false</c>.
    /// </value>
    public bool IsUiUnlocked
    {
        get => _isUiUnlocked;
        set => SetProperty(ref _isUiUnlocked, value);
    }

    protected SynchronizationContext OriginSynchronizationContext { get; }
    protected IFExDispatcher Dispatcher { get; }

    public ThreadingAwareViewModel(params IAsyncInitializable[] dependencies)
    {
        _tasksHandler = FExServiceProvider.Get<ITasksHandler>();
        _logger = this.GetLogger();
        _initializationSemaphore = new();
        _taskSemaphore = new();
        var instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName;
        _dependencies = new();

        foreach (var dependency in dependencies)
            AddDependency(dependency);

        OriginSynchronizationContext = SynchronizationContextExtensions.Get(true);
        Dispatcher = FExCoreStatics.Dispatcher;

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