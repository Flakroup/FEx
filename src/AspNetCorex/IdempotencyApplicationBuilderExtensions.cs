using Microsoft.AspNetCore.Builder;

namespace FEx.AspNetCorex;

public static class IdempotencyApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="IdempotencyMiddleware" />. Place it after authentication (the stored responses are
    /// keyed per user) and call <c>AddIdempotency()</c> - or register your own
    /// <see cref="Abstractions.IIdempotencyStore" /> - so it has somewhere to keep them.
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app) =>
        app.UseMiddleware<IdempotencyMiddleware>();
}