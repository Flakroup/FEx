using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Logging.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExLoggingContainer : IContainer<ILogger>, IContainer<ILoggable>, IContainer<ILoggingService>,
    IContainer<FExLoggingConfigurator>, IContainer<ILoggerFactory>, IContainer<LoggerProviderCollection>
{
}