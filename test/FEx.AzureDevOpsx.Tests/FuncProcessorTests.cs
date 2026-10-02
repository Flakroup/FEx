using FEx.AzureDevOpsx;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AzureDevOpsx.Tests;

[Collection(nameof(FuncProcessorTests))]
public sealed class FuncProcessorTests : IDisposable
{
    private sealed class JsonHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
    }

    private readonly Func<ICredentials, HttpMessageHandler> _originalFactory = FuncProcessor.HandlerFactory;

    public void Dispose() => FuncProcessor.HandlerFactory = _originalFactory;

    [Fact]
    public async Task RunRawAsync_ManyProcessorsSameCredentials_ShareOneHttpClient()
    {
        var created = 0;
        FuncProcessor.HandlerFactory = _ =>
        {
            Interlocked.Increment(ref created);

            return new JsonHandler();
        };
        var credentials = new NetworkCredential("user", "pw");

        for (var i = 0; i < 5; i++)
        {
            using var processor = new FuncProcessor("http://tfs.invalid/_apis/projects", credentials, "env");
            (await processor.RunRawAsync()).ShouldBe("{}");
        }

        created.ShouldBe(1);
    }
}
