# FEx Sample Applications

This directory contains sample applications demonstrating the **FEx Multi-DI pattern**.

## 📁 Samples Overview

### 1. **FEx.Sample.WPF** - StrongInject-Only Pattern
**Technology**: WPF Desktop Application  
**DI Pattern**: StrongInject only (no Microsoft DI)  
**Demonstrates**:
- ✅ StrongInject as base DI container
- ✅ FEx framework initialization with `FExServiceProvider.Initialize<TContainer, TProvider>()`
- ✅ Static service access via `FExLoggingModule.Log<T>()`
- ✅ Clean separation: business logic in standalone classes, DI in `*Module` classes

**How to Run**:
```powershell
dotnet run --project samples/FEx.Sample.WPF/FEx.Sample.WPF.csproj
```

**Key Files**:
- `AppContainer.cs` - Minimal StrongInject container using `[RegisterModule]` and `TestBase`
- `App.xaml.cs` - Application startup with FEx initialization
- `MainWindow.xaml.cs` - UI demonstrating service resolution

**Container Pattern**:
```csharp
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
public partial class AppContainer : TestBase, IContainer<IFExServiceProvider>
{
    // Factories for dependencies...
}
```

---

### 2. **FEx.Sample.WebAPI** - Multi-DI Pattern (StrongInject + Microsoft DI)
**Technology**: ASP.NET Core Web API  
**DI Pattern**: StrongInject + Microsoft DI integration  
**Demonstrates**:
- ✅ StrongInject as foundation DI
- ✅ Microsoft DI opt-in via `FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>()`
- ✅ Engine-agnostic modules implementing `IInitializeModule<IServiceCollection>`
- ✅ ASP.NET Core integration with FEx services
- ✅ Custom app modules alongside FEx framework modules

**How to Run**:
```powershell
dotnet run --project samples/FEx.Sample.WebAPI/FEx.Sample.WebAPI.csproj
```

**Test Endpoints**:
- `GET /health` - Health check with FEx logging
- `GET /weatherforecast` - Sample data with logging

**Key Files**:
- `AppContainer.cs` - StrongInject container with `FExDependencyInjectionModule` and custom `SampleApiModule`
- `Program.cs` - Dual DI initialization: StrongInject first, then Microsoft DI
- `SampleApiModule.cs` - Custom module implementing `IInitializeModule<IServiceCollection>`
- `Controllers/SampleController.cs` - REST API endpoints demonstrating framework integration

**Container Pattern**:
```csharp
[RegisterModule(typeof(FExDependencyInjectionModule))]
[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance, typeof(IFExServiceProvider))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance)]
[Register(typeof(SampleApiModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public partial class AppContainer : TestBase, 
    IContainer<IFExServiceProvider>, 
    IContainer<FExMicrosoftDIServiceProvider>
{
    // Factories for dependencies...
}
```

---

## 🏗️ Architecture Pattern

Both samples follow the **Engine-Agnostic Multi-DI** pattern:

```
┌─────────────────────────────────────────┐
│   Your Application Container           │
│   (StrongInject attributes)             │
└──────────────┬──────────────────────────┘
               │
               │ Initialize<TContainer, TProvider>()
               ▼
┌─────────────────────────────────────────┐
│   FExServiceProvider (Entry Point)      │
│   - Resolves StrongInject provider      │
│   - Calls ConfigureServiceProviderAsync │
└──────────────┬──────────────────────────┘
               │
     ┌─────────┴────────────┐
     │                      │
     ▼                      ▼
┌─────────────┐    ┌──────────────────┐
│ StrongInject│    │ Microsoft DI     │
│ (Always On) │    │ (Optional)       │
└─────────────┘    └──────────────────┘
```

**StrongInject Path** (WPF Sample):
1. Define `AppContainer` with `[Register]` attributes
2. Call `FExServiceProvider.Initialize<AppContainer, FExStrongInjectServiceProvider>()`
3. Use services via `FExServiceProvider.Get<T>()` or static accessors

**Multi-DI Path** (WebAPI Sample):
1. Define `AppContainer` with `[Register]` attributes including `FExMicrosoftDIServiceProvider`
2. Call `FExServiceProvider.Initialize<AppContainer, FExStrongInjectServiceProvider>()`
3. Call `await FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>()`
4. Modules implementing `IInitializeModule<IServiceCollection>` automatically registered
5. Bridge to ASP.NET Core's `IServiceCollection` via builder

---

## 🎯 Key Takeaways

### **Business Logic Classes** (FEx*)
- **Purpose**: Domain logic, configuration, static helpers
- **Inheritance**: `FExInitialize` (not `InitializeModule`)
- **Examples**: `FExWpfx`, `FExMvvm`, `FExPlatforms`, `FExJson`
- **Registration**: Via StrongInject `[Register]` as `IFExInitialize`

### **Module Classes** (*Module)
- **Purpose**: DI registration only
- **Inheritance**: `InitializeModule<TContainer, IServiceCollection>`
- **Method**: `RegisterServices(TContainer container, IServiceCollection services)`
- **Registration**: Via StrongInject `[Register]` as `IInitializeModule<IServiceCollection>`

### **Future Extensibility**
Apps can add support for **any DI engine** (Autofac, Unity, etc.) by:
1. Implementing `IInitializeModule<TTheirEngineContext>`
2. Creating their own service provider (similar to `FExMicrosoftDIServiceProvider`)
3. No changes to FEx framework required!

---

## 📚 Related Documentation

- **Multi-DI Strategy**: `../../Multi-DI-Strategy-Context.md`
- **Migration Guide**: `../../Migration.md`
- **Architecture Rules**: `../../.cursorrules`

