# FEx (FlakEssentials)

Reference .NET library - shared framework used as a git submodule across several .NET projects. This file holds FEx-specific conventions.

## Architecture

- `src/` - 45+ projects organized by feature
- `test/` - test projects
- `samples/` - sample apps (WPF, Avalonia, WebAPI)
- `DevConfigs/` - shared build config (.editorconfig, Directory.Build.props/targets)

## Key Rules

- **Multi-target**: `net10.0`, `netstandard2.0`, `netstandard2.1`, `net481` (via MSBuild properties in `Directory.Build.props`)
- Project naming: `FEx.<Feature>` + `FEx.<Feature>.Abstractions` - abstractions always in a separate project (interfaces in `.Abstractions`, implementations in the main project)
- Folder naming: new folders without the `FEx.` prefix (e.g. `src/Sqlx/FEx.Sqlx.csproj`, `src/MSBuildx/FEx.MSBuildx.csproj`)
- Package versions centralized in `DevConfigs/Directory.Build.props` (`DotNetNugetsVersion`, `AvaloniaVersion`, etc.)
- Used as a submodule in other projects - breaking changes ripple to consumers
- Analyzers (via `Directory.Build.targets`): IDisposableAnalyzers, Microsoft.VisualStudio.Threading.Analyzers, ReflectionAnalyzers
- Nullable: only via csproj/props/targets, never `#nullable enable` in a file
- NEVER exclude projects from the build - fix the build instead of bypassing

## Module System

- DI via StrongInject + Microsoft.Extensions.DependencyInjection; `IInitializeModule<>` / `InitializeModule<,>` for modular registration
- MVVM: two variants - CommunityToolkit.Mvvm (`FEx.MVVM`) and ReactiveUI (`FEx.MVVM.Rx`)
- Logging: `IFExLogger` (Serilog under the hood)

## API Surface

- Generate per-project TOML maps: `.\Generate-ApiSurface.ps1 -RepoPath .` -> `.api-surface/FEx/` (one TOML per project)
- **Regenerate after any public API change.** Consumer projects read these to discover FEx APIs

## Testing

- xUnit v3, NSubstitute, Shouldly, coverlet; shared test mocks in `Agnostics.TestMocks`

## Hosting & publish

- Repo: `github.com/Flakroup/FEx`. Use `gh` for PRs/issues.
- Submodule: `DevConfigs` (shared build config) - run `git submodule update --init` after checkout.
- Publish: `pwsh build.ps1 Publish` (NUKE -> nuget.org).

## Build commands

- `pwsh build.ps1 Compile` - build
- `pwsh build.ps1 Test` - tests
- `pwsh build.ps1 Publish` - pack + publish to nuget.org
- `pwsh .\Generate-ApiSurface.ps1` - generate API surface TOML
