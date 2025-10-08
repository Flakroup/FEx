using FEx.Core.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;

namespace FEx.Common.Abstractions.Interfaces;

public interface IFExAppSettings : ISentryConfig, IAppThreadingSettings
{
}