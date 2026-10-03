using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

    /// <summary>
    /// The probes send the copy, so a setting the copy drops (credentials, headers, user agent ...) silently turns
    /// the probe anonymous. Every public property is populated by reflection, so a property added later without
    /// being copied turns this red.
    /// </summary>
    [Fact]
    public void Copy_CarriesEverySettingOfTheCallersParams()
    {
        var source = new WebRequestParams
        {
            Credentials = new NetworkCredential("user", "secret"),
            UserAgent = "fex-tests",
            Headers = new Dictionary<string, string> { ["X-Custom"] = "1" },
            Cookies = new(),
            Method = "POST",
            Timeout = 1234,
            Pipelined = true,
            KeepAlive = true,
            ReadWriteTimeout = 5678,
            Proxy = new WebProxy("http://proxy.invalid"),
            IsProxyNull = true,
            ServerCertificateValidationCallback = (_, _, _, _) => true
        };

        var properties = typeof(WebRequestParams).GetProperties().Where(x => x.CanWrite).ToArray();

        foreach (var property in properties)
            property.GetValue(source).ShouldNotBeNull($"{property.Name} must be populated above");

        var copy = UriExtensions.Copy(source);

        copy.ShouldNotBeSameAs(source);

        foreach (var property in properties)
            property.GetValue(copy).ShouldBe(property.GetValue(source), property.Name);
    }
}
