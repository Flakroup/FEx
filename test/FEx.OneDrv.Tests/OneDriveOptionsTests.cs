using FEx.OneDrv.Abstractions;
using Shouldly;
using System;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class OneDriveOptionsTests
{
    [Fact]
    public void Defaults_TenantIsCommon_AndScopesIncludeFilesRead()
    {
        var options = new OneDriveOptions();

        options.TenantId.ShouldBe("common");
        options.Scopes.ShouldContain("Files.Read");
        options.Scopes.ShouldContain("User.Read");
    }

    [Fact]
    public void Validate_ValidOptions_DoesNotThrow() =>
        Should.NotThrow(() => new OneDriveOptions
        {
            ClientId = "x"
        }.Validate());

    [Fact]
    public void Validate_BlankScope_Throws() =>
        Should.Throw<ArgumentException>(() => new OneDriveOptions
        {
            ClientId = "x",
            Scopes = ["Files.Read", " "]
        }.Validate());
}
