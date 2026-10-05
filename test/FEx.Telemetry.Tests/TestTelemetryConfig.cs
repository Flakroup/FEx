namespace FEx.Telemetry.Tests;

internal sealed class TestTelemetryConfig : FExTelemetryConfigBase
{
    public string? LastToken { get; private set; }
    public int TokenChangeCount { get; private set; }

    public string EnvironmentParamValue => EnvironmentParam;

    protected override void OnAccessTokenChanged(string accessToken)
    {
        LastToken = accessToken;
        TokenChangeCount++;
    }
}
