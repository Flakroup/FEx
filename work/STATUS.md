# FEx Project Status

**Last Verified**: 2026-03-13
**Branch**: `FlakEssentialsMerge`
**Last Commit**: `a73207d` - Remove obsolete files, replace API catalog generator
**.NET SDK**: 10.0.104

---

## Build & Test

| Metric | Value | Notes |
|--------|-------|-------|
| **Build** | 0 errors | ~53 warnings (analyzers: IDISP, VSTHRD, REFL, CA, SI) |
| **Tests Total** | 38/38 passing | All green |
| **DI Tests** | 14/14 | `FEx.DependencyInjection.Tests` |
| **Logging Tests** | 6/6 | `FEx.Logging.Tests` |
| **Flurlx Tests** | 18/18 | `FEx.Flurlx.Tests` (incl. WireMock integration) |

---

## Architecture

```
FEx.Agnostics.Abstractions     - Framework-independent interfaces
FEx.Agnostics                  - Framework-independent implementations
FEx.DependencyInjection.Abstractions
FEx.DependencyInjection        - Multi-DI (StrongInject + MS DI)
FEx.Core.Abstractions
FEx.Core
FEx.Logging.Abstractions
FEx.Logging                    - IFExLogger (Serilog backend)
FEx.Common.Abstractions
FEx.Common                     - Top layer
```

### Satellite Projects

| Project | Purpose |
|---------|---------|
| FEx.Asyncx | Async utilities, semaphores, reader-writer locks |
| FEx.AppSettings | User settings persistence |
| FEx.Json | JSON utilities (Newtonsoft) |
| FEx.Encryption | Encryption helpers |
| FEx.EFCore | Entity Framework Core services, bulk ops, reactive cache |
| FEx.Flurlx | HTTP client (Flurl + Polly resilience) |
| FEx.Downloader | HTTP download service |
| FEx.Webx | Web utilities |
| FEx.FTPx | FTP client (FluentFTP) |
| FEx.NuGetx | NuGet package management |
| FEx.CLI | Command-line parser helpers |
| FEx.FileSystem | Compression, file utilities |
| FEx.SecureStorage | Secure key storage |
| FEx.OneDrv | OneDrive integration |
| FEx.Platforms.Windows | Windows-specific (ACL, UAC, identity) |
| FEx.MVVM.Abstractions | Reactive base view models |
| FEx.MVVM | MVVM controls |
| FEx.MVVM.Rx | ReactiveUI integration |
| FEx.WPFx | WPF framework utilities |
| FEx.Avaloniax | Avalonia framework utilities |
| FEx.Legacy | Legacy compatibility layer |
| FEx.Telemetry | Sentry integration |
| FEx.Telemetry.Rollbar | Rollbar SDK integration |
| FEx.PersistentStorage | LiteDB-based storage |
| FEx.RESXx | RESX file utilities |
| FEx.KeyVault | Azure KeyVault integration |
| FEx.AzureStorage | Azure Storage integration |
| FEx.MSBuildx | MSBuild project/solution parsing |
| FEx.Sqlx | SQL Server utilities (SqlDbHelper, SQLInstanceInfo) |
| FEx.Sqlx.Abstractions | ISqlDbHelper interface |
| FEx.Imaging.Windows | Windows image caching (FilesCacheService, CachedImage) |
| FEx.AzureDevOpsx | Azure DevOps/TFS integration (net481) |
| FEx.Building | NUKE build helpers (AssemblyInfo, RepositoryInfo) |

---

## Samples

| Sample | Framework | Status |
|--------|-----------|--------|
| FEx.Sample.WebAPI | ASP.NET Core | Working |
| FEx.Sample.WPF | WPF (.NET 10) | Working |
| FEx.Sample.Avalonia | Avalonia | Working |
