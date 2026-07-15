using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Offline.Tests;

/// <summary>
/// Outbox: queued writes replay in order with their persisted idempotency key; a landed write
/// leaves the queue, a validation rejection leaves it too (and is reported), a network failure
/// keeps everything and stops the flush.
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
        result.LastError.ShouldNotBeNull();
        result.LastError.ShouldContain("400");
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
}