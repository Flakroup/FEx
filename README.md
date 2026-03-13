# FEx Framework

**Next-generation, multi-platform, multi-DI .NET application framework**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-22%2F22-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-9.0%20%7C%20Standard%202.0%2F2.1%20%7C%204.8.1-512BD4)]()
[![License](https://img.shields.io/badge/license-Flakroup-blue)]()

---

## 🎯 What is FEx?

**FEx** (Flak Essentials eXtended) is a modern, extensible application framework for .NET that provides:

- ✅ **Multi-Platform Support** - .NET 9.0, .NET Standard 2.0/2.1, .NET Framework 4.8.1
- ✅ **Multi-DI Engine Architecture** - Use StrongInject, Microsoft DI, or bring your own
- ✅ **Framework-Agnostic Core** - Business logic independent of DI engine choice
- ✅ **Rich Extension Library** - 32+ extension method classes for common operations
- ✅ **Modular Design** - Use only what you need
- ✅ **Production Ready** - Built on 6+ months of architectural refinement

**FEx** is the next generation of the **FlakEssentials** framework, redesigned from the ground up to break free from .NET Framework 4.8.1 and Microsoft DI dependencies, enabling true cross-platform and multi-DI flexibility.

---

## 🏗️ Architecture Overview

FEx follows a **clean, layered architecture** with strict dependency hierarchy:

```
┌─────────────────────────────────────────────────────────┐
│           FEx.Common (Top Layer)                        │
│           Orchestrates all framework components         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────┐
│  Feature Projects (WPFx, MVVM, Json, EFCore, etc.)     │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────┐
│  FEx.Logging → Logging.Abstractions                     │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────┐
│  FEx.Core → Core.Abstractions                           │
│  Framework-dependent utilities, stack traces, etc.      │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────┐
│  FEx.DependencyInjection → DI.Abstractions              │
│  Multi-DI engine coordination & service resolution      │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────┐
│  FEx.Agnostics → Agnostics.Abstractions                 │
│  Framework-independent utilities & extensions           │
│  (32+ extension classes, collections, helpers)          │
└─────────────────────────────────────────────────────────┘
```

### Core Principles

1. **Abstractions First** - Interfaces, extensions, DTOs live in `*.Abstractions` projects
2. **No Circular Dependencies** - Strict unidirectional dependency flow
3. **Framework Independence** - Core utilities work without DI or framework dependencies
4. **Separation of Concerns** - Business logic in `FEx*` classes, DI registration in `*Module` classes

---

## 🚀 Quick Start

### Installation

```bash
# Core framework (required)
dotnet add package FEx.Common

# Optional feature packages
dotnet add package FEx.MVVM          # MVVM patterns
dotnet add package FEx.WPFx          # WPF utilities
dotnet add package FEx.Logging       # Logging infrastructure
dotnet add package FEx.Json          # JSON utilities
dotnet add package FEx.EFCore        # Entity Framework Core extensions
# ... and many more
```

### Basic Usage (StrongInject Only)

```csharp
using FEx.Agnostics.TestMocks;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions.Interfaces;
using StrongInject;

// Define your container
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
public partial class AppContainer : TestBase, IContainer<IFExServiceProvider>
{
}

// Initialize framework
public class Program
{
    public static void Main()
    {
        // Initialize FEx with StrongInject
        FExServiceProvider.Initialize<AppContainer>();
        
        // Use services
        var logger = FExLoggingModule.Log<Program>();
        logger.Information("FEx initialized successfully!");
        
        // Your app logic here...
    }
}
```

### Advanced Usage (Multi-DI with Microsoft DI)

```csharp
using Microsoft.Extensions.DependencyInjection;

// Extend your container to support Microsoft DI
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
public partial class AppContainer : TestBase, 
    IContainer<IFExServiceProvider>,
    IContainer<FExMicrosoftDIServiceProvider>
{
}

// Initialize both DI engines
public class Program
{
    public static async Task Main()
    {
        // 1. Initialize StrongInject (foundation)
        FExServiceProvider.Initialize<AppContainer>();
        
        // 2. Initialize Microsoft DI (optional, for ASP.NET Core integration)
        await FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>();
        
        // 3. Get Microsoft DI provider
        var msProvider = FExServiceProvider.Get<FExMicrosoftDIServiceProvider>();
        
        // 4. Use in ASP.NET Core
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton(msProvider.ServiceCollection);
        
        // Your app logic here...
    }
}
```

---

## 🎨 Key Features

### 🔧 Multi-DI Engine Architecture

FEx's **killer feature** is its engine-agnostic dependency injection system:

```
┌─────────────────────────────────────────┐
│   Your Application Container           │
│   (StrongInject attributes)             │
└──────────────┬──────────────────────────┘
               │
               │ FExServiceProvider.Initialize()
               ▼
┌─────────────────────────────────────────┐
│   FExServiceProvider (Entry Point)      │
│   - Resolves services                   │
│   - Coordinates multiple DI engines     │
└──────────────┬──────────────────────────┘
               │
     ┌─────────┴────────────┐
     │                      │
     ▼                      ▼
┌─────────────┐    ┌──────────────────┐
│ StrongInject│    │ Microsoft DI     │
│ (Always On) │    │ (Optional)       │
└─────────────┘    └──────────────────┘
     │                      │
     └─────────┬────────────┘
               │
     ┌─────────┴────────────┐
     │                      │
     ▼                      ▼
  (Future)              (Future)
  Autofac               Unity
```

**Benefits:**
- ✅ Start with StrongInject (compile-time DI)
- ✅ Add Microsoft DI when needed (ASP.NET Core)
- ✅ Framework code is engine-agnostic
- ✅ Add new DI engines without touching framework
- ✅ No vendor lock-in

### 📦 Rich Extension Library

32+ extension method classes in `FEx.Agnostics.Abstractions`:

**Collections & Data**
- `CollectionExtensions`, `EnumerableExtensions`, `ListExtensions`
- `DictionaryExtensions`, `ArrayExtensions`, `ReadOnlyDictionaryExtensions`

**Strings & Primitives**
- `StringExtensions` - Parsing, validation, formatting
- `EnumExtensions` - Attribute access, conversion
- `DateTimeExtensions`, `TimeSpanExtensions`, `DoubleExtensions`

**I/O & System**
- `StreamExtensions`, `FileInfoExtensions`, `DirectoryInfoExtensions`
- `FileSystemInfoExtensions`

**Web & Network**
- `UriExtensions`, `WebClientExtensions`, `WebRequestExtensions`
- `WebResponseExtensions`

**Async & Threading**
- `TaskExtensions`, `SemaphoreSlimExtensions`, `AsyncOptionsExtensions`

**Advanced**
- `TypeExtensions`, `ObjectExtensions`, `ExceptionExtensions`
- `GuardExtensions`, `EventsExtensions`, `WhenResultExtensions`

### 🧩 Modular Project Structure

| Project Category | Description | Examples |
|------------------|-------------|----------|
| **Core** | Foundation & DI | `Agnostics`, `Core`, `DependencyInjection`, `Logging`, `Common` |
| **UI Frameworks** | Platform-specific UI | `WPFx`, `Avaloniax`, `Maui` |
| **Data & Persistence** | Storage & DB | `EFCore`, `PersistentStorage`, `AzureStorage` |
| **MVVM & Patterns** | UI patterns | `MVVM`, `MVVM.Abstractions`, `MVVM.Rx` |
| **Utilities** | Specialized tools | `Json`, `Encryption`, `Downloader`, `FileSystem` |
| **Cloud & Integration** | External services | `KeyVault`, `OneDrive`, `Telemetry` |
| **Web** | HTTP & scraping | `Webx`, `Flurlx`, `WebScraping` |
| **Legacy Support** | Backward compat | `Legacy` |

---

## 📖 Examples

### Example 1: WPF Desktop Application (StrongInject Only)

See complete working sample: [`samples/FEx.Sample.WPF`](samples/FEx.Sample.WPF)

**Key Highlights:**
- StrongInject-only pattern (no Microsoft DI)
- Static service access via `FExLoggingModule.Log<T>()`
- Clean container definition with `[RegisterModule]`

### Example 2: ASP.NET Core Web API (Multi-DI)

See complete working sample: [`samples/FEx.Sample.WebAPI`](samples/FEx.Sample.WebAPI)

**Key Highlights:**
- StrongInject + Microsoft DI integration
- Engine-agnostic modules via `IInitializeModule<IServiceCollection>`
- Seamless ASP.NET Core integration

**For detailed architecture explanation, see** → [`samples/README.md`](samples/README.md)

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/FEx.DependencyInjection.Tests
dotnet test test/FEx.Logging.Tests
```

**Current Test Coverage:**
- ✅ 22/22 tests passing
- ✅ DI Architecture (16 tests)
- ✅ Logging Infrastructure (6 tests)

---

## 🗂️ Project Structure

```
FEx/
├── src/                          # Source projects (42 total)
│   ├── Agnostics.Abstractions/   # Framework-independent interfaces
│   ├── Agnostics/                # Framework-independent implementations
│   ├── Agnostics.TestMocks/      # Test utilities
│   ├── DependencyInjection.Abstractions/
│   ├── DependencyInjection/
│   ├── Core.Abstractions/
│   ├── Core/
│   ├── Logging.Abstractions/
│   ├── Logging/
│   ├── Common.Abstractions/
│   ├── Common/                   # Top-level orchestration
│   ├── FEx.MVVM/                 # MVVM patterns
│   ├── FEx.WPFx/                 # WPF utilities
│   ├── FEx.Json/                 # JSON utilities
│   ├── FEx.EFCore/               # Entity Framework Core
│   └── ... (28 more feature projects)
│
├── test/                         # Test projects (7 total)
│   ├── FEx.DependencyInjection.Tests/
│   ├── FEx.Logging.Tests/
│   └── ... (5 more)
│
├── samples/                      # Sample applications
│   ├── FEx.Sample.WPF/          # WPF desktop app
│   ├── FEx.Sample.WebAPI/       # ASP.NET Core Web API
│   └── README.md                # Detailed architecture guide
│
├── DevConfigs/                   # Build configuration
│   ├── Directory.Build.props
│   └── Directory.Build.targets
│
├── Directory.Build.props         # Solution-wide settings
├── Directory.Build.targets
├── FEx.sln                       # Solution file
└── README.md                     # This file
```

---

## 📋 Target Frameworks

FEx supports **multiple target frameworks** for maximum compatibility:

| Project Type | Frameworks | Use Case |
|--------------|------------|----------|
| **Core/Agnostics** | .NET 9.0, .NET Standard 2.0, 2.1 | Maximum compatibility |
| **Windows-specific** | .NET 9.0-windows, .NET 4.8.1 | WPF, platform APIs |
| **Feature Projects** | .NET 9.0, .NET Standard 2.1 | Modern APIs |
| **Tests** | .NET 9.0 | Latest features |

---

## 🛠️ Build & Development

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022+ or JetBrains Rider
- (Optional) StrongInject source generator support

### Building

```bash
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Build specific project
dotnet build src/Agnostics/FEx.Agnostics.csproj

# Build in Release mode
dotnet build -c Release
```

### Development Workflow

1. **Make changes** in appropriate layer (Agnostics → Core → Feature)
2. **Build affected projects**: `dotnet build --no-restore`
3. **Run tests**: `dotnet test --no-build`
4. **Test in samples**: Run `FEx.Sample.WPF` or `FEx.Sample.WebAPI`
5. **Commit** with meaningful message (handled by git hooks)

---

## 📚 Documentation

- **Samples Guide**: [`samples/README.md`](samples/README.md) - Detailed architecture patterns
- **Migration Rules**: [`.cursorrules`](.cursorrules) - Architecture guidelines & merge workflow
- **Build Configuration**: [`DevConfigs/README.md`](DevConfigs/README.md) - Build system details

---

## 🔗 Related Projects

- **FlakEssentials** - Original .NET 4.8.1 framework (predecessor to FEx)
- **StrongInject** - Compile-time dependency injection library
- **Serilog** - Structured logging library (used in FEx.Logging)

---

## 🎯 Roadmap

### ✅ Completed (Current State)
- [x] Multi-DI architecture (StrongInject + Microsoft DI)
- [x] Core framework projects (Agnostics, Core, DI, Logging, Common)
- [x] 28+ feature projects (MVVM, WPFx, Json, EFCore, etc.)
- [x] Sample applications (WPF + WebAPI)
- [x] Test infrastructure (22 tests passing)
- [x] .NET 9.0 support
- [x] Cross-platform compatibility

### 🚧 In Progress
- [ ] **FlakEssentials Migration** - Migrating remaining components from FlakEssentials framework
- [ ] **Warning Cleanup** - Systematic resolution of analyzer warnings (post-migration)
- [ ] **Expanded Test Coverage** - Additional tests for feature projects

### 🔮 Future Plans
- [ ] NuGet package publishing
- [ ] Additional DI engine support (Autofac, Unity)
- [ ] Avalonia UI enhancements
- [ ] MAUI framework completion
- [ ] Performance benchmarking suite
- [ ] Comprehensive API documentation

---

## 🤝 Contributing

FEx is actively developed by **Flakroup**. Contributions, feedback, and issue reports are welcome!

### Development Guidelines

1. **Follow the architecture hierarchy** - No circular dependencies
2. **Respect layer boundaries** - Abstractions = interfaces/extensions, Implementation = complex logic
3. **Preserve existing patterns** - Multi-DI support, initialization patterns
4. **Write tests** - Add tests for new functionality
5. **Update documentation** - Keep README and samples in sync

---

## 📄 License

Copyright © **Flakroup** 2025-2026. All rights reserved.

---

## 🙏 Acknowledgments

FEx is the evolution of **FlakEssentials**, rebuilt from the ground up to enable:
- ✅ True cross-platform compatibility
- ✅ DI engine independence
- ✅ Modern .NET 9.0 features
- ✅ Clean, maintainable architecture

Special thanks to the .NET community for excellent libraries:
- **StrongInject** - Compile-time DI
- **Serilog** - Structured logging
- **Microsoft.Extensions.DependencyInjection** - Runtime DI integration

---

## 📞 Contact & Support

- **Organization**: Flakroup
- **Repository**: Internal GitLab
- **Status**: Production Ready (pending FlakEssentials migration completion)

---

<div align="center">

**Built with ❤️ by Flakroup**

*Next-generation framework for next-generation applications*

</div>
