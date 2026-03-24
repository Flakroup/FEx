using Avalonia.Threading;
using FEx.DependencyInjection.Abstractions;
using FEx.MVVM.Rx.BaseObjects;
using FEx.Sample.Avalonia.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace FEx.Sample.Avalonia;

/// <summary>
/// ViewModel for MainWindow demonstrating FEx.Flurlx API integration.
/// </summary>
public class MainWindowViewModel : ReactiveNotifyPropertyChanged
{
    private readonly JsonPlaceholderApi _api;
    private string _statusText = "Ready to make API calls...";
    private bool _isBusy;

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ObservableCollection<User> Users { get; } = new();
    public ObservableCollection<Post> Posts { get; } = new();

#pragma warning disable IDISP006 // ReactiveUI commands, disposed by ViewModel lifecycle
    public ReactiveCommand<Unit, Unit> LoadUsersCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPostsCommand { get; }
    public ReactiveCommand<Unit, Unit> TestResilienceCommand { get; }
#pragma warning restore IDISP006

    public MainWindowViewModel()
    {
        // Commands execute on background thread, so we need to marshal UI updates
        LoadUsersCommand = ReactiveCommand.CreateFromTask(LoadUsersAsync, outputScheduler: RxApp.MainThreadScheduler);
        LoadPostsCommand = ReactiveCommand.CreateFromTask(LoadPostsAsync, outputScheduler: RxApp.MainThreadScheduler);

        TestResilienceCommand =
            ReactiveCommand.CreateFromTask(TestResilienceAsync, outputScheduler: RxApp.MainThreadScheduler);

        _api = FExServiceProvider.Get<JsonPlaceholderApi>();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                StatusText = "Loading users from JSONPlaceholder API...";
            });

            var users = await _api.GetUsersAsync().ConfigureAwait(false);

            // Marshal UI updates to main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Users.Clear();

                foreach (var user in users)
                    Users.Add(user);

                StatusText = $"✅ Loaded {users.Count} users at {DateTime.Now:HH:mm:ss}";
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"❌ Error: {ex.Message}";
                IsBusy = false;
            });
        }
    }

    private async Task LoadPostsAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                StatusText = "Loading posts from JSONPlaceholder API...";
            });

            var posts = await _api.GetPostsAsync().ConfigureAwait(false);

            // Marshal UI updates to main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Posts.Clear();

                foreach (var post in posts.Take(10)) // Only show first 10
                    Posts.Add(post);

                StatusText = $"✅ Loaded {posts.Count} posts (showing 10) at {DateTime.Now:HH:mm:ss}";
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"❌ Error: {ex.Message}";
                IsBusy = false;
            });
        }
    }

    private async Task TestResilienceAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                StatusText = "Testing Polly resilience policies (retry, circuit breaker, timeout)...";
            });

            // Make multiple rapid requests to test resilience
            var task1 = _api.GetUsersAsync();
            var task2 = _api.GetPostsAsync();
            var task3 = _api.GetUserPostsAsync(1);

            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText =
                    $"✅ Resilience test passed! All concurrent requests handled successfully at {DateTime.Now:HH:mm:ss}";
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"❌ Resilience test failed: {ex.Message}";
                IsBusy = false;
            });
        }
    }
}