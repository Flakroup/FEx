using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.BaseObjects;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Asyncx.Extensions;
using FEx.AzureDevOpsx.Extensions;
using FEx.AzureDevOpsx.Responses;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.Flurlx.Models;
using FEx.Legacy.Web;
using FEx.MVVM.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Utilities;
using FEx.Webx.Utilities;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using WindowsCredential = Microsoft.VisualStudio.Services.Common.WindowsCredential;

namespace FEx.AzureDevOpsx;

public class TfsEnvironment : NotifyPropertyChanged
{
    private static readonly string _lockKey = $"{nameof(TfsEnvironment)}_{Guid.NewGuid()}";
    private string _name;
    private string _version;
    private Uri _serverUri;
    private string _userName;
    private string _password;
    private ConcurrentList<ProjectsCollection> _projectsCollections;
    private bool _projectsCollectionsIsDirty;
    private bool _projectsCollectionsIsBusy;
    private TfsConfigurationServer _server;
    private bool _isConnecting;
    private AuthenticatedUserInfo _userInfo;

    public string EnvironmentId { get; }
    public IProgressAggregator ProgressViewModel { get; }
    public string ImagesCacheDirPath { get; }
    public string DisplayName => $"{Name} TFS ver. {Version}{Environment.NewLine}{ServerUri}";
    public bool LoadingProjectsCollections => ProjectsCollectionsIsBusy || ProjectsCollectionsIsDirty;
    public EventHandler<EventArgs> SuccessfullyAuthenticated { get; set; }

    public string Name
    {
        get => _name;
        private set
        {
            if (SetProperty(ref _name, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Version
    {
        get => _version;
        private set
        {
            if (SetProperty(ref _version, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    public TfsConfigurationServer Server
    {
        get => _server;
        private set => SetProperty(ref _server, value);
    }

    /// <summary>
    /// Gets or sets the server URI.
    /// </summary>
    /// <value>
    /// The server URI.
    /// </value>
    public Uri ServerUri
    {
        get => _serverUri;
        private set
        {
            LockSrv.RunLocked(() =>
                {
                    if (SetProperty(ref _serverUri, value))
                        OnPropertyChanged(nameof(DisplayName));
                },
                _lockKey);
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set => SetProperty(ref _isConnecting, value);
    }

    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    /// <value>
    /// The name of the user.
    /// </value>
    public string UserName
    {
        get => _userName;
        private set { LockSrv.RunLocked(() => SetProperty(ref _userName, value), _lockKey); }
    }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    /// <value>
    /// The password.
    /// </value>
    public string Password
    {
        get => _password;
        private set { LockSrv.RunLocked(() => SetProperty(ref _password, value), _lockKey); }
    }

    public AuthenticatedUserInfo UserInfo
    {
        get => _userInfo;
        private set => SetProperty(ref _userInfo, value);
    }

    public ConcurrentList<ProjectsCollection> ProjectsCollections
    {
        get => GetProjectsCollections();
        private set => SetProperty(ref _projectsCollections, value);
    }

    private static ISynchronizedAccessService LockSrv => FExCoreStatics.SynchronizedAccessService;

    private SemaphoreSlim EnvironmentLock { get; }
    private SemaphoreSlim CollectionsLock { get; }
    private ConcurrentHashSet<string> CollectionsNames { get; }
    private ReadOnlyCollection<CatalogNode> CollectionNodesCache { get; set; }

    private bool ProjectsCollectionsIsDirty
    {
        get => _projectsCollectionsIsDirty;
        set
        {
            LockSrv.RunLocked(() =>
                {
                    if (SetProperty(ref _projectsCollectionsIsDirty, value))
                        OnPropertyChanged(nameof(LoadingProjectsCollections));
                },
                _lockKey);
        }
    }

    private bool ProjectsCollectionsIsBusy
    {
        get => _projectsCollectionsIsBusy;
        set
        {
            LockSrv.RunLocked(() =>
                {
                    if (SetProperty(ref _projectsCollectionsIsBusy, value))
                        OnPropertyChanged(nameof(LoadingProjectsCollections));
                },
                _lockKey);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TfsEnvironment" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="address">The address.</param>
    /// <param name="viewModel">The view model.</param>
    /// <param name="imagesCacheDirPath">The images cache dir path.</param>
    public TfsEnvironment(string name, Uri address, IProgressAggregator viewModel, string imagesCacheDirPath)
    {
        EnvironmentId = Guid.NewGuid().ToString();
        ImagesCacheDirPath = imagesCacheDirPath;
        ProgressViewModel = viewModel;
        EnvironmentLock = new(1, 1);
        ProjectsCollections = [];
        Name = name;
        ServerUri = address;
        CollectionsNames = [];
        CollectionsLock = new(1, 1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TfsEnvironment" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="address">The address.</param>
    /// <param name="viewModel">The view model.</param>
    /// <param name="imagesCacheDirPath">The images cache dir path.</param>
    public TfsEnvironment(string name, string address, IProgressAggregator viewModel, string imagesCacheDirPath)
        : this(name, new Uri(address), viewModel, imagesCacheDirPath)
    {
    }

    public async Task<bool> InitializeAsync(bool autoLogIn = true, string username = null, string password = null)
    {
        IsConnecting = true;
        ProjectsCollectionsIsBusy = false;
        ProjectsCollectionsIsDirty = true;
        _projectsCollections.Clear();
        var res = await GetServerAsync(autoLogIn, username, password);
        IsConnecting = false;

        return res;
    }

    /// <summary>
    /// Gets the server.
    /// </summary>
    /// <returns>True if successfull</returns>
    public async Task<bool> GetServerAsync(bool autoLogIn = true, string username = null, string password = null)
    {
        if (ServerUri != null)
        {
            if (autoLogIn
                && (Server == null
                    || Server.Uri != ServerUri
                    || !Server.HasAuthenticated
                    || UserName.IsNotNullOrWhiteSpace() && GetDefaultUserName() != GetCurrentUserName()))
                return await LoginAsync(username, password);

            return true;
        }

        return false;
    }

    public VssCredentials GetVssCredentials(bool forceDefault = false) =>
        new(new WindowsCredential(GetCredentials(forceDefault)));

    public ICredentials GetCredentials(bool forceDefault = false) =>
        forceDefault
            ? NetworkUtilities.GetCredentials()
            : NetworkUtilities.GetCredentials(UserName, Password);

    /// <summary>
    /// Gets the name of the current user.
    /// </summary>
    /// <param name="separator">The separator.</param>
    /// <returns></returns>
    public string GetCurrentUserName(string separator = " ") => Server.GetCurrentUserName(separator);

    public async Task<bool> LoginAsync(string username = null, string password = null)
    {
        try
        {
            if (username == null)
                username = UserName;

            if (password == null)
                password = Password;

            var toAuthenitcation = Server == null || Server?.Uri != ServerUri || !Server.HasAuthenticated;

            if (username != UserName
                || Password != password)
            {
                UserName = username;
                Password = password;
                toAuthenitcation = true;
            }

            if (toAuthenitcation)
            {
                ProgressViewModel?.IfNotNull(v => v.SetStatusInfo("Authenticating"));

                if (Server != null)
                {
                    Server.Dispose();
                    Server = null;
                }

                Server = new(ServerUri, GetVssCredentials());
                Server.Authenticate();

                if (Server.HasAuthenticated)
                {
                    UserInfo = await LoadUserInfoAsync();
                    ProgressViewModel?.IfNotNull(v => v.SetStatusInfo("Getting projects collections info"));
                    ProjectsCollectionsIsDirty = true;
                    await GetProjectsCollectionsAsync();
                    await GetBuildServerVersionAsync();

                    if (SuccessfullyAuthenticated != null)
                        await Task.Run(() => SuccessfullyAuthenticated(null, null));
                }
            }

            return true;
        }
        catch (TeamFoundationServerUnauthorizedException tfsEx)
        {
            if (tfsEx.AuthenticationError != TeamFoundationAuthenticationError.Cancelled)
                tfsEx.HandleException();
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return false;
    }

    public async Task GetBuildServerVersionAsync()
    {
        try
        {
            if (await GetServerAsync()
                && ProjectsCollections.Any(x => x.IsEnabled))
            {
                var tfs = ProjectsCollections.Count > 1
                    ? ProjectsCollections.First(x => x.IsEnabled).TfsTeamProjectCollection
                    : ProjectsCollections[0].TfsTeamProjectCollection;

                Version = ((IBuildServer)tfs.GetService(typeof(IBuildServer))).GetBuildServerVersion()
                    .GetServerVersionDescription();
            }
        }
        catch (Exception ex)
        {
            ex.HandleException();
            Version = TfsBuildServerVersion.None.GetServerVersionDescription();
        }
    }

    public async Task GetProjectsCollectionsAsync()
    {
        await EnvironmentLock.WaitAsync();

        if (Server != null)
        {
            if (!ProjectsCollectionsIsBusy && ProjectsCollectionsIsDirty)
                await GetTfsProjectsCollectionsAsync();
            else
                await EnsureProjectsCollectionsCacheAsync();
        }
        else
        {
            ProjectsCollections.Clear();
        }

        EnvironmentLock.Release();
    }

    public ConcurrentList<ProjectsCollection> GetProjectsCollections()
    {
        JoinableTaskExtensions.WaitWithoutThreadLock(GetProjectsCollectionsAsync);

        return _projectsCollections;
    }

    public async Task EnsureProjectsCollectionsCacheAsync()
    {
        if (LoadingProjectsCollections)
            await AsyncStatics.DelayUntilAsync(() => LoadingProjectsCollections);
    }

    public async Task<ImageSource> GetProfileImageAsync()
    {
        if (await GetServerAsync())
            return await GetUserImageAsync(ServerUri, GetCredentials(), Server.AuthorizedIdentity.TeamFoundationId);

        return null;
    }

    public async Task<TResponse> RunProcAsync<TResponse>(string requestUrl,
                                                         IDictionary<string, object> args = null,
                                                         JsonSerializerSettings settings = null,
                                                         IList<HttpStatusCode> omitCodes = null,
                                                         RequestMethod method = RequestMethod.GET)
        where TResponse : BaseTfsResponse, new()
    {
        using var processor = new FuncProcessor(requestUrl, Server.Credentials, EnvironmentId, args, method);

        return await processor.RunAsync<TResponse>(settings, omitCodes);
    }

    public async Task<string> RunRawAsync(string requestUrl,
                                          IDictionary<string, object> args = null,
                                          IList<HttpStatusCode> omitCodes = null,
                                          RequestMethod method = RequestMethod.GET)
    {
        using var processor = new FuncProcessor(requestUrl, Server.Credentials, EnvironmentId, args, method);

        return await processor.RunRawAsync(omitCodes);
    }

    private static Task<ImageSource> GetUserImageAsync(Uri serverUri, ICredentials credentials, Guid tfsUserId)
    {
        var imageUrl = new Uri($"{serverUri}_api/_common/identityImage?id={tfsUserId}");

        return TfsExtensions.LoadImageAsync(imageUrl, credentials);
    }

    /// <summary>
    /// Gets the default name of the user.
    /// </summary>
    /// <returns></returns>
    private string GetDefaultUserName()
    {
        if (ServerUri is null)
            return null;

        using var server = new TfsConfigurationServer(ServerUri, GetVssCredentials(true));
        server.Authenticate();

        return server.GetCurrentUserName();
    }

    private async Task<AuthenticatedUserInfo> LoadUserInfoAsync()
    {
        var json = await WebServices.GetRequestResultAsync($"{ServerUri}/_apis/connectiondata", GetCredentials());

        return JsonConvert.DeserializeObject<AuthenticatedUserInfo>(json);
    }

    /// <summary>
    /// Gets the TFS projects collections.
    /// </summary>
    private async Task GetTfsProjectsCollectionsAsync()
    {
        ProjectsCollectionsIsBusy = true;
        ProjectsCollectionsIsDirty = false;

        CollectionNodesCache = Server.CatalogNode.QueryChildren([CatalogResourceTypes.ProjectCollection],
            false,
            CatalogQueryOptions.None);

        await CollectionsLock.WaitAsync();

        if (!ProjectsCollectionsIsDirty)
        {
            ProgressViewModel?.IfNotNull(v => v.SetStatusInfo("Receiving projects collections info:"));
            CollectionsNames.AddRange(CollectionNodesCache.Select(x => x.Resource.DisplayName).OrderBy(x => x));
            NotifyProgress();
            ProgressViewModel?.PrgSetMax(CollectionNodesCache.Count);
            using var flakTimer = new FExTimer().WithCallback(NotifyProgress);
            flakTimer.Start();
            var tasks = CollectionNodesCache.Select(GetTfsProjectsCollectionsInfoAsync).ToList();
            _projectsCollections.Clear();
            _projectsCollections.AddRange((await Task.WhenAll(tasks)).Where(x => x != null).OrderBy(c => c.Name));
            flakTimer.Stop();

            if (_projectsCollections.Count == 1)
                _projectsCollections[0].IsChecked = true;
        }

        ProgressViewModel?.IfNotNull(v => v.SetCurrItemInfo(string.Empty));
        CollectionsLock.Release();
        ProjectsCollectionsIsBusy = false;
    }

    /// <summary>
    /// Gets the TFS projects collections information.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>
    /// Collection
    /// </returns>
    private async Task<ProjectsCollection> GetTfsProjectsCollectionsInfoAsync(CatalogNode node)
    {
        var collection = new ProjectsCollection(node, this, GetCollectionNameForRequests());
        await collection.RefreshCollectionAsync();
        CollectionsNames.Remove(collection.Name);
        ProgressViewModel?.PrgAdd();

        return collection;
    }

    /// <summary>
    /// Notifies the progress.
    /// </summary>
    private void NotifyProgress()
    {
        ProgressViewModel?.IfNotNull(v => v.SetCurrItemInfo(string.Join(", ", CollectionsNames)));
    }

    private string GetCollectionNameForRequests() =>
        CollectionNodesCache.Count > 1
            ? null
            : "DefaultCollection";
}