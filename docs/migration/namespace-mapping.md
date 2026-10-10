# Namespace and type mapping from older FlakEssentials code

Code written against the older FlakEssentials generation of FEx uses namespaces such as `FEx.Basics`, `FEx.Extensions`, `FEx.Abstractions`, `FEx.Toolbox` and `FEx.Fundamentals`. In the current FEx these were split across the Agnostics, Core, Common and MVVM layers (see [architecture](../architecture.md#layers)).

Rules of thumb:

- **Map by type, not by namespace.** One old namespace can fan out to several new ones. `FEx.Extensions`, for example, now lives in two places: `FEx.Agnostics.Abstractions.Extensions` (most extension methods) and `FEx.Core.Abstractions.Extensions` (`HandleException`).
- `FEx.Common.Extensions` maps to `FEx.Agnostics.Abstractions.Extensions`, not to `FEx.Common.Abstractions`, which is a different assembly.
- The public API of any current type can be looked up with the API surface server described in [ADR 0001](../decisions/0001-api-surface-served-by-external-mcp-server.md).

## Namespaces

| Older namespace | Current namespace | Types and notes |
|---|---|---|
| `FEx.MVVM`, `FEx.MVVM.BaseObjects`, `FEx.Toolbox.BaseObjects` (for `NotifyPropertyChanged`, `PropertyChangeAware`) | `FEx.Agnostics.BaseObjects` | Project `FEx.Agnostics`. `FExMvvm` itself stays in `FEx.MVVM` |
| `FEx.Abstractions.Interfaces` | `FEx.Agnostics.Abstractions.Interfaces` | `ISynchronizedAccessService`, `IFExLogger`, `IFExNotifyPropertyChanged`, `IAppInfoProvider` |
| `FEx.Abstractions` | `FEx.Core.Abstractions` or `FEx.Asyncx.Abstractions` | `AsyncInitializable` moved to `FEx.Asyncx.Abstractions` |
| `FEx.Basics` | `FEx.Core.Abstractions` | The statics class `FExBasics` is now `FExCoreStatics` |
| `FEx.Basics.Collections.Concurrent` | `FEx.Core.Collections.Concurrent` | `ConcurrentObservableList<T>`; the non-observable `ConcurrentList<T>` is in `FEx.Agnostics.Collections.Concurrent` |
| `FEx.Basics.Extensions` | `FEx.Agnostics.Abstractions.Extensions` | `FirstIsOtherThanSecond`, `GetEnumValueDescription`, `IsIn` |
| `FEx.Common.Extensions`, `FEx.Extensions` | `FEx.Agnostics.Abstractions.Extensions` | `HandleException` is the exception: `FEx.Core.Abstractions.Extensions` |
| `FEx.Extensions.Collections.Lists` | `FEx.Agnostics.Abstractions.Extensions.Collections.Lists` | `IsNullOrEmptyList` |
| `FEx.Extensions.Collections.Dictionaries` | `FEx.Agnostics.Abstractions.Extensions` | `ReplaceAndDisposeOldValue`. Read-only dictionary helpers are in `...Extensions.Collections.Dictionaries` |
| `FEx.Extensions.Numericals` | `FEx.Agnostics.Abstractions.Extensions.Numericals` | |
| `FEx.Extensions.IO` | `FEx.Agnostics.Abstractions.IO` | |
| `FEx.Extensions.Web` | `FEx.Agnostics.Abstractions.Extensions.Web` | |
| `FEx.Extensions.Helpers` | `FEx.Agnostics.Abstractions` | `AsyncStatics` |
| `FEx.Extensions.Base.Models` | `FEx.Agnostics.Abstractions.Models` | `WebRequestParams` |
| `FEx.Extensions.Base.Helpers` | `FEx.Core.Abstractions.Helpers` | `ReflectionHelper` (`SetPropertyValue`, `AsDictionary`, `ReadEmbeddedFile`) |
| `FEx.Common.Utilities` | `FEx.Core.Abstractions.Utilities` | `AppUtility` |
| `FEx.Common.Helpers` | `FEx.Core.Abstractions.Helpers` | `ReflectionHelper` |
| `FEx.Utilities.Collections` | `FEx.Agnostics.Abstractions.Collections` | `Map<,>` |
| `FEx.Fundamentals`, `FEx.Fundamentals.Helpers` | `FEx.Core`, `FEx.Core.Helpers` | |
| `FEx.Rx` | `FEx.Core.Abstractions.Subjects` | `FExSubject<T>` |
| `FEx.DI.Abstractions` (and `.Interfaces`) | `FEx.DependencyInjection.Abstractions` (and `.Interfaces`) | `DI` became `DependencyInjection` |
| `FEx.Json` (for `FromJson`, `ToJson`) | `FEx.Json.Extensions` | The extension methods are in the `Extensions` sub-namespace of project `FEx.Json` |
| `FlakEssentials.Json.Converters` | `FEx.Json.Converters` | See the converter split below |
| `FlakEssentials.AppSettings` | `FEx.AppSettings` and `FEx.AppSettings.Abstractions` | `BaseUserSettings` is in `FEx.AppSettings.Abstractions` |
| `FlakEssentials.AsyncEx` | `FEx.Asyncx` | |
| `FlakEssentials.Flurl` | `FEx.Flurlx` | `RequestMethod` in `FEx.Flurlx.Models`, `FlurlApiBase` in `FEx.Flurlx.Services` |
| `FlakEssentials.CommonUtilities.Services` | `FEx.Core` | Dependency injection is now `FEx.DependencyInjection(.Abstractions)` |
| `FlakEssentials.WpfEx` | `FEx.WPFx` | `IconAttribute` in `FEx.WPFx.Attributes`, `IconExtensions` in `FEx.WPFx.Extensions`, `IconDisplayer` and the window and control base classes in `FEx.WPFx.Controls` |

## Type renames and moves

| Older | Current | Namespace |
|---|---|---|
| `FExBasics`, `FExFoundation` (statics) | `FExCoreStatics` | `FEx.Core.Abstractions` |
| `FExBasics.SynchronizedAccessService` | `FExCoreStatics.SynchronizedAccessService` | `FEx.Core.Abstractions` |
| `Application.Name` | `FExCoreStatics.AppInfoProvider.Name` | `FEx.Core.Abstractions` |
| `ILockService` | `ISynchronizedAccessService` | `FEx.Agnostics.Abstractions.Interfaces` |
| `ObservableConcurrentCollection<T>` | `ConcurrentObservableList<T>` | `FEx.Core.Collections.Concurrent` |
| `ConcurrentCollection<T>` | `ConcurrentList<T>` | `FEx.Agnostics.Collections.Concurrent` |
| `ObservableHashSet<T>` | `ObservableHashSet<T>` | `FEx.Core.Abstractions.Collections` |
| `StaticAsyncHelper` | `AsyncStatics` | `FEx.Agnostics.Abstractions` |
| `RunWithWhenAllTasksAsync`, `RunWithWhenAllAsync` | `WithWhenAllTasksAsync`, `WithWhenAllAsync` | `FEx.Agnostics.Abstractions.Extensions` |
| `DateTimeFromUnixUtcTimestampSeconds` | `DateTimeFromUnixTimestampSeconds` | `FEx.Agnostics.Abstractions.Extensions` |
| `ServiceProviderHelper.Instance` | `FExServiceProvider`, plus classes derived from `StaticsBase` (`...Abstractions.Basics`) | `FEx.DependencyInjection.Abstractions` |
| `WebModule` | `FExWebx` (`WebModule.SetDefaultConnectionLimit(n)` became the property `FExWebx.DefaultConnectionLimit = n`) | `FEx.Webx` |
| `Cmd`, `ICmd` | unchanged names | `FEx.Agnostics.Utilities` |
| `CompressionHelper` | unchanged name | `FEx.FileSystem.Helpers` |
| `DeepCopyByExpressionTree` and the other deep-copy helpers | unchanged names | `FEx.Agnostics.Helpers` |
| `LinkableNotifyPropertyChanged`, `ILinkableNotifyPropertyChanged` | unchanged names | `FEx.MVVM.Abstractions` (`.Interfaces` for the interface) |
| `IMessagePopupService` | unchanged name; the convenience overloads are in `MessagePopupServiceExtensions` | `FEx.MVVM.Abstractions.Interfaces`, `FEx.MVVM.Abstractions.Extensions` |
| `FlakWindow`, `FlakUserControl`, `FlakShellWindow` | `FExWindow<TViewModel>`, `FExUserControl`, `FExShellWindow<T>` | `FEx.WPFx.Controls` |
| `SecureNotifyPropertyChanged` | unchanged name | `FEx.Encryption` |
| `FlurlClientService` | `FlurlApiBase` (a different model, based on Flurl 4 and Polly) | `FEx.Flurlx.Services` |
| `Loggable`, `ILoggable` | removed. Use an `IFExLogger` field set to `FExStaticLogger.Instance` | `FEx.Agnostics.Abstractions.Interfaces`, `FEx.Agnostics.Abstractions.Logging` |

## Changed signatures and behavior

| Member | Change | Migration |
|---|---|---|
| `ParseStringConverter` | Split into `ParseStringToDoubleConverter` and `ParseStringToLongConverter` (each with a `Singleton`), in `FEx.Json.Converters` | Pick the converter by the type of the field. In a global converter list, add both |
| `IFExLogger` | Methods are `Debug`, `Information`, `Error` and so on, not `LogDebug` or `LogError`. The overloads that take an exception put the `Exception` first: `Error(Exception exception, string? message)` | Rename the calls and reorder the arguments |
| `ISynchronizedAccessService.WaitAsync` | Signature is `WaitAsync(string key, int maxParallel = 1, CancellationToken cancellationToken = default)` | Pass the token as a named argument |
| `BaseUserSettings` | `LoadSettings(path)` became `GetSettings(path)` | Rename |
| `InitializeModule<TContainer>` | Became `InitializeModule<TContainer, TEngineContext>`; for Microsoft DI, `TEngineContext` is `IServiceCollection` and the registration method is `RegisterServices` | Register against `IInitializeModule<IServiceCollection>`. See [architecture](../architecture.md#module-system) |
| `AsyncInitializable` | `OnInitializationAsync(bool reInitialize)` returning `Task<bool>` became `OnInitializeAsync()` returning `Task`. `RunInitialize()` became `BeginInitialization()`. The public `InitializationTask` is gone: use `InitializeAsync()` and the flags `IsInitialized`, `IsInitializing`, `HasFinishedInitialization`. The base class takes its logger from `FExStaticLogger.Instance` | Update overrides and call sites |
| `IAzureStorageService` | Default parameters were removed from the interface | Pass all arguments, or use the convenience overloads in `FEx.AzureStorage.Extensions` |
| Resolving services where injection is not available | `FExServiceProvider.Get<T>()` replaces `ServiceProviderHelper.Instance.GetRequiredService<T>()` | Use sparingly: constructor injection is preferred |

## Project references

| Older project | Current project |
|---|---|
| `FlakEssentials.WpfEx` | `FEx.WPFx` |
| `FlakEssentials.MVVM` | `FEx.MVVM` |
| `FlakEssentials.CommonUtilities.Services` | `FEx.Core`, which brings `FEx.Core.Abstractions` and `FEx.Agnostics.Abstractions` transitively |

Folders under `src/` do not always match the project name (for example `src/Core/FEx.Core.csproj`); reference the project file, not the folder.
