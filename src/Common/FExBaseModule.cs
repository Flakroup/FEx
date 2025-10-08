using FEx.Common.Abstractions.Interfaces;
using FEx.Core;
using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;

namespace FEx.Common;

[Register(typeof(FExBaseModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[RegisterModule(typeof(FExDependencyInjectionModule))]
[RegisterModule(typeof(FExCoreModule))]
[RegisterModule(typeof(FExLoggingModule))]
[RegisterModule(typeof(FExCommonModule))]
public class FExBaseModule : InitializeModule<IFExBaseContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExBaseContainer container, IServiceCollection context)
    {
    }
}