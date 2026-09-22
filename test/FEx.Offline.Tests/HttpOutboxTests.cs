using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Offline.Tests;

/// <summary>
/// Outbox: queued writes replay in order with their persisted idempotency key; a landed write
/// leaves the queue, a validation or permission rejection leaves it too (and is reported), a 401, a
/// network failure or a server error keeps everything and stops the flush.
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

        await outbox.FlushAsync(http);

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

        var result = await outbox.FlushAsync(http);

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
    public async Task ValidationRejection_DropsTheEntry_AndReportsIt()
    {
        HttpOutbox outbox = new(new InMemoryKeyValueStore(), new FixedTime(T0));
        await outbox.EnqueueAsync("POST", "api/sales/inquiries", """{"bad":true}""");

        using ScriptedHandler handler = new();
        handler.EnqueueResponse(HttpStatusCode.BadRequest, """{"detail":"Ten klient jest już zapisany."}""");

        using HttpClient http = new(handler)
        {
            BaseAddress = new("http://localhost/")
        };

        var result = await outbox.FlushAsync(http);

        result.Sent.ShouldBe(0);
        result.Rejected.ShouldBe(1);
        result.Remaining.ShouldBe(0); // retrying an unchanged rejected write can never succeed
        result.LastError.ShouldBe("""HTTP 400: {"detail":"Ten klient jest już zapisany."}""");
    }

    [Fact]
    public async Task PermissionRefusal_DropsTheEntry_AndReportsItWithTheBody()
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

        var result = await outbox.FlushAsync(http);

        result.Sent.ShouldBe(1);
        result.Rejected.ShouldBe(1);
        result.Remaining.ShouldBe(0);
        result.LastError.ShouldBe("""HTTP 403: {"detail":"Brak uprawnień."}""");
        handler.Requests.Count.ShouldBe(2); // the flush went on past the refusal
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

        var result = await outbox.FlushAsync(http);

        result.Sent.ShouldBe(0);
        result.Rejected.ShouldBe(0);
        result.Remaining.ShouldBe(2);
        result.LastError.ShouldBe("HTTP 401");
        handler.Requests.Count.ShouldBe(1); // the second entry was never attempted

        var kept = (await outbox.ListAsync())[0];
        kept.Attempts.ShouldBe(1);
        kept.LastError.ShouldBe("HTTP 401");
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

        var result = await outbox.FlushAsync(http);

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

        var result = await outbox.FlushAsync(http);

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

        await outbox.FlushAsync(http);
        var second = await outbox.FlushAsync(http);

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

        var result = await outbox.FlushAsync(http);

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

        var result = await outbox.FlushAsync(http);

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

        var result = await outbox.FlushAsync(http);

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

    private const string MarkerValue = "fex-offline-test-client";
    private const string SecondValue = "fex-offline-second-header";

    // Two headers, so a replay that stamps only the first one registered cannot pass.
    private static Dictionary<string, string> ClientMarker() => new()
    {
        ["X-Client"] = MarkerValue,
        ["X-Second"] = SecondValue
    };

    private static HttpOutbox Outbox(Dictionary<string, string> replayHeaders) =>
        new(new InMemoryKeyValueStore(), new FixedTime(T0), null, replayHeaders);
}