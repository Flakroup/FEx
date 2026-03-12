using FEx.Agnostics.Abstractions.Extensions;
using FEx.AzureDevOpsx.Responses;
using FEx.Core.Collections.Concurrent;
using FEx.Flurlx.Models;
using FEx.Legacy.Mvvm.ViewModels;
using FEx.MVVM.Abstractions.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FEx.AzureDevOpsx.Services;

public sealed class TfsService : ThreadingAwareViewModel
{
    private TfsEnvironment _selectedEnvironment;

    public static string ImagesCacheDirPath { get; set; }

    public EventHandler<EventArgs> EnvironmentSet { get; set; }
    public EventHandler<EventArgs> TfsUriChange { get; set; }

    public ConcurrentObservableList<TfsEnvironment> TfsEnvironments { get; }

    public TfsEnvironment SelectedEnvironment
    {
        get => _selectedEnvironment;
        set
        {
            var isSet = false;
            Dispatcher.InvokeOnIdleMainThread(() =>
            {
                if (_selectedEnvironment?.Name != value?.Name
                    || _selectedEnvironment?.ServerUri != value?.ServerUri)
                {
                    if (SelectedEnvironment != null)
                        SelectedEnvironment.SuccessfullyAuthenticated -= OnUriChanged;

                    if (SetProperty(ref _selectedEnvironment, value))
                    {
                        if (SelectedEnvironment != null)
                            SelectedEnvironment.SuccessfullyAuthenticated += OnUriChanged;

                        isSet = true;
                    }
                }
            });
            if (isSet)
                SelectedEnvironmentHasChanged?.Invoke(_selectedEnvironment, EventArgs.Empty);
        }
    }

    private EventHandler<EventArgs> SelectedEnvironmentHasChanged { get; }

    public static void SetEnvironments(IDictionary<string, Uri> environments, IProgressAggregator mainViewModel)
    {
        Instance.TfsEnvironments.Clear();
        Instance.TfsEnvironments.AddRange(environments.Select(x => new TfsEnvironment(x.Key, x.Value, mainViewModel, ImagesCacheDirPath)));
    }

    public static string GetEnvironmentId(string requestUrl, ICredentials credentials)
    {
        return Instance.TfsEnvironments.FindInEnumerable(x => x.GetCredentials() == credentials && requestUrl.StartsWith(x.Server.Uri.AbsoluteUri))
            ?.EnvironmentId;
    }

    public Task<TResponse> RunProcAsync<TResponse>(string requestUrl, string environmentId, IDictionary<string, object> args = null, IList<HttpStatusCode> ommitCodes = null, JsonSerializerSettings settings = null, RequestMethod method = RequestMethod.GET) where TResponse : BaseTfsResponse, new()
    {
        TfsEnvironment env = TfsEnvironments.First(x => x.EnvironmentId == environmentId);
        return env.RunProcAsync<TResponse>(requestUrl, args, settings, ommitCodes, method);
    }

    public Task<string> RunRawAsync(string requestUrl, string environmentId, IDictionary<string, object> args = null, IList<HttpStatusCode> ommitCodes = null, RequestMethod method = RequestMethod.GET)
    {
        TfsEnvironment env = TfsEnvironments.First(x => x.EnvironmentId == environmentId);
        return env.RunRawAsync(requestUrl, args, ommitCodes, method);
    }

    private void OnSelectedEnvironmentHasChanged(object sender, EventArgs eventArgs)
    {
        if (SelectedEnvironment != null)
            EnvironmentSet.Invoke(_selectedEnvironment, EventArgs.Empty);
    }

    private void OnUriChanged(object sender, EventArgs e)
    {
        TfsUriChange.Invoke(null, null);
    }

    #region Singleton

    private static volatile TfsService _instance;
    private static object SyncRoot { get; } = new object();

    public static TfsService Instance
    {
        get
        {
            if (_instance == null)
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new TfsService();
                }

            return _instance;
        }
    }

    private TfsService()
    {
        TfsEnvironments = [];
        TfsEnvironments.CollectionChanged += TfsEnvironments_CollectionChanged;
        SelectedEnvironmentHasChanged += OnSelectedEnvironmentHasChanged;
    }

    private void TfsEnvironments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedEnvironment == null
            || TfsEnvironments.All(x => x.ServerUri != SelectedEnvironment.ServerUri))
            SelectedEnvironment = TfsEnvironments.FindInEnumerable();
    }

    #endregion
}
