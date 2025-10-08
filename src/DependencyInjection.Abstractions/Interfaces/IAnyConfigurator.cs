using FEx.DependencyInjection.Abstractions.Enums;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IAnyConfigurator
{
#if NETSTANDARD2_0_OR_GREATER
    ConfigurationPriority Priority { get; }
#else
    ConfigurationPriority Priority => ConfigurationPriority.Default;
#endif
}