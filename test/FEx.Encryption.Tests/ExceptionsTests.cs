using FEx.Agnostics.Exceptions;
using FEx.Encryption.Exceptions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// The two exception types are separate on purpose: a consumer catches
/// <see cref="FExDecryptionException" /> to tell a user their saved value cannot be read, and must not
/// swallow a missing-configuration bug in the same clause. These pin that separation and the plumbing
/// each constructor is expected to carry.
/// </summary>
public sealed class ExceptionsTests
{
    [Fact]
    public void NeitherTypeIsAssignableToTheOther()
    {
        typeof(FExDecryptionException).IsAssignableFrom(typeof(FExEncryptionNotConfiguredException)).ShouldBeFalse();
        typeof(FExEncryptionNotConfiguredException).IsAssignableFrom(typeof(FExDecryptionException)).ShouldBeFalse();
    }

    [Fact]
    public void BothDeriveFromTheFrameworkBase()
    {
        new FExDecryptionException().ShouldBeAssignableTo<FExException>();
        new FExEncryptionNotConfiguredException().ShouldBeAssignableTo<FExException>();
    }

    [Fact]
    public void DecryptionException_CarriesItsMessage() =>
        new FExDecryptionException("cannot read").Message.ShouldBe("cannot read");

    [Fact]
    public void DecryptionException_CarriesItsInnerException()
    {
        var inner = new InvalidOperationException("root");

        var exception = new FExDecryptionException("cannot read", inner);

        exception.Message.ShouldBe("cannot read");
        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void NotConfiguredException_CarriesItsMessage() =>
        new FExEncryptionNotConfiguredException("no passphrase").Message.ShouldBe("no passphrase");

    [Fact]
    public void NotConfiguredException_CarriesItsInnerException()
    {
        var inner = new InvalidOperationException("root");

        var exception = new FExEncryptionNotConfiguredException("no passphrase", inner);

        exception.Message.ShouldBe("no passphrase");
        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void ParameterlessConstructorsProduceUsableInstances()
    {
        new FExDecryptionException().Message.ShouldNotBeNullOrEmpty();
        new FExEncryptionNotConfiguredException().Message.ShouldNotBeNullOrEmpty();
    }
}
