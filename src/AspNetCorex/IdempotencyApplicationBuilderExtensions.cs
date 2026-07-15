using Microsoft.AspNetCore.Builder;

namespace FEx.AspNetCorex;

public static class IdempotencyApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="IdempotencyMiddleware" />. Place it after authentication (the replay cache is
    /// scoped per user) and register <c>AddMemoryCache()</c>.
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app) =>
        app.UseMiddleware<IdempotencyMiddleware>();
}