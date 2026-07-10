using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Extensions;
using FEx.Flurlx.Models;
using Flurl.Http;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Flurlx.Services;

/// <summary>
/// Abstract base class for building typed API clients with Polly resilience.
/// </summary>
/// <remarks>
/// Provides HTTP request/response handling with built-in resilience patterns (Retry, Circuit Breaker, Timeout, etc.).
/// Doesn't require <c>Initialize();</c> call in .ctor
/// </remarks>
public abstract class FlurlApiBase : AsyncInitializable
{
#pragma warning disable IDISP006 // Implement IDisposable - done by FlurlCache
    protected IFlurlClient FlurlClient { get; }
#pragma warning restore IDISP006 // Implement IDisposable
    protected IAsyncPolicy<IFlurlResponse> ResiliencePolicy { get; }

    // No dependency on AsyncInitializable parent (empty dependency set) - implicit base ctor
    protected FlurlApiBase(IFlurlConfigurator flurlConfigurator)
    {
        FlurlClient = flurlConfigurator.Guard(nameof(flurlConfigurator)).GetClient();
        ResiliencePolicy = flurlConfigurator.GetResiliencePolicy();

        BeginInitialization();
    }

    private static readonly JsonSerializerOptions PrettyPrintOptions = new() { WriteIndented = true };

    protected static string GetStatusMessage(HttpStatusCode statusCode) => statusCode.ToString();

    protected static bool IsSuccess(IFlurlResponse response) => response.IsSuccessStatusCode();

    /// <summary>
    /// Indents <paramref name="content"/> when it is valid JSON; returns it unchanged otherwise
    /// (e.g. a plain-text error body). Total - never throws on malformed input.
    /// </summary>
    private static string PrettyPrintOrRaw(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        try
        {
            using var document = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(document.RootElement, PrettyPrintOptions);
        }
        catch (JsonException)
        {
            return content;
        }
    }

    protected virtual async Task<T> GetResponseAsync<T, TReq>(string apiPath,
                                                              Func<IFlurlRequest, IFlurlRequest>? func = null,
                                                              RequestMethod method = RequestMethod.GET,
                                                              TReq requestContent = default!, // unconstrained generic default (null for reference TReq)
                                                              CancellationToken cancellationToken = default)
    {
        var req = FlurlClient.Request(apiPath);

        if (func is not null)
            req = func(req).FixBooleanQueryParameters();

        req = AddConstantsToRequest(req);

        var responseUri = req.Url.ToUri();

        // Execute with Polly resilience
#pragma warning disable IDE0063
        // ReSharper disable ConvertToUsingDeclaration
        using (var httpResponse = await ResiliencePolicy.ExecuteAsync(async ct =>
                       // ReSharper restore ConvertToUsingDeclaration
#pragma warning restore IDE0063
                       method switch
                       {
                           RequestMethod.GET => await req.GetAsync(cancellationToken: ct),
                           RequestMethod.POST => await req.PostJsonAsync(requestContent, cancellationToken: ct),
                           RequestMethod.PUT => await req.PutJsonAsync(requestContent, cancellationToken: ct),
                           RequestMethod.DELETE => await req.DeleteAsync(cancellationToken: ct),
                           RequestMethod.PATCH => await req.PatchJsonAsync(requestContent, cancellationToken: ct),
                           RequestMethod.HEAD => await req.HeadAsync(cancellationToken: ct),
                           RequestMethod.OPTIONS => await req.OptionsAsync(cancellationToken: ct),
                           _ => throw new NotImplementedException($"{method} is not implemented")
                       },
                   cancellationToken))
        {
            var content = await httpResponse.GetStringAsync();

            if (!httpResponse.IsSuccessStatusCode())
            {
                var statusCode = (HttpStatusCode)httpResponse.StatusCode;
#if NET5_0_OR_GREATER
                throw new HttpRequestException(
                    $"Request {method} {responseUri} has failed. {GetStatusMessage(statusCode)}{Environment.NewLine}{PrettyPrintOrRaw(content)}",
                    null,
                    statusCode);
#else
                throw new HttpRequestException(
                    $"Request {method} {responseUri} has failed. {GetStatusMessage(statusCode)}{Environment.NewLine}{PrettyPrintOrRaw(content)}");
#endif
            }

            var res = FlurlClient.Settings.JsonSerializer.Deserialize<T>(content);

            return res;
        }
    }

    protected virtual IFlurlRequest AddConstantsToRequest(IFlurlRequest req) => req;

    protected async Task<T> GetResponseAsync<T>(string apiPath,
                                                Func<IFlurlRequest, IFlurlRequest>? func = null,
                                                RequestMethod method = RequestMethod.GET,
                                                CancellationToken cancellationToken = default) =>
        await GetResponseAsync<T, T>(apiPath, func, method, cancellationToken: cancellationToken);
}