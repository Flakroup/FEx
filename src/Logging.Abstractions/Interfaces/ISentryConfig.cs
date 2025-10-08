namespace FEx.Logging.Abstractions.Interfaces;

public interface ISentryConfig
{
    string SentryDsn { get; }
    bool AttachScreenshot { get; }
    string EnvironmentId { get; }
}