using FEx.Core.Extensions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Core.Tests.Extensions;

// The error handler logs through FExStaticLogger, which other tests swap.
[Collection(StaticStateCollection.Name)]
public sealed class JsonExtensionsTests
{
    [Fact]
    public void SafeSerializeObject_SkipsThrowingMemberInsteadOfThrowing()
    {
        string json = null!;

        Should.NotThrow(() => json = new Faulty().SafeSerializeObject());

        json.ShouldContain("Name");
        json.ShouldContain("ok");
    }

    private sealed class Faulty
    {
        public string Name { get; } = "ok";

        public string Boom => throw new InvalidOperationException("cannot read");
    }
}
