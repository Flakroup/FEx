# FEx Sample - Avalonia + Flurlx Demo

Cross-platform desktop application demonstrating **FEx framework** integration with **Avalonia UI** and **Flurlx HTTP client** with **Polly resilience**.

## 🎯 What This Demo Shows

✅ **Avalonia UI** - Cross-platform MVVM desktop framework  
✅ **FEx.Flurlx** - HTTP client with Polly resilience policies (Retry, Circuit Breaker, Timeout, Bulkhead)  
✅ **ReactiveUI** - Reactive MVVM pattern with commands  
✅ **StrongInject DI** - Compile-time dependency injection  
✅ **Proper Thread Marshalling** - UI updates on main thread using `Dispatcher.UIThread`  
✅ **JSONPlaceholder API** - Real REST API integration

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         FEx.Sample.Avalonia             │
│  (Avalonia UI + ReactiveUI MVVM)        │
└────────────────┬────────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
┌───────▼──────┐  ┌──────▼────────────┐
│ FEx.Avaloniax│  │ FEx.Flurlx        │
│ (UI Services)│  │ (HTTP + Polly)    │
└──────────────┘  └───────────────────┘
                         │
              ┌──────────┴──────────┐
              │                     │
       ┌──────▼─────┐      ┌───────▼────────┐
       │ Flurl.Http │      │ Polly Policies │
       └────────────┘      └────────────────┘
```

## 🚀 Features Demonstrated

### 1. **Flurlx with Polly Resilience**
- **Automatic Retry** - Exponential backoff for transient failures
- **Circuit Breaker** - Prevents cascading failures (5 failures = circuit opens)
- **Timeout Protection** - 10-second request timeout
- **Bulkhead** - Limits concurrent requests (max 10)

### 2. **Proper Async/Thread Handling**
```csharp
// API call on background thread
List<User> users = await _api.GetUsersAsync().ConfigureAwait(false);

// UI update marshalled to main thread
await Dispatcher.UIThread.InvokeAsync(() =>
{
    Users.Clear();
    foreach (User user in users)
        Users.Add(user);
});
```

### 3. **ReactiveUI Commands**
```csharp
LoadUsersCommand = ReactiveCommand.CreateFromTask(
    LoadUsersAsync, 
    outputScheduler: RxSchedulers.MainThreadScheduler
);
```

### 4. **StrongInject Container**
```csharp
[RegisterModule(typeof(FExModule))]
[RegisterModule(typeof(FExFlurlxModule))]
[Register(typeof(JsonPlaceholderApiConfiguration), Scope.SingleInstance, typeof(IApiConfiguration))]
[Register(typeof(JsonPlaceholderApi), Scope.SingleInstance, typeof(JsonPlaceholderApi))]
public sealed partial class AppContainer : FExModule, IFExContainer, IFExFlurlxContainer
{
    [Instance]
    public static IAsyncConfigurator[] AsyncConfigurators { get; } = [];
}
```

## 📋 Project Structure

```
FEx.Sample.Avalonia/
├── App.axaml(.cs)                    # Application entry + FEx initialization
├── AppContainer.cs                    # StrongInject DI container
├── MainWindow.axaml(.cs)              # Main window XAML + code-behind
├── MainWindowViewModel.cs             # ViewModel with ReactiveUI commands
├── Configuration/
│   └── JsonPlaceholderApiConfiguration.cs  # Flurlx + Polly config
└── Services/
    └── JsonPlaceholderApi.cs          # API client (extends FlurlApiBase)
```

## 🔧 Key Implementation Details

### API Client with Flurlx
```csharp
public class JsonPlaceholderApi : FlurlApiBase
{
    public JsonPlaceholderApi(IFlurlConfigurator flurlConfigurator)
        : base(flurlConfigurator)
    {
    }

    public async Task<List<User>> GetUsersAsync() =>
        await GetResponseAsync<List<User>, object>("/users", method: RequestMethod.GET);
}
```

### Polly Configuration
```csharp
public PollyPolicyConfiguration PollyConfig => new()
{
    MaxRetryAttempts = 3,
    InitialRetryDelay = TimeSpan.FromSeconds(1),
    RequestTimeout = TimeSpan.FromSeconds(10),
    CircuitBreakerFailureThreshold = 5,
    CircuitBreakerDuration = TimeSpan.FromSeconds(15),
    MaxParallelization = 10
};
```

## 🎮 How to Run

```bash
# Build
dotnet build

# Run
dotnet run
```

## 🧪 Testing Features

**Load Users** - Fetches 10 users from JSONPlaceholder API  
**Load Posts** - Fetches first 10 posts from JSONPlaceholder API  
**Test Resilience** - Makes 3 concurrent requests to test Polly policies

## 📚 What You'll Learn

1. ✅ How to integrate **Avalonia** with **FEx framework**
2. ✅ How to use **Flurlx** for resilient HTTP calls
3. ✅ How to configure **Polly policies** (Retry, Circuit Breaker, Timeout, Bulkhead)
4. ✅ How to properly **marshal UI updates** from background threads
5. ✅ How to use **ReactiveUI** with FEx framework
6. ✅ How to set up **StrongInject DI** container
7. ✅ How to build **cross-platform desktop apps** with .NET 9

## 🔗 Related FEx Modules

- **FEx.Avaloniax** - Avalonia UI integration
- **FEx.Flurlx** - HTTP client with Polly resilience
- **FEx.MVVM.Rx** - ReactiveUI base classes
- **FEx.DependencyInjection** - Multi-DI support

## 📖 Further Reading

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Flurl Documentation](https://flurl.dev/)
- [Polly Documentation](https://www.thepollyproject.org/)
- [ReactiveUI Documentation](https://www.reactiveui.net/)

---

**Built with:** .NET 10.0 | Avalonia 12.0 | Flurl.Http 4.x | Polly 8.x | ReactiveUI 23.x




