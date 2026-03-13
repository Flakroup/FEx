using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Models;
using FEx.Agnostics.BaseObjects;
using FEx.AzureDevOpsx.Entities;
using FEx.AzureDevOpsx.Extensions;
using FEx.AzureDevOpsx.Responses;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.Downloader.Clients;
using FEx.Flurlx.Models;
using FEx.Json.Extensions;
using FEx.Legacy.Web;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Extensions;
using FEx.Webx.Models;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FEx.AzureDevOpsx;

/// <summary>
///     Container for TFS projects collection properties
///     https://www.visualstudio.com/en-us/docs/integrate/api/tfs/project-collections
/// </summary>
public class ProjectsCollection : NotifyPropertyChanged
{
    private bool _isChecked;
    private ConcurrentObservableList<Workspace> _workspaces;
    private bool _isEnabled;
    private ConcurrentObservableList<ShelvesetContent> _shelvesets;
    private bool _isIdle;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        private set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    ///     Gets the projects.
    /// </summary>
    /// <value>
    ///     The projects.
    /// </value>
    public ConcurrentObservableList<Project> Projects { get; }

    /// <summary>
    ///     Gets or sets the shelvesets.
    /// </summary>
    /// <value>
    ///     The shelvesets.
    /// </value>
    public ConcurrentObservableList<ShelvesetContent> Shelvesets
    {
        get => _shelvesets;
        private set => SetProperty(ref _shelvesets, value);
    }

    public bool IsIdle
    {
        get => _isIdle;
        set => SetProperty(ref _isIdle, value);
    }

    /// <summary>
    ///     Gets the workspaces.
    /// </summary>
    /// <value>
    ///     The workspaces.
    /// </value>
    public ConcurrentObservableList<Workspace> Workspaces
    {
        get => _workspaces;
        private set => SetProperty(ref _workspaces, value);
    }

    public string CollectionNameForRequests { get; }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>
    ///     The name.
    /// </value>
    public string Name { get; }

    /// <summary>
    ///     Gets the description.
    /// </summary>
    /// <value>
    ///     The description.
    /// </value>
    public string Description { get; }

    /// <summary>
    ///     Gets the identifier.
    /// </summary>
    /// <value>
    ///     The identifier.
    /// </value>
    public Guid Identifier { get; }

    /// <summary>
    ///     Gets or sets the collection node.
    /// </summary>
    /// <value>
    ///     The collection node.
    /// </value>
    public CatalogNode CollectionNode { get; }

    /// <summary>
    ///     Gets the URL.
    /// </summary>
    /// <value>
    ///     The URL.
    /// </value>
    public Uri Url { get; }

    internal TfsTeamProjectCollection TfsTeamProjectCollection { get; }

    private TfsEnvironment TfsEnvironment { get; }

    private string BaseApiUri { get; }

    private VssConnection Connection { get; }

    /// <summary>
    ///     Initializes new <see cref="ProjectsCollection" /> class instance.
    /// </summary>
    public ProjectsCollection(CatalogNode collectionNode, TfsEnvironment environment, string collectionNameForRequests)
    {
        TfsEnvironment = environment;
        CollectionNode = collectionNode;
        TfsTeamProjectCollection = CollectionNode.GetTeamProjectCollection(environment.Server);
        Identifier = CollectionNode.Resource.Identifier;
        Description = CollectionNode.Resource.Description;
        Name = CollectionNode.Resource.DisplayName;
        CollectionNameForRequests = collectionNameForRequests ?? Name;
        Url = new Uri($"{TfsTeamProjectCollection.ConfigurationServer.Uri.AbsoluteUri}/{CollectionNameForRequests}/");
        BaseApiUri = $"{Url.AbsoluteUri}_apis";
        Shelvesets = [];
        Workspaces = [];
        Projects = [];
        IsIdle = true;
        Connection = new VssConnection(Url, environment.Server.ClientCredentials);
    }

    /// <summary>
    ///     Refreshes the collection.
    /// </summary>
    /// <returns></returns>
    public async Task RefreshCollectionAsync()
    {
        IsIdle = false;
        string raw = await GetRawTfsCollectionProjectsAsync(ProjectState.All, 1);
        if (raw.IsNotNullOrEmptyString()
            && JObject.Parse(raw)["count"]
                .Value<int>()
            > 0)
            await Task.WhenAll(RefreshTfsCollectionProjectsAsync(), Task.Run(() => GetWorkspace()));

        IsEnabled = Projects.IsNotNullOrEmptyList();
        IsIdle = true;
    }

    /// <summary>
    ///     Refreshes information about all projects from provided collection.
    /// </summary>
    /// <param name="prg">The PRG.</param>
    /// <returns>
    ///     <see cref="IList{Project}" /> containing requested information.
    /// </returns>
    public async Task RefreshTfsCollectionProjectsAsync(IProgress<string> prg = null)
    {
        bool unsetIdle = IsIdle;
        IsIdle = false;
        Projects.Clear();
        string jsonStr = await GetRawTfsCollectionProjectsAsync();
        if (jsonStr != null)
        {
            List<Project> projects = JObject.Parse(jsonStr)["value"]
                .DeserializeToken<List<Project>>();
            var tasks = projects.Select((_, i) => i)
                .Select(idx => GetTfsCollectionProjectAsync(projects[idx]
                    .Id, true, prg))
                .ToList();
            Projects.AddRange((await Task.WhenAll(tasks)).Where(x => x != null));
        }

        if (unsetIdle)
            IsIdle = true;
    }

    /// <summary>
    ///     Downloads the shelveset.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <param name="id">The identifier.</param>
    /// <param name="viewModel">The view model.</param>
    /// <returns></returns>
    public async Task DownloadShelvesetAsync(string rootDirectory, string id, IProgressAggregator viewModel)
    {
        viewModel?.IfNotNull(v => v.SetStatusInfo("Downloading Shelveset"));
        string jsonStr = await GetRequestResultAsync($"tfvc/shelvesets/{id}?maxChangeCount={int.MaxValue}");
        ShelvesetContent slv = JsonConvert.DeserializeObject<ShelvesetContent>(jsonStr);
        viewModel?.PrgSetMax(slv.Changes.Count);
        viewModel?.IfNotNull(v => v.SetUnit("Changes"));
        IEnumerable<Task> changesSeq = slv.Changes.Select(change => DownloadFileFromUrlAsync(rootDirectory, change.Item.Url, change.Item.Path.Replace("$/", $"{rootDirectory}/")
                .Replace("/", "\\"), TfsTeamProjectCollection.ConfigurationServer.Credentials, viewModel)
            .ContinueWith(_ => viewModel?.PrgAdd(), TaskScheduler.Default));
        await Task.WhenAll(changesSeq.ToArray());
        viewModel?.IfNotNull(v => v.SetUnit(string.Empty));
    }

    public async Task GetShelvesetsAsync(bool refresh = true)
    {
        if (refresh || Shelvesets.IsNullOrEmptyList())
            await RefreshShelvesetsAsync();
    }

    /// <summary>
    ///     Gets the myself information.
    /// </summary>
    protected async Task GetMyselfInfoAsync()
    {
        var requestString = $"projectCollections/{Identifier.ToString().ToLower(CultureInfo.InvariantCulture)}";
        string json = await GetRequestResultAsync(requestString, true,
            [HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable, HttpStatusCode.Forbidden]);
        if (json.IsNotNullOrEmptyString())
        {
            var res = JToken.Parse(json);
        }
    }

    /// <summary>
    ///     Lists the shelveset changes.
    /// </summary>
    /// <param name="shelveset">The shelveset.</param>
    /// <param name="viewModel">The view model.</param>
    /// <returns>
    ///     List{Change}
    /// </returns>
    protected async Task LoadShelvesetChangesAsync(ShelvesetContent shelveset, IProgressAggregator viewModel)
    {
        await shelveset.LoadChangesAsync();
        viewModel?.PrgAdd();
    }

    protected async Task RefreshOwnerImageAsync(ShelvesetContent shelvesetContent, IProgressAggregator viewModel = null)
    {
        await shelvesetContent.Owner.RefreshImageAsync(TfsEnvironment);
        viewModel?.PrgAdd();
    }

    /// <summary>
    ///     Handles GET requests.
    /// </summary>
    /// <param name="requestUrl">Pass only what's between .../_apis/ and &amp;api-version... Rest is added automatically</param>
    /// <param name="resultAsJson">Set to true (which is default) if you expect JSON in response. Else set to false.</param>
    /// <param name="ommitCodes">List of expected - not critical status codes.</param>
    /// <param name="afterCollectionName">Name of the after collection.</param>
    /// <param name="cookies">The cookies.</param>
    /// <returns>
    ///     <see cref="string" /> with requested data.
    /// </returns>
    internal Task<string> GetRequestResultAsync(string requestUrl, bool resultAsJson = true, List<HttpStatusCode> ommitCodes = null, string afterCollectionName = "", List<Cookie> cookies = null)
    {
        return WebServices.GetRequestResultAsync($"{Url}{afterCollectionName}_apis/{requestUrl}", TfsTeamProjectCollection.ConfigurationServer.Credentials, resultAsJson, ommitCodes, cookies);
    }

    internal Task<TResponse> RunProcAsync<TResponse>(string scriptPath, IDictionary<string, object> args = null, JsonSerializerSettings settings = null, IList<HttpStatusCode> ommitCodes = null, RequestMethod method = RequestMethod.GET) where TResponse : BaseTfsResponse, new()
    {
        return TfsEnvironment.RunProcAsync<TResponse>(BaseApiUri + scriptPath, args, settings, ommitCodes, method);
    }

    internal Task<string> RunProcAsync(string scriptPath, IDictionary<string, object> args = null, IList<HttpStatusCode> ommitCodes = null, RequestMethod method = RequestMethod.GET)
    {
        return TfsEnvironment.RunRawAsync(BaseApiUri + scriptPath, args, ommitCodes, method);
    }

    /// <summary>
    ///     Handles POST requests.
    /// </summary>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="postContent">Content of the post.</param>
    /// <param name="ommitCodes">The ommit codes.</param>
    /// <param name="cookies">The cookies.</param>
    /// <returns></returns>
    internal async Task<bool> PostRequestResultAsync(string requestUrl, string postContent, List<HttpStatusCode> ommitCodes = null, List<Cookie> cookies = null)
    {
        return (await WebServices.PostRequestResultAsync($"{BaseApiUri}/{requestUrl}", postContent, TfsTeamProjectCollection.ConfigurationServer.Credentials, ommitCodes, cookies)).IsSuccess;
    }

    /// <summary>
    ///     Handles PATCH requests.
    /// </summary>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="patchContent">Content of the patch.</param>
    /// <param name="ommitCodes">The ommit codes.</param>
    /// <param name="cookies">The cookies.</param>
    /// <returns></returns>
    internal Task<ResponseResult> PatchRequestResultAsync(string requestUrl, string patchContent, List<HttpStatusCode> ommitCodes = null, List<Cookie> cookies = null)
    {
        requestUrl = $"{BaseApiUri}/{requestUrl}";
        requestUrl = Uri.EscapeUriString(requestUrl);
        HttpClient client = WebServices.PrepareHttpClient(requestUrl, TfsTeamProjectCollection.ConfigurationServer.Credentials, false, cookies);
        HttpContent patch = new StringContent(patchContent, Encoding.UTF8, MediaTypes.ApplicationJson.GetEnumValueDescription());
        return WebServices.HandleResponseAsync(requestUrl, ommitCodes, () => client.PatchAsync(requestUrl, patch));
    }

    /// <summary>
    ///     Downloads the file from URL.
    /// </summary>
    /// <param name="rootDirectory">The root directory.</param>
    /// <param name="url">The URL.</param>
    /// <param name="serverPath">The server path.</param>
    /// <param name="credentials">The credentials.</param>
    /// <param name="viewModel">The view model.</param>
    /// <returns></returns>
    internal async Task DownloadFileFromUrlAsync(string rootDirectory, Uri url, string serverPath, ICredentials credentials, IProgressAggregator viewModel)
    {
        using var fwc = new FlakWebClient(new WebRequestParams { Credentials = credentials }, (_, e) => viewModel.SetCurrentDownloadState(e));

        string localPath = serverPath.Replace("$/", $"{rootDirectory}/")
            .Replace("/", "\\");
        string dirPath = Path.GetDirectoryName(localPath);
        if (dirPath.IsNotNullOrWhiteSpace())
            Directory.CreateDirectory(dirPath);

        if (File.Exists(localPath))
            File.Delete(localPath);

        await fwc.DownloadFileTaskAsync(url, localPath);
    }

    /// <summary>
    ///     Gets the workspace.
    /// </summary>
    private void GetWorkspace(string workspaceName = null, string userName = null, string compName = null)
    {
        try
        {
            VersionControlServer service = TfsTeamProjectCollection.GetService<VersionControlServer>();

            if (compName == null)
                compName = Environment.MachineName;

            Workspace[] workspaces = service.QueryWorkspaces(workspaceName, userName, compName);
            Workspaces.Clear();
            Workspaces.AddRange(workspaces);
        }
        catch (Exception ex)
        {
            var tfsEx = ex as TeamFoundationServerUnauthorizedException;
            if (tfsEx != null)
                tfsEx.HandleException(false);
            else
                ex.HandleException(false);
        }
    }

    /// <summary>
    ///     Gets raw JSON string with information about all projects from provided collection and in selected state.
    /// </summary>
    /// <param name="filter">
    ///     <see cref="ProjectState" /> used to filter results. Default is  <see cref="ProjectState.All" />
    /// </param>
    /// <param name="top">The maximum returned results.</param>
    /// <returns>
    ///     JSON <see cref="string" /> with requested information.
    /// </returns>
    private Task<string> GetRawTfsCollectionProjectsAsync(ProjectState filter = ProjectState.All, int top = int.MaxValue)
    {
        var requestString = $"projects?stateFilter={filter}&$top={top}";
        return GetRequestResultAsync(requestString, true,
            [HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable]);
    }

    private async Task<ShelvesetContent[]> ListShelvesetsAsync(int top = 100)
    {
        ShelvesetResponse response = await RunProcAsync<ShelvesetResponse>("/tfvc/shelvesets", new Dictionary<string, object>
        {
            ["maxCommentLength"] = int.MaxValue,
            ["$top"] = top
        }, ShelvesetResponse.Settings);
        foreach (ShelvesetContent shelveset in response.Value)
        {
            shelveset.EnvironmentId = TfsEnvironment.EnvironmentId;
            shelveset.Owner.EnvironmentId = TfsEnvironment.EnvironmentId;
        }

        return response.Value;
    }

    /// <summary>
    ///     Gets project properties.
    /// </summary>
    /// <param name="projectIdOrName">Name or id (<see cref="Guid" />) of the project.</param>
    /// <param name="includeCapabilities">If true (which is default) capabilities (such as source control) will be included.</param>
    /// <param name="prg">The PRG.</param>
    /// <returns></returns>
    private async Task<Project> GetTfsCollectionProjectAsync(string projectIdOrName, bool includeCapabilities = true, IProgress<string> prg = null)
    {
        Project project = null;
        var requestString = $"projects/{projectIdOrName}";
        if (includeCapabilities)
            requestString += "?includeCapabilities=true";
        string jsonStr = await GetRequestResultAsync(requestString);
        if (jsonStr != null)
        {
            project = JsonConvert.DeserializeObject<Project>(jsonStr);
            project.Collection = this;
            await project.RefreshAsync();
        }

        if (prg != null)
            prg.Report(projectIdOrName);
        return project;
    }

    /// <summary>
    ///     Refreshes the shelvesets.
    /// </summary>
    private async Task RefreshShelvesetsAsync()
    {
        Shelvesets.Clear();
        Shelvesets.AddRange(await ListShelvesetsAsync(int.MaxValue));
    }
}
