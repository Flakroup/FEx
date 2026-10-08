using FEx.Telemetry.Subjects;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class TelemetryAccessTokenSubjectTests
{
    [Fact]
    public void Value_InitiallyDefault()
    {
        using var sut = new TelemetryAccessTokenSubject();

        sut.Value.ShouldBeNull();
    }

    [Fact]
    public void OnNext_UpdatesValueAndNotifiesSubscribers()
    {
        using var sut = new TelemetryAccessTokenSubject();
        var seen = new List<string>();
        using var subscription = sut.Subscribe(new Collector(seen));

        sut.OnNext("t1");
        sut.OnNext("t2");

        sut.Value.ShouldBe("t2");
        seen.ShouldBe([null!, "t1", "t2"]);
    }

    private sealed class Collector(List<string> sink) : IObserver<string>
    {
        public void OnNext(string value) => sink.Add(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
