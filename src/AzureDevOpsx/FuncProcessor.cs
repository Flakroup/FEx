using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.AzureDevOpsx.Responses;
using FEx.AzureDevOpsx.Services;
using FEx.Flurlx.Models;
using FEx.Json.Extensions;
using Flurl.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AzureDevOpsx;

public class FuncProcessor
{
    public static ConcurrentDictionary<string, SemaphoreSlim> RateLimits { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
    public static string ExpectedContentType { get; } = "application/json";

    public RequestMethod Method { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    private string RequestUrl { get; }
    private IDictionary<string, object> Args { get; }
    private CancellationToken CancellationToken { get; }
    private ICredentials Credentials { get; }

    public FuncProcessor(string serverUrl, string scriptPath, ICredentials credentials, string environmentId, IDictionary<string, object> args = null, RequestMethod method = RequestMethod.GET)
        : this(serverUrl + scriptPath, credentials, environmentId, args, method)
    {
    }

    public FuncProcessor(string serverUrl, string scriptPath, ICredentials credentials, IDictionary<string, object> args = null, RequestMethod method = RequestMethod.GET)
        : this(serverUrl + scriptPath, credentials, TfsService.GetEnvironmentId(serverUrl, credentials), args, method)
    {
    }

    public FuncProcessor(string requestUrl, ICredentials credentials, IDictionary<string, object> args = null, RequestMethod method = RequestMethod.GET)
        : this(requestUrl, credentials, TfsService.GetEnvironmentId(requestUrl, credentials), args, method)
    {
    }

    public FuncProcessor(string requestUrl, ICredentials credentials, string environmentId, IDictionary<string, object> args = null, RequestMethod method = RequestMethod.GET)
    {
        if (requestUrl.IsNullOrWhiteSpace()
            || environmentId.IsNullOrWhiteSpace())
            throw new ArgumentNullException(requestUrl.IsNullOrWhiteSpace()
                ? nameof(requestUrl)
                : nameof(environmentId));

        RequestUrl = requestUrl;
        Args = args;
        Method = method;
        CancellationTokenSource = new CancellationTokenSource();
        CancellationToken = CancellationTokenSource.Token;
        Credentials = credentials;
    }

    public static TResponse ProcessResponse<TResponse>(string response, JsonSerializerSettings settings = null) where TResponse : BaseTfsResponse, new()
    {
        return response.FromJson<TResponse>(settings);
    }

    public async Task<TResponse> RunAsync<TResponse>(JsonSerializerSettings settings = null, IList<HttpStatusCode> ommitCodes = null) where TResponse : BaseTfsResponse, new()
    {
        string res = await RunRawAsync(ommitCodes);
        return res != null
            ? ProcessResponse<TResponse>(res, settings)
            : default;
    }

    public async Task<string> RunRawAsync(IList<HttpStatusCode> ommitCodes = null)
    {
        using var handler = new HttpClientHandler { Credentials = Credentials };
        using var httpClient = new HttpClient(handler);
        using var flurlClient = new FlurlClient(httpClient);

        var request = flurlClient.Request(RequestUrl);

        if (Args != null)
            foreach (var arg in Args)
                request = request.SetQueryParam(arg.Key, arg.Value);

        request = request
            .WithHeader("User-Agent", "VSTSAuthSample-AuthenticateADALNonInteractive")
            .WithHeader("X-TFS-FedAuthRedirect", "Suppress");

        try
        {
            IFlurlResponse response;
            switch (Method)
            {
                case RequestMethod.POST:
                    response = await request.PostAsync(cancellationToken: CancellationToken);
                    break;
                case RequestMethod.PUT:
                    response = await request.PutAsync(cancellationToken: CancellationToken);
                    break;
                case RequestMethod.DELETE:
                    response = await request.DeleteAsync(cancellationToken: CancellationToken);
                    break;
                case RequestMethod.PATCH:
                    response = await request.PatchAsync(cancellationToken: CancellationToken);
                    break;
                default:
                    response = await request.GetAsync(cancellationToken: CancellationToken);
                    break;
            }

            using (response)
            {
                var statusCode = (HttpStatusCode)response.StatusCode;
                if (ommitCodes.IsNullOrEmptyList() || !ommitCodes.Contains(statusCode))
                {
                    string content = await response.GetStringAsync();
                    string contentType = response.Headers.FirstOrDefault("Content-Type");

                    if (contentType == null || contentType.Split(';')[0].Trim() == ExpectedContentType)
                        return content;

                    throw new ArgumentException($"Unexpected response type. {contentType} instead of {ExpectedContentType}. Response was of {statusCode} status.");
                }
            }
        }
        catch (FlurlHttpException ex)
        {
            var statusCode = (HttpStatusCode)(ex.StatusCode ?? 0);
            if (ommitCodes != null && ommitCodes.Contains(statusCode))
                return null;
            throw;
        }

        return null;
    }
}
