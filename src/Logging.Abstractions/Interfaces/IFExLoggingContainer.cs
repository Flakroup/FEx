using FEx.Agnostics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Logging.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExLoggingContainer : IContainer<IFExLogger>, IContainer<ILoggingConfiguration>,
    IContainer<IPlatformLogger>, IContainer<IFExLoggingConfigurator>, IContainer<ISinkConfigurator[]>,
    IContainer<ILogger>, IContainer<ILoggable>, IContainer<ILoggerFactory>, IContainer<LoggerProviderCollection>,
    IContainer<IFExLoggingService>
{
}