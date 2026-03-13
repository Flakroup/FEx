using FEx.Logging.Abstractions.Enums;

namespace FEx.Logging.Abstractions.Extensions;

public static class LoggingOptionsExtensions
{
    public static bool HasFlagFast(this LoggingOptions value, LoggingOptions flag) => (value & flag) != 0;
}