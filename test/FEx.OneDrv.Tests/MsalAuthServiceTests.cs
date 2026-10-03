using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class MsalAuthServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_MissingClientId_ThrowsClearArgumentException(string? clientId)
    {
        var options = new OneDriveOptions
        {
            ClientId = clientId!
        };

        var ex = Should.Throw<ArgumentException>(() => new MsalAuthService(options, Substitute.For<IFExLogger>()));
        ex.Message.ShouldContain("ClientId");
    }

    [Fact]
    public void Constructor_EmptyScopes_Throws() =>
        Should.Throw<ArgumentException>(() => new MsalAuthService(new()
            {
                ClientId = "x",
                Scopes = []
            },
            Substitute.For<IFExLogger>()));

    [Fact(Skip = "TODO: token acquisition (silent -> interactive fallback, sign-out) needs a seam over IPublicClientApplication.")]
    public void GetAccessTokenAsync_SilentFailure_FallsBackToInteractive()
    {
    }
}
