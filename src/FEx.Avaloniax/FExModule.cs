using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Common;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json;
using FEx.MVVM;
using FEx.MVVM.Rx;
using FEx.Platforms;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(FExModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher))]
[RegisterModule(typeof(FExBaseModule))]
[RegisterModule(typeof(FExMvvmModule))]
[RegisterModule(typeof(FExMvvmRxModule))]
[RegisterModule(typeof(FExJsonModule))]
[RegisterModule(typeof(FExPlatformsModule))]
[RegisterModule(typeof(FExAvaloniaxModule))]
public class FExModule : InitializeModule<IFExContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExContainer container, IServiceCollection context)
    {
    }
}