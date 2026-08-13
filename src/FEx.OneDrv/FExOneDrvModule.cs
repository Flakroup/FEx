using FEx.DependencyInjection.Abstractions;
using FEx.OneDrv.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.OneDrv;

public sealed class FExOneDrvModule : InitializeOnlyModule
{
    private readonly OneDriveOptions _options;

    public FExOneDrvModule(OneDriveOptions options)
    {
        _options = options;
    }

    protected override void RegisterServices(object? container, IServiceCollection services)
    {
        services.AddOneDrv(_options);
    }
}