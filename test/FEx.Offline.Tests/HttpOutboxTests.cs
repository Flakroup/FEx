using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Offline.Tests;

/// <summary>
/// Outbox: queued writes replay in order with their persisted idempotency key; a landed write
/// leaves the queue, a validation or permission rejection moves to the dead-letter list, a 401, a
/// network failure or a server error keeps everything and stops the flush, and a server error that
/// repeats until the limit is dead-lettered too.
/// </summary>
public sealed class HttpOutboxTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 15, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enqueue_Persists_AndCounts()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));

        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"firstName":"Jan"}""");

        (await outbox.CountAsync()).ShouldBe(1);
        var entry = (await outbox.ListAsync())[0];
        entry.Method.ShouldBe("POST");
        entry.Url.ShouldBe("api/sales/inquiries");
        entry.Attempts.ShouldBe(0);
    }

    [Fact]
    public async Task EnqueueWithExplicitId_PersistsThatKey_AndReplaysOnIt()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        Guid key = new("11111111-1111-1111-1111-111111111111");

        // The key a request already carried online: enqueuing under it lets the server dedup a landed-but-
        // lost write instead of executing it twice.
        var entry = await outbox.EnqueueAsync(key, "POST", "api/sales/contracts/5/payments", """{"n":1}""");
        entry.Id.ShouldBe(key);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Created);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        handler.IdempotencyKeys.ShouldBe(new() { key.ToString() });
    }

    [Fact]
    public async Task Flush_SendsInEnqueueOrder_WithThePersistedIdempotencyKey_AndEmptiesTheQueue()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        var first = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        var second = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Created);
        handler.EnqueueResponse(HttpStatusCode.Created);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(2, 0, 0, null));

        handler.RequestBodies.ShouldBe(new()
        {
            """{"n":1}""",
            """{"n":2}"""
        });

        handler.IdempotencyKeys.ShouldBe(new()
        {
            first.Id.ToString(),
            second.Id.ToString()
        });
    }

    [Fact]
    public async Task ValidationRejection_MovesTheEntryToTheDeadLetterList_AndReportsIt()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"bad":true}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest, """{"detail":"Ten klient jest już zapisany."}""");

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.DeadLettered.ShouldBe(1);
        result.Remaining.ShouldBe(0); // retrying an unchanged rejected write can never succeed
        result.LastError.ShouldBe("""HTTP 400: {"detail":"Ten klient jest już zapisany."}""");

        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.StatusCode.ShouldBe(400);
        dead.ResponseBody.ShouldBe("""{"detail":"Ten klient jest już zapisany."}""");
    }

    [Fact]
    public async Task PermissionRefusal_DeadLettersTheEntry_AndReportsItWithTheBody()
    {
        // A 403 is the server refusing this request for this user - no sign-in makes it land, so it must not
        // block the queue behind it the way a 401 does.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Forbidden, """{"detail":"Brak uprawnień."}""");
        handler.EnqueueResponse(HttpStatusCode.Created);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(1);
        result.DeadLettered.ShouldBe(1);
        result.Remaining.ShouldBe(0);
        result.LastError.ShouldBe("""HTTP 403: {"detail":"Brak uprawnień."}""");
        handler.Requests.Count.ShouldBe(2); // the flush went on past the refusal
    }

    [Theory]
    [InlineData("200")]
    [InlineData("201")]
    [InlineData("204")]
    public async Task ConflictForAWriteThatAlreadyCompleted_RemovesTheEntryAsDelivered_AndTheFlushGoesOn(string originalStatus)
    {
        // The server's idempotency layer answers 409 + the original 2xx when a write landed but its stored
        // response was too large to replay: the write is done, so it must not show up as a failed one.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Conflict, "Idempotency-Original-Status", originalStatus);
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(2, 0, 0, null));
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
        handler.Requests.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-201")]
    [InlineData("+201")]
    [InlineData("199")]
    [InlineData("300")]
    [InlineData("409")]
    [InlineData("500")]
    public async Task Conflict_WithoutA2xxOriginalStatus_StillMovesTheEntryToTheDeadLetterList(string? originalStatus)
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();

        if (originalStatus is null)
            handler.EnqueueResponse(HttpStatusCode.Conflict, "duplicate");
        else
            handler.EnqueueResponse(HttpStatusCode.Conflict, "Idempotency-Original-Status", originalStatus);

        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.DeadLettered.ShouldBe(1);
        result.Remaining.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe(409);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task OriginalStatusHeaderOnAnotherClientError_DoesNotMakeTheWriteDelivered(HttpStatusCode status)
    {
        // Only the idempotency layer's 409 says the write landed; the header on any other rejection (a proxy forwarding it, say) must not drop the write.
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(status, "Idempotency-Original-Status", "201");
        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.DeadLettered.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe((int)status);
    }

    [Fact]
    public async Task ConflictForAWriteThatAlreadyCompleted_LeavesNoStaleDeadCopy()
    {
        // The write was buried, removing the live entry failed, and the replay then found the write done.
        FailingFirstRemoveStore store = new();
        HttpOutbox outbox = new(store, new FixedTime(T0), maxServerFailures: 1);
        await outbox.EnqueueAsync("POST", "api/a", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.Conflict, "Idempotency-Original-Status", "201");
        using var http = Client(handler);

        await Should.ThrowAsync<IOException>(() => outbox.FlushAsync(http, TestContext.Current.CancellationToken));
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem();

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(1, 0, 0, null));
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionNotEnough_KeepsTheEntryWithTheError_AndStopsTheFlush()
    {
        // A 401 means sign in again: the unchanged write can land afterwards, so dropping it would lose it.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.DeadLettered.ShouldBe(0);
        result.Remaining.ShouldBe(2);
        result.LastError.ShouldBe("HTTP 401");
        handler.Requests.Count.ShouldBe(1); // the second entry was never attempted

        var kept = (await outbox.ListAsync())[0];
        kept.Attempts.ShouldBe(1);
        kept.LastError.ShouldBe("HTTP 401");
        kept.JsonBody.ShouldBe("""{"n":1}""");
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task TransientAnswer_KeepsTheEntryWithTheError_AndStopsTheFlush(HttpStatusCode status)
    {
        // 408, 429, 502, 503 and 504 are timeouts, throttling, a failing gateway and temporary overload: the unchanged
        // write can land later, so one answer drops nothing, and none of them counts as a server failure. All but the
        // 429 count as an unavailable answer, which has a limit of its own.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(status);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.DeadLettered.ShouldBe(0);
        result.Remaining.ShouldBe(2);
        result.LastError.ShouldBe($"HTTP {(int)status}");
        handler.Requests.Count.ShouldBe(1);

        var kept = (await outbox.ListAsync())[0];
        kept.Attempts.ShouldBe(1);
        kept.ServerFailures.ShouldBe(0);
        kept.UnavailableAnswers.ShouldBe(status == HttpStatusCode.TooManyRequests ? 0 : 1);
        kept.JsonBody.ShouldBe("""{"n":1}""");
    }

    [Fact]
    public async Task NetworkFailure_KeepsTheEntryWithTheError_AndStopsTheFlush()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueNetworkFailure();

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(0);
        result.Remaining.ShouldBe(2);
        handler.Requests.Count.ShouldBe(1); // the second entry was never attempted
        (await outbox.ListAsync())[0].Attempts.ShouldBe(1);
        (await outbox.ListAsync())[0].LastError.ShouldNotBeNull();
    }

    [Fact]
    public async Task ServerError_KeepsTheEntry_AndStopsTheFlush()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Remaining.ShouldBe(1);
        result.LastError.ShouldBe("HTTP 500");
    }

    [Fact]
    public async Task RetryAfterRecovery_SendsTheSameIdempotencyKey()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        var entry = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueNetworkFailure();
        handler.EnqueueResponse(HttpStatusCode.Created);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        var second = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        second.Sent.ShouldBe(1);
        second.Remaining.ShouldBe(0);

        // The key that makes the server-side replay safe: identical on every attempt.
        handler.IdempotencyKeys.ShouldBe(new()
        {
            entry.Id.ToString(),
            entry.Id.ToString()
        });
    }

    [Fact]
    public async Task EmptyOutbox_FlushIsANoOp()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        using ScriptedHandler handler = new();

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(0, 0, 0, null));
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Flush_StampsTheRegisteredReplayHeaders_OnEveryReplay_BodilessIncluded()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, null, ClientMarker());
        var withBody = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));

        // The shape a server that demands the marker most needs it on: a POST with no body at all.
        var bodiless = await outbox.EnqueueAsync("POST", "api/sales/participations/7/cancel", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.OK);
        handler.EnqueueResponse(HttpStatusCode.OK);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(2, 0, 0, null));
        handler.RequestHeaders.Select(h => h.GetValueOrDefault("X-Client")).ShouldBe(new[] { MarkerValue, MarkerValue });
        handler.RequestHeaders.Select(h => h.GetValueOrDefault("X-Second")).ShouldBe(new[] { SecondValue, SecondValue });

        // Registering a header must not displace the one the outbox sends itself.
        handler.IdempotencyKeys.ShouldBe(new()
        {
            withBody.Id.ToString(),
            bodiless.Id.ToString()
        });
    }

    [Fact]
    public async Task EntryParkedByAnOlderVersion_ReplaysWithTheRegisteredHeader()
    {
        // Stored exactly as 0.4.0-alpha.8 wrote it, which knew nothing of replay headers: the header can only
        // come from the outbox doing the replay, which is the whole reason it is not persisted per entry.
        InMemoryKeyValueStore store = new();
        Guid id = new("22222222-2222-2222-2222-222222222222");

        await store.SetAsync($"outbox:{T0.UtcTicks:D19}:{id:N}",
            $$"""{"id":"{{id}}","method":"POST","url":"api/sales/participations/7/cancel","jsonBody":null,"createdAtUtc":"2026-07-15T03:00:00+00:00","attempts":0,"lastError":null}""");

        HttpOutbox outbox = new(store, new FixedTime(T0), null, ClientMarker());

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.OK);

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(1, 0, 0, null));
        handler.RequestHeaders.Single()["X-Client"].ShouldBe(MarkerValue);
        handler.RequestHeaders.Single()["X-Second"].ShouldBe(SecondValue);
        handler.IdempotencyKeys.ShouldBe(new() { id.ToString() });
    }

    [Fact]
    public async Task ReplayHeaders_AreNeverWrittenToTheStore()
    {
        // Nothing a host registers lands in browser storage, so registering a header is never a way to
        // persist a value in the clear.
        InMemoryKeyValueStore store = new();
        HttpOutbox outbox = new(store, new FixedTime(T0), null, ClientMarker());

        await outbox.EnqueueAsync("POST", "api/sales/participations/7/cancel", null);

        var key = (await store.GetKeysAsync("outbox:")).Single();
        var stored = await store.GetAsync(key);
        stored.ShouldNotBeNull();
        stored.ShouldNotContain("x-client"); // Shouldly compares strings case-insensitively unless told otherwise
        stored.ShouldNotContain(MarkerValue);
    }

    [Theory]
    [InlineData("Idempotency-Key")]
    [InlineData("idempotency-key")]
    public void RegisteringTheIdempotencyHeader_IsRefused(string name) =>
        Should.Throw<ArgumentException>(() => Outbox(new() { [name] = "x" })).ParamName.ShouldBe("replayHeaders");

    [Fact]
    public void RegisteringAContentHeader_IsRefusedAtConstruction() =>
        Should.Throw<InvalidOperationException>(() => Outbox(new() { ["Content-Type"] = "application/json" }));

    [Theory]
    [InlineData("Not A Header", "x")]
    [InlineData("X-Client", "line\r\nbreak")]
    [InlineData("X-Client", "zażółć")]
    public void RegisteringAnInvalidHeader_IsRefusedAtConstruction(string name, string value) =>
        Should.Throw<FormatException>(() => Outbox(new() { [name] = value }));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisteringABlankValue_IsRefused(string value) =>
        Should.Throw<ArgumentException>(() => Outbox(new() { ["X-Client"] = value })).ParamName.ShouldBe("replayHeaders");

    [Fact]
    public void RegisteringNoDictionary_IsRefused() =>
        Should.Throw<ArgumentNullException>(() => Outbox(null!));

    [Fact]
    public async Task ClientRejection_StoresTheStatusAndATruncatedBody_AndNeverReplaysIt()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        var entry = await outbox.EnqueueAsync("PUT", "api/sales/inquiries/5", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(5));

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Conflict, new string('x', 500));
        using var http = Client(handler);

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.Id.ShouldBe(entry.Id);
        dead.Method.ShouldBe("PUT");
        dead.Url.ShouldBe("api/sales/inquiries/5");
        dead.JsonBody.ShouldBe("""{"n":1}""");
        dead.CreatedAtUtc.ShouldBe(T0);
        dead.DeadAtUtc.ShouldBe(T0.AddMinutes(5));
        dead.StatusCode.ShouldBe(409);
        dead.ResponseBody.ShouldBe(new string('x', 200));
        dead.ServerFailures.ShouldBe(0);

        // A second flush has nothing to send: the scripted handler would throw if the dead entry were replayed.
        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 0, null));
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ServerError_CountsOnTheEntry_AndStopsTheFlushUntilTheLimit()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxServerFailures: 3);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.NotImplemented);
        using var http = Client(handler);

        var first = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        var second = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        first.Remaining.ShouldBe(2);
        second.Remaining.ShouldBe(2);
        second.DeadLettered.ShouldBe(0);
        handler.Requests.Count.ShouldBe(2); // each flush stopped at the head; the second entry was never attempted

        var queue = await outbox.ListAsync();
        queue[0].ServerFailures.ShouldBe(2);
        queue[0].Attempts.ShouldBe(2);
        queue[1].ServerFailures.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported)]
    public async Task ServerError_AtTheLimit_MovesTheEntryToTheDeadLetterList_AndTheFlushGoesOn(HttpStatusCode status)
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxServerFailures: 2);
        var poisoned = await outbox.EnqueueAsync("POST", "api/sales/leads/7/trash", null);
        time.Advance(TimeSpan.FromMinutes(1));
        var behind = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(status, "boom");
        handler.EnqueueResponse(status, "boom again");
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(1);
        result.DeadLettered.ShouldBe(1);
        result.Remaining.ShouldBe(0);
        result.LastError.ShouldBe($"HTTP {(int)status}: boom again");

        handler.IdempotencyKeys.ShouldBe(new()
        {
            poisoned.Id.ToString(),
            poisoned.Id.ToString(),
            behind.Id.ToString()
        });

        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.Id.ShouldBe(poisoned.Id);
        dead.StatusCode.ShouldBe((int)status);
        dead.ResponseBody.ShouldBe("boom again");
        dead.ServerFailures.ShouldBe(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GatewayError_DoesNotCountTowardTheServerFailureLimit_SoAGatewayOutageCannotBuryAValidWrite(HttpStatusCode status)
    {
        // 502 and 504 say the proxy in front of the server failed, not that the server refused this write - like 503 they
        // stay out of the server-failure limit, so a short outage keeps the entry at the head of the queue.
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxServerFailures: 2);
        var entry = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();

        for (var i = 0; i < 5; i++)
            handler.EnqueueResponse(status);

        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        for (var i = 0; i < 5; i++)
            (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(0);

        var kept = (await outbox.ListAsync()).ShouldHaveSingleItem();
        kept.Id.ShouldBe(entry.Id);
        kept.Attempts.ShouldBe(5);
        kept.ServerFailures.ShouldBe(0);
        kept.UnavailableAnswers.ShouldBe(5);
        kept.LastError.ShouldBe($"HTTP {(int)status}");
        (await outbox.ListDeadAsync()).ShouldBeEmpty();

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task UnavailableAnswer_AtTheLimit_MovesTheEntryToTheDeadLetterList_AndTheFlushGoesOn(HttpStatusCode status)
    {
        // The #256 probe: a write the origin always answers 408/502/503/504 would otherwise sit at the head of the queue forever.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxUnavailableAnswers: 3);
        var stuck = await outbox.EnqueueAsync("POST", "api/sales/leads/7/trash", null);
        time.Advance(TimeSpan.FromMinutes(1));
        var behind = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(status, "first");
        handler.EnqueueResponse(status, "second");
        handler.EnqueueResponse(status, "last");
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(1);
        result.DeadLettered.ShouldBe(1);
        result.Remaining.ShouldBe(0);
        result.LastError.ShouldBe($"HTTP {(int)status}: last");

        handler.IdempotencyKeys.ShouldBe(new()
        {
            stuck.Id.ToString(),
            stuck.Id.ToString(),
            stuck.Id.ToString(),
            behind.Id.ToString()
        });

        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.Id.ShouldBe(stuck.Id);
        dead.StatusCode.ShouldBe((int)status);
        dead.ResponseBody.ShouldBe("last");
        dead.ServerFailures.ShouldBe(0);
    }

    [Fact]
    public async Task UnavailableAnswer_BelowTheLimit_KeepsTheEntryLive_AndBlocksTheQueue()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxUnavailableAnswers: 3);
        var head = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        using var http = Client(handler);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 2, "HTTP 503"));
        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 2, "HTTP 503"));

        handler.Requests.Count.ShouldBe(2); // each flush stopped at the head; the second entry was never attempted

        var queue = await outbox.ListAsync();
        queue[0].Id.ShouldBe(head.Id);
        queue[0].UnavailableAnswers.ShouldBe(2);
        queue[0].Attempts.ShouldBe(2);
        queue[1].UnavailableAnswers.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ServerError_CountsTowardTheServerFailureLimit_NotTheUnavailableOne()
    {
        // The unavailable limit is 1 here, so a 500 that counted on it would bury the entry on the first answer.
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxServerFailures: 3, maxUnavailableAnswers: 1);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        using var http = Client(handler);

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        var kept = (await outbox.ListAsync()).ShouldHaveSingleItem();
        kept.ServerFailures.ShouldBe(2);
        kept.UnavailableAnswers.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
    }

    [Fact]
    public async Task SessionThrottlingAndTransportAnswers_NeverCountTowardTheUnavailableLimit()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxUnavailableAnswers: 1);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized);
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueNetworkFailure();
        handler.EnqueueTimeout();
        using var http = Client(handler);

        for (var i = 0; i < 4; i++)
            (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(0);

        var kept = (await outbox.ListAsync()).ShouldHaveSingleItem();
        kept.Attempts.ShouldBe(4);
        kept.UnavailableAnswers.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task UnavailableAnswers_SurviveARestart()
    {
        InMemoryKeyValueStore store = new();
        FixedTime time = new(T0);
        HttpOutbox before = new(store, time, maxUnavailableAnswers: 3);
        await before.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.GatewayTimeout);
        handler.EnqueueResponse(HttpStatusCode.GatewayTimeout);
        handler.EnqueueResponse(HttpStatusCode.GatewayTimeout);
        using var http = Client(handler);

        await before.FlushAsync(http, TestContext.Current.CancellationToken);
        await before.FlushAsync(http, TestContext.Current.CancellationToken);

        // A new outbox over the same store: the count comes from the stored entry, not from the instance.
        HttpOutbox after = new(store, time, maxUnavailableAnswers: 3);

        (await after.ListAsync()).ShouldHaveSingleItem().UnavailableAnswers.ShouldBe(2);
        (await after.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
        (await after.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe(504);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TheDefaultLimit_IsTen(bool withReplayHeaders)
    {
        var outbox = DefaultOutbox(withReplayHeaders);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();

        for (var i = 0; i < 10; i++)
            handler.EnqueueResponse(HttpStatusCode.InternalServerError);

        using var http = Client(handler);

        for (var i = 0; i < 9; i++)
            (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(0);

        (await outbox.ListAsync()).ShouldHaveSingleItem().ServerFailures.ShouldBe(9);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().ServerFailures.ShouldBe(10);
        HttpOutbox.DefaultMaxServerFailures.ShouldBe(10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TheDefaultUnavailableLimit_IsAHundred(bool withReplayHeaders)
    {
        var outbox = DefaultOutbox(withReplayHeaders);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();

        for (var i = 0; i < 100; i++)
            handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        using var http = Client(handler);

        for (var i = 0; i < 99; i++)
            (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(0);

        (await outbox.ListAsync()).ShouldHaveSingleItem().UnavailableAnswers.ShouldBe(99);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe(503);
        HttpOutbox.DefaultMaxUnavailableAnswers.ShouldBe(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ALimitBelowOne_IsRefused(int limit)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new HttpOutbox(new InMemoryKeyValueStore(), new FixedTime(T0), maxServerFailures: limit))
            .ParamName.ShouldBe("maxServerFailures");

        Should.Throw<ArgumentOutOfRangeException>(() => new HttpOutbox(new InMemoryKeyValueStore(), new FixedTime(T0), null, ClientMarker(), limit))
            .ParamName.ShouldBe("maxServerFailures");
    }

    [Theory]
    [InlineData(HttpStatusCode.Found, "/login", "GET /login", false)]
    [InlineData(HttpStatusCode.Found, "/login", "GET /login", true)]
    [InlineData(HttpStatusCode.TemporaryRedirect, "/api/elsewhere", "POST /api/elsewhere", false)]
    [InlineData(HttpStatusCode.SeeOther, "/api/payments", "GET /api/payments", false)]
    public async Task AnAnswerAfterAFollowedRedirect_KeepsTheEntry_CountsItAsUnavailable_AndStopsTheFlush(
        HttpStatusCode redirect, string location, string finalRequest, bool absoluteUrl)
    {
        // A 302 turns the POST into a GET of the login page, whose 200 used to remove the write as delivered; a 307 keeps the
        // method and changes only the URI; a 303 back to the same URI changes only the method. Either way the final request
        // is not the queued one.
        using var server = RedirectingServer(redirect, location);
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", absoluteUrl ? $"{server.BaseAddress}api/payments" : "api/payments", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/ok", """{"n":2}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(0, 0, 2, "HTTP 200 after a redirect"));
        server.Requests.ShouldBe(["POST /api/payments", finalRequest]); // the second entry was never attempted

        var kept = (await outbox.ListAsync())[0];
        kept.UnavailableAnswers.ShouldBe(1);
        kept.Attempts.ShouldBe(1);
        kept.ServerFailures.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AFollowedRedirect_AtTheUnavailableLimit_MovesTheEntryToTheDeadLetterList_AndTheFlushGoesOn()
    {
        using var server = RedirectingServer(HttpStatusCode.Found, "/login");
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxUnavailableAnswers: 2);
        var redirected = await outbox.EnqueueAsync("POST", "api/payments", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/ok", """{"n":2}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).Remaining.ShouldBe(2);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(1, 1, 0, "HTTP 200: login page"));

        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.Id.ShouldBe(redirected.Id);
        dead.StatusCode.ShouldBe(200);
        dead.ResponseBody.ShouldBe("login page");
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ARedirectThatEndsInA4xx_KeepsTheEntry_AndIsNotTakenForARejection(HttpStatusCode landing)
    {
        // The 4xx is the redirect target's answer (a login page that is gone), not the server judging the write: the entry
        // must not be dead-lettered on it.
        using RawHttpServer server = new(request => request switch
        {
            "POST /api/payments" => (302, "/login", ""),
            "GET /login" => ((int)landing, null, "nope"),
            _ => (200, null, "ok")
        });
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/payments", """{"n":1}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 1, $"HTTP {(int)landing} after a redirect"));
        (await outbox.ListAsync()).ShouldHaveSingleItem().UnavailableAnswers.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ARedirectLoop_IsCounted_AndDeadLetteredAtTheUnavailableLimit()
    {
        // The client gives up after 50 hops and hands back the last 307, whose method and URI equal the queued ones.
        using RawHttpServer server = new(request => request == "POST /api/payments" ? (307, "/api/payments", "") : (200, null, "ok"));
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxUnavailableAnswers: 2);
        await outbox.EnqueueAsync("POST", "api/payments", """{"n":1}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).Remaining.ShouldBe(1);
        (await outbox.ListAsync()).ShouldHaveSingleItem().UnavailableAnswers.ShouldBe(1);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe(307);
    }

    [Fact]
    public async Task ARedirectTheClientDidNotFollow_IsCountedAsUnavailable_NotLeftToBlockTheQueue()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxUnavailableAnswers: 2);
        await outbox.EnqueueAsync("POST", "api/a", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Found);
        handler.EnqueueResponse(HttpStatusCode.Found);
        using var http = Client(handler);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 1, "HTTP 302 after a redirect"));
        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().StatusCode.ShouldBe(302);
    }

    [Fact]
    public async Task EveryFlushedRequest_AsksTheBrowserFetchNotToFollowRedirects()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/a", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("PUT", "api/b", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Created);
        handler.EnqueueResponse(HttpStatusCode.OK);
        using var http = Client(handler);

        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        handler.Requests.Count.ShouldBe(2);
        foreach (var request in handler.Requests)
        {
            request.Options.TryGetValue(new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions"), out var options).ShouldBeTrue();
            options.ShouldBe(new Dictionary<string, object> { ["redirect"] = "manual" });
        }
    }

    [Fact]
    public async Task AnOpaqueRedirectFromTheBrowser_KeepsTheEntry_CountsItAsUnavailable_AndStopsTheFlush()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/a", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueOpaqueRedirect();
        using var http = Client(handler);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 2, "HTTP 0 after a redirect"));

        handler.Requests.Count.ShouldBe(1);
        var first = (await outbox.ListAsync())[0];
        first.UnavailableAnswers.ShouldBe(1);
        first.Attempts.ShouldBe(1);
        first.LastError.ShouldBe("HTTP 0 after a redirect");
    }

    [Fact]
    public async Task AnOpaqueRedirectFromTheBrowser_AtTheUnavailableLimit_MovesTheEntryToTheDeadLetterList_AndTheFlushGoesOn()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxUnavailableAnswers: 2);
        var redirected = await outbox.EnqueueAsync("POST", "api/a", """{"n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueOpaqueRedirect();
        handler.EnqueueOpaqueRedirect();
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).Remaining.ShouldBe(2);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.DeadLettered.ShouldBe(1);
        result.Sent.ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(redirected.Id);
    }

    [Fact]
    public async Task AnEntryWithNoUrl_IsDeliveredToTheBaseAddress_NotTakenForARedirect()
    {
        // HttpClient fills the request's missing URI from BaseAddress while sending, so the queued URI must be that address too.
        using RawHttpServer server = new(_ => (200, null, "ok"));
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "", """{"n":1}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        server.Requests.ShouldBe(["POST /"]);
    }

    [Fact]
    public async Task AHandlerThatAddsAQueryStringKey_DoesNotMakeADelivered2xxLookRedirected()
    {
        // An API-key handler rewrites RequestUri on every request; the final request is still the queued write.
        using var server = RedirectingServer(HttpStatusCode.Found, "/login");
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/ok", """{"n":1}""");

        using var http = new HttpClient(new RewritingHandler(uri => new UriBuilder(uri) { Query = "api_key=k" }.Uri)) { BaseAddress = server.BaseAddress };

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        (await outbox.ListAsync()).ShouldBeEmpty();
        server.Requests.ShouldHaveSingleItem().ShouldStartWith("POST /api/ok");
    }

    [Fact]
    public async Task AHandlerThatChangesTheHost_DoesNotMakeADelivered2xxLookRedirected()
    {
        // Hedging and routing handlers point the request at another endpoint than the queued URL names.
        using var server = RedirectingServer(HttpStatusCode.Found, "/login");
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "https://primary.invalid/api/ok", """{"n":1}""");

        using var http = new HttpClient(new RewritingHandler(uri => new UriBuilder(uri) { Scheme = server.BaseAddress.Scheme, Host = server.BaseAddress.Host, Port = server.BaseAddress.Port }.Uri));

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        (await outbox.ListAsync()).ShouldBeEmpty();
        server.Requests.ShouldBe(["POST /api/ok"]);
    }

    [Fact]
    public async Task AHandlerThatRewritesThePath_IsStillTakenForARedirect()
    {
        // The documented ceiling of the redirect check: a path change cannot be told from a followed redirect.
        using var server = RedirectingServer(HttpStatusCode.Found, "/login");
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/ok", """{"n":1}""");

        using var http = new HttpClient(new RewritingHandler(uri => new UriBuilder(uri) { Path = "/api/v2/ok" }.Uri)) { BaseAddress = server.BaseAddress };

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 1, "HTTP 200 after a redirect"));
    }

    [Fact]
    public async Task AnAnswerWithNoRedirect_StillDeliversTheEntry_OverARealClient()
    {
        using var server = RedirectingServer(HttpStatusCode.Found, "/login");
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/ok", """{"n":1}""");

        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = server.BaseAddress }; // no proxy: a machine-wide one must not swallow loopback

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        server.Requests.ShouldBe(["POST /api/ok"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AnUnavailableLimitBelowOne_IsRefused(int limit)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new HttpOutbox(new InMemoryKeyValueStore(), new FixedTime(T0), maxUnavailableAnswers: limit))
            .ParamName.ShouldBe("maxUnavailableAnswers");

        Should.Throw<ArgumentOutOfRangeException>(() => new HttpOutbox(new InMemoryKeyValueStore(), new FixedTime(T0), null, ClientMarker(), 10, limit))
            .ParamName.ShouldBe("maxUnavailableAnswers");
    }

    [Fact]
    public async Task OnlyServerAnswersCount_AWriteSurvivesSixtyTransientFailures_AndLandsOnTheNext2xx()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        var entry = await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();

        for (var i = 0; i < 10; i++)
        {
            handler.EnqueueNetworkFailure();
            handler.EnqueueTimeout();
            handler.EnqueueResponse(HttpStatusCode.Unauthorized);
            handler.EnqueueResponse(HttpStatusCode.RequestTimeout);
            handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
            handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        }

        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        for (var i = 0; i < 60; i++)
            (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(0);

        var kept = (await outbox.ListAsync()).ShouldHaveSingleItem();
        kept.Id.ShouldBe(entry.Id);
        kept.Attempts.ShouldBe(60);
        kept.ServerFailures.ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
    }

    [Fact]
    public async Task TransientAnswers_KeepTheServerFailuresAlreadyCounted()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0), maxServerFailures: 3);
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"n":1}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        handler.EnqueueNetworkFailure();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        using var http = Client(handler);

        for (var i = 0; i < 5; i++)
            await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        (await outbox.ListAsync()).ShouldHaveSingleItem().ServerFailures.ShouldBe(2);

        (await outbox.FlushAsync(http, TestContext.Current.CancellationToken)).DeadLettered.ShouldBe(1);
    }

    [Fact]
    public async Task DeadEntries_SurviveARestart_AreNeverMixedIntoTheQueue_AndAreNeverReplayed()
    {
        InMemoryKeyValueStore store = new();
        FixedTime time = new(T0);
        HttpOutbox before = new(store, time);
        var dead = await before.EnqueueAsync("POST", "api/sales/inquiries", """{"bad":true}""");
        time.Advance(TimeSpan.FromMinutes(1));
        var live = await before.EnqueueAsync("POST", "api/sales/inquiries", """{"n":2}""");

        using ScriptedHandler first = new();
        first.EnqueueResponse(HttpStatusCode.UnprocessableEntity);
        first.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        using var firstHttp = Client(first);
        await before.FlushAsync(firstHttp, TestContext.Current.CancellationToken);

        HttpOutbox after = new(store, time);

        (await after.ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(dead.Id);
        (await after.ListAsync()).ShouldHaveSingleItem().Id.ShouldBe(live.Id);
        (await after.CountAsync()).ShouldBe(1);

        using ScriptedHandler second = new();
        second.EnqueueResponse(HttpStatusCode.Created);
        using var secondHttp = Client(second);

        (await after.FlushAsync(secondHttp, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        second.IdempotencyKeys.ShouldBe(new() { live.Id.ToString() });
        (await after.ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(dead.Id);
    }

    [Fact]
    public async Task RemoveDead_DeletesOnlyTheEntryWithThatId()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        var first = await outbox.EnqueueAsync("POST", "api/a", null);
        time.Advance(TimeSpan.FromMinutes(1));
        var second = await outbox.EnqueueAsync("POST", "api/b", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest);
        handler.EnqueueResponse(HttpStatusCode.BadRequest);
        using var http = Client(handler);
        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        (await outbox.ListDeadAsync()).Select(d => d.Id).ShouldBe(new[] { first.Id, second.Id });

        (await outbox.RemoveDeadAsync(first.Id)).ShouldBeTrue();
        (await outbox.RemoveDeadAsync(first.Id)).ShouldBeFalse();
        (await outbox.RemoveDeadAsync(Guid.NewGuid())).ShouldBeFalse();

        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(second.Id);
    }

    [Fact]
    public async Task ClearDead_DeletesEveryDeadEntry_AndLeavesTheLiveOnesToFlush()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/a", """{"secret":"a"}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", """{"secret":"b"}""");

        using ScriptedHandler rejecting = new();
        rejecting.EnqueueResponse(HttpStatusCode.BadRequest);
        rejecting.EnqueueResponse(HttpStatusCode.BadRequest);
        using var rejectingHttp = Client(rejecting);
        await outbox.FlushAsync(rejectingHttp, TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(1));
        var liveOne = await outbox.EnqueueAsync("POST", "api/c", null);
        time.Advance(TimeSpan.FromMinutes(1));
        var liveTwo = await outbox.EnqueueAsync("POST", "api/d", null);
        (await outbox.ListDeadAsync()).Count.ShouldBe(2);

        (await outbox.ClearDeadAsync(TestContext.Current.CancellationToken)).ShouldBe(2);

        (await outbox.ListDeadAsync()).ShouldBeEmpty();
        (await outbox.ListAsync()).Select(e => e.Id).ShouldBe(new[] { liveOne.Id, liveTwo.Id });

        using ScriptedHandler accepting = new();
        accepting.EnqueueResponse(HttpStatusCode.Created);
        accepting.EnqueueResponse(HttpStatusCode.Created);
        using var acceptingHttp = Client(accepting);

        (await outbox.FlushAsync(acceptingHttp, TestContext.Current.CancellationToken)).ShouldBe(new(2, 0, 0, null));
        accepting.IdempotencyKeys.ShouldBe(new() { liveOne.Id.ToString(), liveTwo.Id.ToString() });
    }

    [Fact]
    public async Task ClearDead_OnAnEmptyDeadList_ReturnsZero_AndKeepsTheLiveEntries()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/a", null);

        (await outbox.ClearDeadAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        (await outbox.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SignOut_ClearAndClearDead_LeaveNothingForTheNextUsersFlushToSend()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        await outbox.EnqueueAsync("POST", "api/rejected", """{"email":"alice@example.com","n":1}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/profile", """{"email":"alice@example.com","n":2}""");

        // User A's flush: the first write is rejected (dead), the second fails on transport and stays live.
        using ScriptedHandler userA = new();
        userA.EnqueueResponse(HttpStatusCode.BadRequest);
        userA.EnqueueNetworkFailure();
        using var userAHttp = Client(userA);
        (await outbox.FlushAsync(userAHttp, TestContext.Current.CancellationToken)).ShouldBe(new(0, 1, 1, "connection refused"));

        (await outbox.ClearAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await outbox.ClearDeadAsync(TestContext.Current.CancellationToken)).ShouldBe(1);

        // User B signs in; the host flushes with B's token.
        using ScriptedHandler userB = new();
        using var userBHttp = Client(userB);
        userBHttp.DefaultRequestHeaders.Authorization = new("Bearer", "token-of-user-b");

        (await outbox.FlushAsync(userBHttp, TestContext.Current.CancellationToken)).ShouldBe(new(0, 0, 0, null));
        userB.Requests.ShouldBeEmpty();
        (await outbox.ListAsync()).ShouldBeEmpty();
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Clear_DeletesEveryLiveEntry_AndLeavesTheDeadOnes()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        var rejected = await outbox.EnqueueAsync("POST", "api/a", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest);
        using var http = Client(handler);
        await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", null);
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/c", null);

        (await outbox.ClearAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
        (await outbox.ClearAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        (await outbox.CountAsync()).ShouldBe(0);
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(rejected.Id);
    }

    [Fact]
    public async Task ClearDead_StartedDuringAFlush_WaitsForIt_AndDeletesTheEntryItDeadLetters()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/profile", """{"email":"alice@example.com"}""");

        using GatedHandler handler = new();
        using var http = Gated(handler);
        var flush = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await handler.Entered;

        var clear = outbox.ClearDeadAsync(TestContext.Current.CancellationToken);
        clear.IsCompleted.ShouldBeFalse();

        handler.Answer(HttpStatusCode.BadRequest);

        (await flush).DeadLettered.ShouldBe(1);
        (await clear).ShouldBe(1);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Clear_StartedDuringAFlush_WaitsForIt_AndDeletesTheEntryItKeeps()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/profile", """{"email":"alice@example.com"}""");

        using GatedHandler handler = new();
        using var http = Gated(handler);
        var flush = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await handler.Entered;

        var clear = outbox.ClearAsync(TestContext.Current.CancellationToken);
        clear.IsCompleted.ShouldBeFalse();

        handler.FailOnTransport();

        (await flush).Remaining.ShouldBe(1);
        (await clear).ShouldBe(1);
        (await outbox.ListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task TwoConcurrentFlushes_SendTheEntryOnce()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/profile", null);

        using GatedHandler handler = new();
        using var http = Gated(handler);
        var first = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await handler.Entered;

        var second = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        second.IsCompleted.ShouldBeFalse();

        handler.Answer(HttpStatusCode.Created);

        (await first).ShouldBe(new(1, 0, 0, null));
        (await second).ShouldBe(new(0, 0, 0, null));
        handler.Requests.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ACancelledClear_WaitingBehindAFlush_Throws_LeavesTheFlushAlone_AndLeavesTheGateUsable(bool clearDead)
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/profile", null);

        using GatedHandler handler = new();
        using var http = Gated(handler);
        var flush = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await handler.Entered;

        using CancellationTokenSource cts = new();
        var clear = clearDead ? outbox.ClearDeadAsync(cts.Token) : outbox.ClearAsync(cts.Token);
        clear.IsCompleted.ShouldBeFalse();

        await cts.CancelAsync();

        // Cancelled while the flush still holds the gate. The bound only turns a clear that ignores the token into a failure
        // instead of a hang. (Answering first would race: SemaphoreSlim hands the gate to a waiter whose cancellation has not
        // been processed yet.)
        await Should.ThrowAsync<OperationCanceledException>(() => clear.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        handler.Answer(HttpStatusCode.Created);
        (await flush).ShouldBe(new(1, 0, 0, null));

        // The cancelled wait released nothing: the gate still admits a clear (and the flush's own release did not throw).
        await outbox.EnqueueAsync("POST", "api/profile", null);
        (await (clearDead ? outbox.ClearDeadAsync(TestContext.Current.CancellationToken) : outbox.ClearAsync(TestContext.Current.CancellationToken))).ShouldBe(clearDead ? 0 : 1);
    }

    [Fact]
    public async Task ACancelledFlush_WaitingBehindAnotherFlush_Throws_SendsNothing_AndLeavesTheGateUsable()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/profile", null);

        using GatedHandler handler = new();
        using var http = Gated(handler);
        var first = outbox.FlushAsync(http, TestContext.Current.CancellationToken);
        await handler.Entered;

        using CancellationTokenSource cts = new();
        var second = outbox.FlushAsync(http, cts.Token);
        second.IsCompleted.ShouldBeFalse();

        await cts.CancelAsync();

        // Cancelled while the first flush still holds the gate. The bound only turns a flush that ignores the token into a
        // failure instead of a hang. (Answering first would race: SemaphoreSlim hands the gate to a waiter whose cancellation
        // has not been processed yet.)
        await Should.ThrowAsync<OperationCanceledException>(() => second.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        handler.Answer(HttpStatusCode.Created);
        (await first).ShouldBe(new(1, 0, 0, null));
        handler.Requests.ShouldBe(1);

        // The cancelled wait released nothing: the gate still admits a flush (and the first flush's own release did not throw).
        await outbox.EnqueueAsync("POST", "api/profile", null);
        using GatedHandler later = new();
        using var laterHttp = Gated(later);
        var third = outbox.FlushAsync(laterHttp, TestContext.Current.CancellationToken);
        await later.Entered;
        later.Answer(HttpStatusCode.Created);
        (await third).ShouldBe(new(1, 0, 0, null));
    }

    [Fact]
    public async Task AFlushCancelledBetweenEntries_StopsThere_AndLeavesTheNextEntryQueued()
    {
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time);
        var first = await outbox.EnqueueAsync("POST", "api/a", null);
        time.Advance(TimeSpan.FromMinutes(1));
        var second = await outbox.EnqueueAsync("POST", "api/b", null);

        using CancellationTokenSource cts = new();
        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.Created, cts.Cancel);
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        await Should.ThrowAsync<OperationCanceledException>(() => outbox.FlushAsync(http, cts.Token));

        handler.IdempotencyKeys.ShouldBe(new() { first.Id.ToString() });
        (await outbox.ListAsync()).ShouldHaveSingleItem().ShouldBe(second);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AFlushCancelledWhileSending_LeavesThatEntryQueued_WithItsCountersUnchanged()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        var entry = await outbox.EnqueueAsync("POST", "api/profile", """{"n":1}""");

        using CancellationTokenSource cts = new();
        using GatedHandler handler = new();
        using var http = Gated(handler);
        var flush = outbox.FlushAsync(http, cts.Token);
        await handler.Entered;

        await cts.CancelAsync();

        // The bound only turns a send that ignores the token into a failure instead of a hang.
        await Should.ThrowAsync<OperationCanceledException>(() => flush.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        (await outbox.ListAsync()).ShouldHaveSingleItem().ShouldBe(entry);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();

        // The next flush replays it with the same idempotency key.
        using ScriptedHandler replay = new();
        replay.EnqueueResponse(HttpStatusCode.Created);
        using var replayHttp = Client(replay);

        (await outbox.FlushAsync(replayHttp, TestContext.Current.CancellationToken)).ShouldBe(new(1, 0, 0, null));
        replay.IdempotencyKeys.ShouldBe(new() { entry.Id.ToString() });
    }

    private static HttpClient Gated(GatedHandler handler) => new(handler)
    {
        BaseAddress = new("http://localhost/")
    };

    /// <summary>Holds every request until the test answers, so a flush can be caught in flight without sleeping.</summary>
    private sealed class GatedHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Func<HttpResponseMessage>> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requests;

        public Task Entered => _entered.Task;

        public int Requests => Volatile.Read(ref _requests);

        public void Answer(HttpStatusCode status) => _answer.SetResult(() => new(status)
        {
            Content = new StringContent("answer")
        });

        public void FailOnTransport() => _answer.SetResult(() => throw new HttpRequestException("connection refused"));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            _entered.TrySetResult();

#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            return (await _answer.Task.WaitAsync(cancellationToken))();
#pragma warning restore VSTHRD003
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task ABodyThatCannotBeRead_StillDeadLettersTheEntry_AndDoesNotBlockTheQueue(HttpStatusCode status)
    {
        // An undecodable charset makes ReadAsStringAsync throw; that must not escape the flush and pin the head entry.
        FixedTime time = new(T0);
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), time, maxServerFailures: 1);
        await outbox.EnqueueAsync("POST", "api/a", null);
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", null);

        using ScriptedHandler handler = new();
        handler.EnqueueUnreadableResponse(status);
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.Sent.ShouldBe(1);
        result.DeadLettered.ShouldBe(1);
        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.StatusCode.ShouldBe((int)status);
        dead.ResponseBody.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFailedDeadLetterWrite_LeavesTheWriteInTheQueue()
    {
        // The dead copy lands before the live entry goes, so a store that fails the first write loses nothing.
        HttpOutbox outbox = new(new DeadWriteFailingStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/a", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest);
        using var http = Client(handler);

        await Should.ThrowAsync<IOException>(() => outbox.FlushAsync(http, TestContext.Current.CancellationToken));

        (await outbox.ListAsync()).ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task AnEntryAlreadyPastALoweredLimit_IsNotDeadLetteredByAnAnswerThatNeverCounts(HttpStatusCode status)
    {
        // Stored with 5 server failures, then the host restarts with a limit of 3: a 401 is still only a session to
        // renew and a 502, 503 or 504 only a failing gateway or an overloaded server.
        InMemoryKeyValueStore store = new();
        Guid id = new("44444444-4444-4444-4444-444444444444");

        await store.SetAsync($"outbox:{T0.UtcTicks:D19}:{id:N}",
            $$"""{"id":"{{id}}","method":"POST","url":"api/a","jsonBody":null,"createdAtUtc":"2026-07-15T03:00:00+00:00","attempts":5,"lastError":"HTTP 500","serverFailures":5}""");

        HttpOutbox outbox = new(store, new FixedTime(T0), maxServerFailures: 3);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(status);
        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.DeadLettered.ShouldBe(0);
        (await outbox.ListAsync()).ShouldHaveSingleItem().ServerFailures.ShouldBe(5);
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AWriteBuriedBeforeAFailedRemoval_ThatLandsOnTheNextFlush_LeavesNoDeadCopy()
    {
        FailingFirstRemoveStore store = new();
        HttpOutbox outbox = new(store, new FixedTime(T0), maxServerFailures: 1);
        await outbox.EnqueueAsync("POST", "api/a", null);

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError);
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        // The dead copy is written, then removing the live entry fails.
        await Should.ThrowAsync<IOException>(() => outbox.FlushAsync(http, TestContext.Current.CancellationToken));
        (await outbox.ListDeadAsync()).ShouldHaveSingleItem();

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(1, 0, 0, null));
        (await outbox.ListDeadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ConsumerJsonOptionsWithoutDeadOutboxEntry_StillDeadLetterTheEntry_AndDoNotBlockTheQueue()
    {
        // A source-generated resolver that lists OutboxEntry only: serializing DeadOutboxEntry through it throws
        // NotSupportedException, which used to escape the flush before the live entry was removed - on every flush.
        InMemoryKeyValueStore store = new();
        FixedTime time = new(T0);
        JsonSerializerOptions consumerOptions = new()
        {
            TypeInfoResolver = OutboxEntryOnlyJsonContext.Default
        };

        HttpOutbox outbox = new(store, time, consumerOptions);
        var rejected = await outbox.EnqueueAsync("POST", "api/a", """{"bad":true}""");
        time.Advance(TimeSpan.FromMinutes(1));
        await outbox.EnqueueAsync("POST", "api/b", """{"n":2}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest, "no way");
        handler.EnqueueResponse(HttpStatusCode.Created);
        using var http = Client(handler);

        var result = await outbox.FlushAsync(http, TestContext.Current.CancellationToken);

        result.ShouldBe(new(1, 1, 0, "HTTP 400: no way"));
        (await outbox.CountAsync()).ShouldBe(0);

        // The dead entry is readable through the same outbox and through one with other options: FEx owns its format.
        var dead = (await outbox.ListDeadAsync()).ShouldHaveSingleItem();
        dead.Id.ShouldBe(rejected.Id);
        dead.StatusCode.ShouldBe(400);
        dead.ResponseBody.ShouldBe("no way");
        (await new HttpOutbox(store, time).ListDeadAsync()).ShouldHaveSingleItem().Id.ShouldBe(rejected.Id);
    }

    [Fact]
    public async Task EntryParkedByAnOlderVersion_ListsWithNoServerFailures()
    {
        // Written before ServerFailures existed: the missing property reads as zero, so the entry gets its full limit.
        InMemoryKeyValueStore store = new();
        Guid id = new("33333333-3333-3333-3333-333333333333");

        await store.SetAsync($"outbox:{T0.UtcTicks:D19}:{id:N}",
            $$"""{"id":"{{id}}","method":"POST","url":"api/a","jsonBody":null,"createdAtUtc":"2026-07-15T03:00:00+00:00","attempts":4,"lastError":"HTTP 500"}""");

        var entry = (await new HttpOutbox(store, new FixedTime(T0)).ListAsync()).ShouldHaveSingleItem();

        entry.Attempts.ShouldBe(4);
        entry.ServerFailures.ShouldBe(0);
        entry.UnavailableAnswers.ShouldBe(0);
    }

    [Fact]
    public async Task DeadEntryInTheStoredFormat_ListsEveryField()
    {
        // The stored dead-entry format is FEx's own (camelCase) and read case-sensitively: pin it, so a naming change cannot silently blank entries already stored.
        InMemoryKeyValueStore store = new();
        Guid id = new("44444444-4444-4444-4444-444444444444");

        await store.SetAsync($"outbox-dead:{T0.UtcTicks:D19}:{id:N}",
            $$"""{"id":"{{id}}","method":"POST","url":"api/a","jsonBody":"{\"n\":1}","createdAtUtc":"2026-07-15T03:00:00+00:00","deadAtUtc":"2026-07-15T04:00:00+00:00","serverFailures":3,"statusCode":422,"responseBody":"no"}""");

        var dead = (await new HttpOutbox(store, new FixedTime(T0)).ListDeadAsync()).ShouldHaveSingleItem();

        dead.ShouldBe(new(id, "POST", "api/a", """{"n":1}""", new(2026, 7, 15, 3, 0, 0, TimeSpan.Zero),
            new(2026, 7, 15, 4, 0, 0, TimeSpan.Zero), 3, 422, "no"));
    }

    private static HttpClient Client(ScriptedHandler handler) => new(handler)
    {
        BaseAddress = new("http://localhost/")
    };

    private const string MarkerValue = "fex-offline-test-client";
    private const string SecondValue = "fex-offline-second-header";

    // Two headers, so a replay that stamps only the first one registered cannot pass.
    private static Dictionary<string, string> ClientMarker() => new()
    {
        ["X-Client"] = MarkerValue,
        ["X-Second"] = SecondValue
    };

    /// <summary>A pipeline handler that rewrites the request's URI before the real client sends it.</summary>
    private sealed class RewritingHandler(Func<Uri, Uri> rewrite) : DelegatingHandler(new SocketsHttpHandler { UseProxy = false }) // no proxy: a machine-wide one must not swallow loopback
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = rewrite(request.RequestUri!);

            return base.SendAsync(request, cancellationToken);
        }
    }

    // POST /api/payments answers a redirect; the login page, the redirect target and /api/ok answer 200.
    private static RawHttpServer RedirectingServer(HttpStatusCode redirect, string location) => new(request => request switch
    {
        "POST /api/payments" => ((int)redirect, location, ""),
        "GET /login" => (200, null, "login page"),
        _ => (200, null, "ok")
    });

    // The default limits through the constructor without replay headers, or the one with them.
    private static HttpOutbox DefaultOutbox(bool withReplayHeaders) =>
        withReplayHeaders
            ? Outbox(ClientMarker())
            : new(new InMemoryKeyValueStore(), new FixedTime(T0));

    private static HttpOutbox Outbox(Dictionary<string, string> replayHeaders) =>
        new(new InMemoryKeyValueStore(), new FixedTime(T0), null, replayHeaders);
}

/// <summary>A consumer's source-generated context that knows the queue's entry type but not the dead-letter one.</summary>
[JsonSerializable(typeof(OutboxEntry))]
internal sealed partial class OutboxEntryOnlyJsonContext : JsonSerializerContext;
