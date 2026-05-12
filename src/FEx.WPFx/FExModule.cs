using FEx.Common;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Json;
using FEx.MVVM;
using FEx.MVVM.Rx.Legacy;
using FEx.Platforms;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Implementations;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;

namespace FEx.WPFx;

[Register(typeof(FExModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(DispatcherContextExecutor), typeof(IFExDispatcher))]
[RegisterModule(typeof(FExBaseModule))]
[RegisterModule(typeof(FExMvvmModule))]
[RegisterModule(typeof(FExMvvmRxModule))]
[RegisterModule(typeof(FExJsonModule))]
[RegisterModule(typeof(FExPlatformsModule))]
[RegisterModule(typeof(FExWpfxModule))]
public class FExModule : InitializeModule<IFExContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExContainer container, IServiceCollection context)
    {
    }
}