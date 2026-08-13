using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FEx.AspNetCorex;

/// <summary>
/// A container HEALTHCHECK probe: hits the app's own <c>/health</c> endpoint over loopback and
/// reports success/failure as an exit code, so it can run before the host is built (no configuration
/// or database connection needed). The caller decides how to exit the process - this type never calls
/// <see cref="Environment.Exit" /> itself, so it stays unit-testable.
/// </summary>
public static class FExHealthcheckProbe
{
    /// <summary>True when <paramref name="args" /> is the single-argument <c>--healthcheck</c> probe invocation.</summary>
    public static bool IsProbe(string[] args) => args is ["--healthcheck"];

    /// <summary>Probes <paramref name="healthEndpoint" /> and returns the process exit code (0 = healthy, 1 = not).</summary>
    public static async Task<int> ProbeAsync(Uri healthEndpoint, TimeSpan timeout, HttpMessageHandler? handler = null)
    {
        using var client = handler is null
            ? new HttpClient()
            : new HttpClient(handler);

        client.Timeout = timeout;

        try
        {
            using var response = await client.GetAsync(healthEndpoint);

            return response.IsSuccessStatusCode
                ? 0
                : 1;
        }
        catch (Exception)
        {
            // Any failure (connection refused, timeout, non-success) means the app is not healthy.
            return 1;
        }
    }
}