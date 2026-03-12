# FEx (FlakEssentials)

Reference .NET library - shared framework used as a git submodule across projects (DotNetMate, miGit, etc.).

## Architecture

- `src/` - 45+ projects organized by feature
- `test/` - test projects
- `samples/` - sample apps (WPF, Avalonia, WebAPI)
- `DevConfigs/` - shared build config (.editorconfig, Directory.Build.props/targets)

## Key Rules

- **Multi-target**: `net10.0`, `netstandard2.0`, `netstandard2.1`, `net481` (via MSBuild properties in Directory.Build.props)
- Project naming: `FEx.<Feature>` and `FEx.<Feature>.Abstractions`
- Folder naming: new folders without `FEx.` prefix (e.g., `src/Sqlx/FEx.Sqlx.csproj`, `src/MSBuildx/FEx.MSBuildx.csproj`)
- Abstractions always in separate projects - interfaces in Abstractions, implementations in the main project
- Package versions centralized in `DevConfigs/Directory.Build.props` (`DotNetNugetsVersion`, `AvaloniaVersion`, etc.)
- This repo is used as a **submodule** in other projects - breaking changes affect DotNetMate, miGit, and others
- Analyzers: IDisposableAnalyzers, Microsoft.VisualStudio.Threading.Analyzers, ReflectionAnalyzers (via Directory.Build.targets)

## Module System

- DI via StrongInject + Microsoft.Extensions.DependencyInjection
- `IInitializeModule<>` / `InitializeModule<,>` for modular DI registration
- MVVM: two variants - CommunityToolkit.Mvvm (`FEx.MVVM`) and ReactiveUI (`FEx.MVVM.Rx`)
- Logging: `IFExLogger` / Serilog under the hood

## GitLab Integration

- Use **GitLab MCP** server for: merge requests, issues, pipelines, branches

## API Surface

- Generate per-project TOML API surface maps: `.\Generate-ApiSurface.ps1 -RepoPath .`
- Output: `.api-surface/FEx/` directory with one TOML file per project
- **Must be regenerated** after any public API changes
- Other projects (DotNetMate, miGit) reference these files to discover available FEx APIs

## Testing

- xUnit v3, NSubstitute, Shouldly, coverlet
- Shared test mocks in `Agnostics.TestMocks`
- 38 tests: 14 DI + 6 Logging + 18 Flurlx
