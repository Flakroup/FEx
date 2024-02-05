using Microsoft.Extensions.Logging;
using StrongInject;

namespace FEx.Logging.Abstractions.Interfaces;

public interface IFExLoggingModule : IContainer<ILogger>, IContainer<ILoggable>
{
}