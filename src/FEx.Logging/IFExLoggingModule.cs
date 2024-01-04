using Microsoft.Extensions.Logging;
using StrongInject;

namespace FEx.Logging;

public interface IFExLoggingModule : IContainer<ILogger>
{
}