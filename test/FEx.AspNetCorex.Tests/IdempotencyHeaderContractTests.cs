using FEx.Offline;
using Shouldly;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>
/// <see cref="HttpOutbox" /> cannot reference ASP.NET Core, so it keeps its own copy of the
/// idempotency header name. This pins the two copies equal - renaming one without the other would
/// silently break replay between an offline outbox and this middleware.
/// </summary>
public sealed class IdempotencyHeaderContractTests
{
    [Fact]
    public void OutboxHeaderName_MatchesTheMiddlewareHeaderName() =>
        HttpOutbox.IdempotencyHeader.ShouldBe(IdempotencyMiddleware.HeaderName);
}