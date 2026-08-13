using System;

namespace FEx.Logging.Abstractions.Enums;

[Flags]
public enum LoggingOptions
{
    Debug = 1 << 1,
    File = 1 << 2,
    Platform = 1 << 3,
    Sentry = 1 << 4,
    Console = 1 << 5,
    DefaultDebugLog = Debug | File | Console,
    DefaultReleaseLog = File | Sentry
}