# FEx architecture

FEx (Flakroup Extensions) is a modular, cross-platform .NET framework. Repositories consume it either as a git submodule or as the NuGet packages published to nuget.org. This page describes how the projects fit together. Rules for working in the repository are in [`CLAUDE.md`](../CLAUDE.md); the reasoning behind individual choices is in [`docs/decisions/`](decisions/README.md).

## Repository layout

| Path | Contents |
|---|---|
| `src/` | 62 library projects, one folder each (see [Projects](#projects)) |
| `test/` | 41 test projects |
| `samples/` | Three runnable apps: `FEx.Sample.WPF`, `FEx.Sample.Avalonia`, `FEx.Sample.WebAPI` |
| `build/` | The NUKE build of this repository (`build.ps1` is the entry point) |
| `DevConfigs/` | Git submodule with the shared build configuration: `.editorconfig`, `Directory.Build.props` and `.targets`, ReSharper settings and their validators |
| `docs/` | This documentation |

## Target frameworks

The target framework sets are defined once, in `DevConfigs/Directory.Build.props`, and projects refer to them by property:

| Property | Targets | Used by |
|---|---|---|
| `StandardTargetFrameworkIds` | `net10.0`, `netstandard2.1`, `netstandard2.0` | Most library projects, including all of the core layers |
| `WindowsTargetFrameworkIds` | `net481`, `net10.0-windows` | Windows-only UI and tooling projects such as `FEx.WPFx` and `FEx.MSBuildx` |
| `FullTargetFrameworkIds` | `net481`, `netstandard2.1`, `net10.0` | `FEx.Sqlx` |
| `TargetFrameworkId` | `net10.0` | Projects that need a modern runtime, such as `FEx.Avaloniax`, `FEx.AspNetCorex`, `FEx.Blazorx` and `FEx.Offline` |

`FEx.Analyzers` targets `netstandard2.0` only, as an analyzer package must. The csproj of a project is the authority for what it targets.

## Layers

Dependencies point downwards. A project never references a project in a layer above it. The table lists the direct project references of the core layers.

| Layer | Project | Direct project references |
|---|---|---|
| 1 | `FEx.Agnostics.Abstractions` | none |
| 2 | `FEx.Agnostics` | `FEx.Agnostics.Abstractions` |
| 2 | `FEx.DependencyInjection.Abstractions` | `FEx.Agnostics.Abstractions` |
| 3 | `FEx.DependencyInjection` | `FEx.DependencyInjection.Abstractions` |
| 3 | `FEx.Logging.Abstractions` | `FEx.Agnostics.Abstractions`, `FEx.Agnostics`, `FEx.DependencyInjection.Abstractions` |
| 4 | `FEx.Logging` | `FEx.Agnostics.Abstractions`, `FEx.Agnostics`, `FEx.Logging.Abstractions` |
| 4 | `FEx.Core.Abstractions` | `FEx.Agnostics`, `FEx.DependencyInjection.Abstractions`, `FEx.Logging.Abstractions` |
| 5 | `FEx.Core` | `FEx.Core.Abstractions`, `FEx.Agnostics`, `FEx.Logging.Abstractions` |
| 5 | `FEx.Common.Abstractions` | `FEx.DependencyInjection.Abstractions`, `FEx.Core.Abstractions`, `FEx.Logging.Abstractions` |
| 6 | `FEx.Common` | `FEx.Common.Abstractions`, `FEx.DependencyInjection`, `FEx.Core`, `FEx.Logging` |

What each core layer holds:

- **Agnostics.Abstractions** - framework-independent building blocks: `IFExLogger` and the static logger `FExStaticLogger`, `ISynchronizedAccessService`, `AsyncStatics`, the `FExInitializable` base class, and the extension methods for collections, strings, dates, IO, tasks and exceptions. `FEx.Agnostics` adds implementations: `NotifyPropertyChanged`, the concurrent collections such as `ConcurrentList<T>`, comparers and utilities such as `Cmd`.
- **DependencyInjection** - the module system and the service providers (see [Module system](#module-system)).
- **Logging** - `FExSerilogLogger`, the Serilog implementation of `IFExLogger`, plus sinks and configuration. The logger interface itself lives in `FEx.Agnostics.Abstractions` so that even the lowest layers can log.
- **Core** - threading and dispatching abstractions, the exception handler and other static services (`FExCoreStatics`), reactive subjects (`FExSubject<T>`) and concurrent observable collections (`ConcurrentObservableList<T>`).
- **Common** - orchestration: `FExBaseModule` composes the dependency injection, core, logging and common modules into one registration.

Feature projects (everything else under `src/`) sit above these layers. They reference the core layers and, where needed, each other, but the core layers never reference a feature project. `FEx.Legacy` is the one feature project that several others build on (`FEx.WPFx`, `FEx.AzureDevOpsx` and `FEx.Imaging.Windows` reference it); it references only `FEx.MVVM`, `FEx.MVVM.Rx` and `FEx.Webx`.

## Abstractions split

A module that exposes a contract keeps the contract in a separate `FEx.<Feature>.Abstractions` project and the implementation in `FEx.<Feature>`. Consumers that only need to program against the interface reference the small project. An Abstractions project never references its own implementation project, but it may reference lower-layer implementation projects: `FEx.Core.Abstractions`, for example, references `FEx.Agnostics`.

Modules that have the split today: Agnostics, AspNetCorex, Common, Core, DependencyInjection, Json, Logging, MVVM, Offline, OneDrv, PersistentStorage, SecureStorage and Sqlx. Other modules (for example Asyncx, Downloader, Flurlx, Telemetry and WPFx) are single projects. New modules follow the split.

## Module system

Dependency injection in FEx is built around two engines that work side by side:

- **StrongInject** is the compile-time container. An application declares a partial container class and composes FEx modules with `[RegisterModule(typeof(...))]`. `FEx.Common` ships `FExBaseModule` (dependency injection, core, logging and common modules); `FEx.WPFx` and `FEx.Avaloniax` each ship an `FExModule` for their UI stack.
- **Microsoft.Extensions.DependencyInjection** is the runtime engine, for libraries and hosts that need `IServiceCollection` (ASP.NET Core, EF Core, `HttpClient` factories).

The pieces, all in `FEx.DependencyInjection.Abstractions` unless noted:

| Type | Role |
|---|---|
| `IInitializeModule<TEngineContext>` | A module: `RegisterServices(context)`, then `CompleteInitializationAsync(context)`; `HasBeenCompleted` reports whether the second step ran |
| `InitializeModule<TContainer, TEngineContext>` | Base class of a module. `TContainer` is the container type the module resolves its services from (the default container held by `FExServiceProvider`); `TEngineContext` is the engine's registration context, `IServiceCollection` for Microsoft DI. A module names the `IFExInitializable` modules it depends on through its constructor |
| `InitializeOnlyModule` | A Microsoft DI module that needs no container access (`InitializeModule<object, IServiceCollection>`) |
| `FExServiceProvider` | Static entry point. `InitializeAsync<TContainer>()` creates the application's StrongInject container once (repeat calls return the same one) and wires it up; `Get<T>()` and its variants resolve services where injection is not available, which is discouraged |
| `IFExServiceContainer` / `FExServiceContainer` (the implementation is in `FEx.DependencyInjection`) | Resolves from the StrongInject container and, when an `IServiceCollection` is supplied, runs the registered `IMicrosoftDIConfigurator` instances against it |
| `FExStrongInjectServiceProvider`, `FExMicrosoftDIServiceProvider` (in `FEx.DependencyInjection`) | The two `IFExServiceProvider` implementations. The Microsoft DI one collects every registered `IInitializeModule<IServiceCollection>` from the container, runs `RegisterServices` and `CompleteInitializationAsync` on each, and builds the `ServiceProvider` with `ValidateOnBuild` |

Application startup therefore reads: define a container that composes the modules, call `FExServiceProvider.InitializeAsync<AppContainer>()`, then resolve services. `samples/FEx.Sample.WPF/AppContainer.cs` is the reference for a StrongInject-first desktop application.

Two initialization primitives are used throughout:

- `FExInitializable` / `IFExInitializable` - an object that initializes once, after the dependencies it was given have initialized.
- `AsyncInitializable` in `FEx.Asyncx` - a base class for services with asynchronous setup. Subclasses override `OnInitializeAsync()`; callers use `InitializeAsync()`, `BeginInitialization()` and the `IsInitialized`, `IsInitializing` and `HasFinishedInitialization` flags.

## MVVM

There are two interchangeable MVVM stacks over a shared abstractions project:

- `FEx.MVVM` - built on CommunityToolkit.Mvvm.
- `FEx.MVVM.Rx` - built on ReactiveUI (`ViewModelBase` lives here, in namespace `FEx.MVVM.Rx.BaseObjects`).
- `FEx.MVVM.Abstractions` - the dialog and popup service contracts, linkable properties (`LinkManager`, `ILinkableNotifyPropertyChanged`) and tree view nodes that both stacks use.

The UI integrations (`FEx.WPFx`, `FEx.Avaloniax`) build on these.

## Projects

Roles are taken from each project's package description.

| Area | Projects |
|---|---|
| Core layers | `FEx.Agnostics(.Abstractions)`, `FEx.DependencyInjection(.Abstractions)`, `FEx.Logging(.Abstractions)`, `FEx.Core(.Abstractions)`, `FEx.Common(.Abstractions)` |
| UI | `FEx.WPFx` (WPF integration, bootstrapper, splash screen), `FEx.Avaloniax` (Avalonia integration), `FEx.Blazorx` (MudBlazor integration), `FEx.MVVM`, `FEx.MVVM.Rx`, `FEx.MVVM.Abstractions` |
| ASP.NET Core | `FEx.AspNetCorex(.Abstractions)` (idempotent-POST replay middleware, JWT bearer auth, health probe), `FEx.Logging.Web`, `FEx.Telemetry.Sentry.Web` |
| Serialization | `FEx.Json` (Newtonsoft.Json extensions and converters), `FEx.Json.Abstractions` (`IFExJsonSerializer`), `FEx.Json.SystemTextJsonx` (System.Text.Json implementation) |
| Data and storage | `FEx.EFCore`, `FEx.Sqlx(.Abstractions)`, `FEx.PersistentStorage(.Abstractions)` and `FEx.PersistentStorage.Rx` (LiteDB-backed storage and caching), `FEx.SecureStorage(.Abstractions)`, `FEx.Encryption`, `FEx.AppSettings`, `FEx.Imaging.Windows` |
| Networking | `FEx.Flurlx` and `FEx.Flurlx.Newtonsoft` (Flurl HTTP client with Polly), `FEx.Webx`, `FEx.Downloader` (chunked, resumable downloads), `FEx.FTPx`, `FEx.WebScraping`, `FEx.Offline(.Abstractions)` (draft autosave, idempotent write outbox and read cache for browser clients) |
| Cloud services | `FEx.AzureStorage`, `FEx.AzureDevOpsx`, the Azure secret-store client project, `FEx.OneDrv(.Abstractions)` |
| Telemetry | `FEx.Telemetry`, `FEx.Telemetry.Sentry` |
| Utilities | `FEx.Asyncx`, `FEx.FileSystem`, `FEx.CLI`, `FEx.NuGetx`, `FEx.MSBuildx`, `FEx.RESXx`, `FEx.Platforms`, `FEx.Platforms.Windows`, `FEx.Legacy` |
| Tooling | `FEx.Building` (reusable NUKE build components), `FEx.Analyzers` (Roslyn analyzers shipped as a package), `FEx.Agnostics.TestMocks` (shared test doubles) |

## Conventions

| Topic | Rule |
|---|---|
| Names | `FEx.<Feature>` plus `FEx.<Feature>.Abstractions`. A module named after a trademarked product takes an `x` suffix (`FEx.WPFx`, `FEx.Blazorx`, `FEx.AspNetCorex`, `FEx.Avaloniax`); a generic name stays plain (`FEx.Offline`, `FEx.Logging`) |
| Folders | Newer folders omit the `FEx.` prefix, for example `src/Sqlx/FEx.Sqlx.csproj` |
| Nullable | Enabled for every project from `DevConfigs/Directory.Build.props`. It is set only in MSBuild files, never with `#nullable` in source. PolySharp supplies the nullable attributes on `netstandard2.0` and `net481` |
| Implicit usings | Disabled: every file lists its `using` directives |
| Warnings | `TreatWarningsAsErrors` is on for the whole repository, except CS1591 (missing XML comment). A false positive is suppressed at the call site with a justification |
| Package versions | Centralized in `DevConfigs/Directory.Build.props` (for example `DotNetNugetsVersion`, `AvaloniaVersion`) |
| Analyzers | IDisposableAnalyzers, Microsoft.VisualStudio.Threading.Analyzers and ReflectionAnalyzers are referenced for every project by `DevConfigs/Directory.Build.targets` |
| Public API | FEx is consumed as a submodule, so a breaking change reaches every consumer. It goes in the `Breaking changes` section of the changelog |

## Build and quality gates

`pwsh build.ps1 <Target>` runs the NUKE build. `build/Build.cs` composes the reusable targets from `FEx.Building`:

| Target | What it does |
|---|---|
| `Compile` | Restore and build the solution (`FEx.slnx`) |
| `Test` | Run the xUnit v3 suites |
| `Coverage` | Run the suites instrumented and fail below 100% coverage for any file that is not on the explicit debt list in `build/CoverageDebt.cs`. The list can only shrink |
| `Inspect` | Run ReSharper `inspectcode` and fail on any error-severity finding. Manual only; CI does not run it |
| `Pack`, `Publish`, `Tag` | Pack the NuGet packages, push them to nuget.org and tag the version |

Continuous integration (`.github/workflows/`) runs `Coverage` on every pull request, checks that a pull request that changes shipped paths adds a `CHANGELOG.md` entry under `[Unreleased]` (unless it carries the `no-changelog` label), scans for secrets, and publishes from `main` and `develop`. NuGet vulnerability auditing is always on, including transitive packages (see [ADR 0003](decisions/0003-nuget-audit-runs-on-every-build.md)). The validators for the shared configuration run in the `DevConfigs` repository (see [ADR 0006](decisions/0006-devconfigs-shared-layer-holds-configuration-only.md)).

Tests use xUnit v3, NSubstitute, Shouldly and coverlet. Shared test doubles live in `FEx.Agnostics.TestMocks`.

## Discovering the public API

The public API of every project is served on demand by the external ApiSurfaceMcp server, which scans the source with Roslyn. Nothing is generated or committed in this repository. See [ADR 0001](decisions/0001-api-surface-served-by-external-mcp-server.md).

## Decisions

The architectural decision records are indexed in [`docs/decisions/`](decisions/README.md). Moving from the older FlakEssentials namespaces is covered in [`docs/migration/namespace-mapping.md`](migration/namespace-mapping.md).
