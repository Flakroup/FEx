using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Core.Tests.Extensions;

/// <summary>
/// The probes set their own method and timeout; the caller's params may be reused for a later request and must come
/// back untouched. A file URI keeps these off the network: the params are prepared before the scheme is looked at.
/// </summary>
public sealed class UriExtensionsTests
{
    [Fact]
    public async Task IsUriReachableAsync_DoesNotChangeTheCallersParams()
    {
        var path = Path.GetTempFileName();

        try
        {
            var pars = new WebRequestParams();

            var (isAvailable, _) = await new Uri(path).IsUriReachableAsync(pars);

            isAvailable.ShouldBeTrue();
            pars.Method.ShouldBeNull();
            pars.Timeout.ShouldBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CheckIfLinkIsExpiredAsync_DoesNotChangeTheCallersParams()
    {
        var pars = new WebRequestParams { Method = "GET", Timeout = 1234 };

        await new Uri(Path.Combine(Path.GetTempPath(), "fex-missing-file")).CheckIfLinkIsExpiredAsync(pars);

        pars.Method.ShouldBe("GET");
        pars.Timeout.ShouldBe(1234);
    }
}
