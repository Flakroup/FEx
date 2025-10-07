using FEx.Abstractions.Models;
using FEx.Downloader.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using FEx.Extensions.Web;
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
    public string FilePath => Client.FilePath;

    public Uri Url => Client.Url;

    public string DirPath => Client.DirPath;

    public DownloadState DState => Client.DState;

    public WebRequestParams Pars
    {
        get => Client.Pars;
        set => Client.Pars = value;
    }

    protected HttpClientEx Client { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="pars">The <see cref="T:WebRequestParams" /> parameters for processing HTTP response messages.</param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public FlakHttpClient(WebRequestParams pars = null,
                          bool disposeHandler = true,
                          CancellationTokenSource cancellationTokenSource = default)
        : this(pars.GetHttpClientHandler(), disposeHandler, cancellationTokenSource)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:FlakHttpClient" /> class with a specific handler.
    /// </summary>
    /// <param name="handler">
    /// The <see cref="T:System.Net.Http.HttpMessageHandler" /> responsible for processing the HTTP
    /// response messages.
    /// </param>
    /// <param name="disposeHandler">
    /// <see langword="true" /> if the inner handler should be disposed of by Dispose(),
    /// <see langword="false" /> if you intend to reuse the inner handler.
    /// </param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    public FlakHttpClient(HttpClientHandler handler,
                          bool disposeHandler = true,
                          CancellationTokenSource cancellationTokenSource = default)
    {
        Mode = ProgressOperationMode.Stream;
        Client = new(handler, disposeHandler, cancellationTokenSource);
        Client.PropertyChanged += Client_PropertyChanged;
    }

    public int CompareTo(object obj) => Client.CompareTo(obj);

    public int CompareTo(IDownloadBase other) => Client.CompareTo(other);

    public bool Equals(IDownloadBase other) => Client.Equals(other);

    public async Task DelayAsync() => await Client.DelayAsync();

    public async Task DoDownloadAsync(string filePath, HttpResponseMessage response, bool lockOnFilePath = true) =>
        await Client.DoDownloadAsync(filePath, response, lockOnFilePath);

    public async Task<HttpResponseMessage> GetAsync(Uri requestUri,
                                                    HttpCompletionOption completionOption,
                                                    CancellationToken cancellationToken) =>
        await Client.GetAsync(requestUri, completionOption, cancellationToken);

    private void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
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