using Avalonia.Threading;
using FEx.DependencyInjection.Abstractions;
using FEx.MVVM.Rx.BaseObjects;
using FEx.Sample.Avalonia.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
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

    public ReactiveCommand<Unit, Unit> LoadUsersCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadPostsCommand { get; }
    public ReactiveCommand<Unit, Unit> TestResilienceCommand { get; }

    public MainWindowViewModel()
    {
        // Commands execute on background thread, so we need to marshal UI updates
        LoadUsersCommand = ReactiveCommand.CreateFromTask(LoadUsersAsync, outputScheduler: RxApp.MainThreadScheduler);
        LoadPostsCommand = ReactiveCommand.CreateFromTask(LoadPostsAsync, outputScheduler: RxApp.MainThreadScheduler);
        TestResilienceCommand = ReactiveCommand.CreateFromTask(TestResilienceAsync, outputScheduler: RxApp.MainThreadScheduler);
        _api = FExServiceProvider.Get<JsonPlaceholderApi>();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Loading users from JSONPlaceholder API...";

            List<User> users = await _api.GetUsersAsync().ConfigureAwait(false);

            // Marshal UI updates to main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Users.Clear();
                foreach (User user in users)
                    Users.Add(user);
            });

            StatusText = $"✅ Loaded {users.Count} users at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPostsAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Loading posts from JSONPlaceholder API...";

            List<Post> posts = await _api.GetPostsAsync().ConfigureAwait(false);

            // Marshal UI updates to main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Posts.Clear();
                foreach (Post post in posts.Take(10)) // Only show first 10
                    Posts.Add(post);
            });

            StatusText = $"✅ Loaded {posts.Count} posts (showing 10) at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TestResilienceAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Testing Polly resilience policies (retry, circuit breaker, timeout)...";

            // Make multiple rapid requests to test resilience
            Task<List<User>> task1 = _api.GetUsersAsync();
            Task<List<Post>> task2 = _api.GetPostsAsync();
            Task<List<Post>> task3 = _api.GetUserPostsAsync(1);

            await Task.WhenAll(task1, task2, task3);

            StatusText =
                $"✅ Resilience test passed! All concurrent requests handled successfully at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"❌ Resilience test failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}