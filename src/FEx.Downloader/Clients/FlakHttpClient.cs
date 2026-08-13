using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Utilities;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Clients;

public class FlakHttpClient : ProgressAggregator, IDownloadBase
{
    public string? FilePath => Client.FilePath;

    public Uri? Url => Client.Url;

    public string? DirPath => Client.DirPath;

    public DownloadState DState => Client.DState;

    public WebRequestParams? Pars
    {
        get => Client.Pars;
        set => Client.Pars = value;
    }

    protected HttpClientEx Client { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="pars">The <see cref="WebRequestParams" /> parameters for processing HTTP response messages.</param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public FlakHttpClient()
        : this(new WebRequestParams(), true, null)
    {
    }

    public FlakHttpClient(WebRequestParams pars)
        : this(pars, true, null)
    {
    }

    public FlakHttpClient(WebRequestParams pars, bool disposeHandler)
        : this(pars, disposeHandler, null)
    {
    }

    public FlakHttpClient(WebRequestParams pars, bool disposeHandler, CancellationTokenSource? cancellationTokenSource)
        : this(pars.GetHttpClientHandler(), disposeHandler, cancellationTokenSource)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="handler">
    /// The <see cref="HttpMessageHandler" /> responsible for processing the HTTP
    /// response messages.
    /// </param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public FlakHttpClient(HttpClientHandler handler)
        : this(handler, true, null)
    {
    }

    public FlakHttpClient(HttpClientHandler handler, bool disposeHandler)
        : this(handler, disposeHandler, null)
    {
    }

    public FlakHttpClient(HttpClientHandler handler,
                          bool disposeHandler,
                          CancellationTokenSource? cancellationTokenSource)
    {
        Mode = ProgressOperationMode.Stream;
        Client = new(handler, disposeHandler, cancellationTokenSource);
        Client.PropertyChanged += Client_PropertyChanged;
    }

    public int CompareTo(object? obj) => Client.CompareTo(obj);

    public int CompareTo(IDownloadBase? other) => Client.CompareTo(other);

    public bool Equals(IDownloadBase? other) => Client.Equals(other);

    public async Task DelayAsync() => await Client.DelayAsync();

    public async Task DoDownloadAsync(string filePath, HttpResponseMessage response) =>
        await Client.DoDownloadAsync(filePath, response, true);

    public async Task DoDownloadAsync(string filePath, HttpResponseMessage response, bool lockOnFilePath) =>
        await Client.DoDownloadAsync(filePath, response, lockOnFilePath);

    public async Task<HttpResponseMessage> GetAsync(Uri requestUri,
                                                    HttpCompletionOption completionOption,
                                                    CancellationToken cancellationToken) =>
        await Client.GetAsync(requestUri, completionOption, cancellationToken);

    private void Client_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Client.ProgressValue):
                Value = Client.ProgressValue;

                break;
            case nameof(Client.ProgressMaximum):
                Maximum = Client.ProgressMaximum;

                break;
        }
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Client?.Dispose();

        base.Dispose(disposing);
    }
    #endregion
}