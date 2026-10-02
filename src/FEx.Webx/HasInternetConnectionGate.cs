using FEx.Core.Abstractions.Interfaces;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Webx;

public class HasInternetConnectionGate
{
    private readonly IExceptionHandler _exceptionHandler;

    private static readonly HttpClient SharedHttpClient = new();

    private readonly HttpClient _httpClient;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    public HasInternetConnectionGate(IExceptionHandler exceptionHandler)
        : this(exceptionHandler, SharedHttpClient)
    {
    }

    public HasInternetConnectionGate(IExceptionHandler exceptionHandler, HttpClient httpClient)
    {
        _exceptionHandler = exceptionHandler;
        _httpClient = httpClient;
    }

    public Task<bool> CheckAsync() => CheckAsync(null, CancellationToken.None);

    public Task<bool> CheckAsync(CancellationToken cancellationToken) => CheckAsync(null, cancellationToken);

    /// <summary>
    /// Probes <paramref name="url" /> (a connectivity-check endpoint by default). Returns false when the probe
    /// fails, is cancelled via <paramref name="cancellationToken" />, or takes longer than <see cref="Timeout" />.
    /// </summary>
    public async Task<bool> CheckAsync(Uri? url, CancellationToken cancellationToken = default)
    {
        url ??= new("http://clients3.google.com/generate_204");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            return true;
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex);

            return false;
        }
    }
}