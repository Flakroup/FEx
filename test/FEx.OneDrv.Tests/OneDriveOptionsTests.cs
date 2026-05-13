using FEx.OneDrv.Abstractions;
using Shouldly;
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
}
