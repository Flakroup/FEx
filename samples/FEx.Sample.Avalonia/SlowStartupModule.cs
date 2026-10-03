using System;
using System.Threading.Tasks;
using FEx.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Sample.Avalonia;

/// <summary>
/// Makes the container build really yield (about a second of async work, like reading config or warming a cache),
/// so the sample exercises the async startup path: the startup window is shown and the UI stays responsive meanwhile.
/// </summary>
public sealed class SlowStartupModule : InitializeOnlyModule
{
    public override async ValueTask OnCompleteInitializationAsync(IServiceCollection services) =>
        await Task.Delay(TimeSpan.FromSeconds(1));
}
