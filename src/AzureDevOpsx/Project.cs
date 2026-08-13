using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.AzureDevOpsx.Extensions;
using FEx.Json.Converters;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace FEx.AzureDevOpsx;

/// <summary>
/// Container for TFS project properties
/// https://www.visualstudio.com/en-us/docs/integrate/api/tfs/projects
/// </summary>
[JsonConverter(typeof(JsonPathConverter))]
public class Project : NotifyPropertyChanged
{
    private bool _isWorkspacePresent;
    private List<Workspace>? _projectWorkspaces;
    private Uri? _defaultTeamUrl;
    private string? _defaultTeamId;
    private string? _defaultTeamName;
    private Uri? _webUrl;
    private string? _id;
    private string? _name;
    private string? _description;
    private Uri? _apiUrl;
    private string? _stateString;
    private ProjectState _state;
    private string? _sourceControlTypeString;
    private SourceControlTypes _sourceControlType;
    private string? _processTemplateName;

    // Assigned by the owning ProjectsCollection immediately after construction/deserialization; never read before.
    private ProjectsCollection _collection = null!;
    private Uri? _codeSite;
    private string? _idOfRepo;

    /// <summary>
    /// Gets a value indicating whether this instance is workspace present.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is workspace present; otherwise, <c>false</c>.
    /// </value>
    public bool IsWorkspacePresent
    {
        get => _isWorkspacePresent;
        private set => SetProperty(ref _isWorkspacePresent, value);
    }

    public List<Workspace>? ProjectWorkspaces
    {
        get => _projectWorkspaces;
        set => SetProperty(ref _projectWorkspaces, value);
    }

    /// <summary>
    /// URL of project default team.
    /// </summary>
    public Uri? DefaultTeamUrl
    {
        get => _defaultTeamUrl;
        private set => SetProperty(ref _defaultTeamUrl, value);
    }

    public TfsConfigurationServer Server => Collection.TfsTeamProjectCollection.ConfigurationServer;

    /// <summary>
    /// ID of project default team.
    /// </summary>
    [JsonProperty("defaultTeam.id")]
    public string? DefaultTeamId
    {
        get => _defaultTeamId;
        set => SetProperty(ref _defaultTeamId, value);
    }

    /// <summary>
    /// Name  of project default team.
    /// </summary>
    [JsonProperty("defaultTeam.name")]
    public string? DefaultTeamName
    {
        get => _defaultTeamName;
        set => SetProperty(ref _defaultTeamName, value);
    }

    /// <summary>
    /// URL of project dashboard site.
    /// </summary>
    [JsonProperty("_links.web.href")]
    public Uri? WebUrl
    {
        get => _webUrl;
        set => SetProperty(ref _webUrl, value);
    }

    /// <summary>
    /// ID (<see cref="Guid" />) of the project.
    /// </summary>
    [JsonProperty("id")]
    public string? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// Name of the project.
    /// </summary>
    [JsonProperty("name")]
    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// Description of the project.
    /// </summary>
    [JsonProperty("description")]
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>
    /// Base TFS REST API URL of the project.
    /// </summary>
    [JsonProperty("url")]
    public Uri? ApiUrl
    {
        get => _apiUrl;
        set => SetProperty(ref _apiUrl, value);
    }

    /// <summary>
    /// State of the project.
    /// </summary>
    [JsonProperty("state")]
    public string? StateString
    {
        get => _stateString;
        set => SetProperty(ref _stateString, value);
    }

    /// <summary>
    /// State of the project.
    /// </summary>
    public ProjectState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    /// <summary>
    /// Source control type of the project.
    /// </summary>
    [JsonProperty("capabilities.versioncontrol.sourceControlType")]
    public string? SourceControlTypeString
    {
        get => _sourceControlTypeString;
        set => SetProperty(ref _sourceControlTypeString, value);
    }

    /// <summary>
    /// Source control type of the project.
    /// </summary>
    public SourceControlTypes SourceControlType
    {
        get => _sourceControlType;
        private set => SetProperty(ref _sourceControlType, value);
    }

    /// <summary>
    /// Process template name of the project.
    /// </summary>
    [JsonProperty("capabilities.processTemplate.templateName")]
    public string? ProcessTemplateName
    {
        get => _processTemplateName;
        set => SetProperty(ref _processTemplateName, value);
    }

    /// <summary>
    /// Collection name of the project.
    /// </summary>
    public ProjectsCollection Collection
    {
        get => _collection;
        protected internal set
        {
            if (SetProperty(ref _collection, value))
                OnPropertyChanged(nameof(CollectionName));
        }
    }

    /// <summary>
    /// Collection name of the project.
    /// </summary>
    public string? CollectionName => Collection.Name;

    /// <summary>
    /// URL of project code explorer site.
    /// </summary>
    public Uri? CodeSite
    {
        get => _codeSite;
        private set => SetProperty(ref _codeSite, value);
    }

    /// <summary>
    /// ID of Git project repository.
    /// </summary>
    public string? IdOfRepo
    {
        get => _idOfRepo;
        private set => SetProperty(ref _idOfRepo, value);
    }

    /// <summary>
    /// Initializes new <see cref="Project" /> class instance.
    /// </summary>
    public Project()
    {
    }

    public async Task RefreshAsync()
    {
        SourceControlType = TfsExtensions.SourceControlTypes
            .FirstOrDefault(x => x.Value.IsEqual(SourceControlTypeString ?? string.Empty))
            .Key;

        DefaultTeamUrl = WebUrl != null && DefaultTeamName != null
            ? new Uri(Uri.EscapeUriString($"{WebUrl}/{DefaultTeamName}/_admin?_a=members"))
            : null;

        State = TfsExtensions.ProjectStates.FirstOrDefault(x => x.Value.IsEqual(StateString ?? string.Empty)).Key;
        CodeSite = GetProjectCodeSiteUrl();
        IdOfRepo = await GitGetRepoIdAsync();
        var workspaces = this.GetProjectWorkspaces();
        ProjectWorkspaces = workspaces;
        IsWorkspacePresent = workspaces.Count > 0;
    }

    /// <summary>
    /// Gets the file hyperlink.
    /// </summary>
    /// <param name="serverItem">The server item.</param>
    /// <returns>Uri.</returns>
    public async Task<Uri?> GetFileHyperlinkAsync(string serverItem)
    {
        var file = await GetFileContentAsync(serverItem);

        return file.IsNotNullOrEmptyString()
            ? new($"{CodeSite?.AbsoluteUri}?path={HttpUtility.UrlEncode(VerifyFilePath(serverItem))}&_a=contents")
            : CodeSite;
    }

    /// <summary>
    /// Gets contents of file at specified path in specified project.
    /// </summary>
    /// <param name="serverItem"></param>
    /// <returns></returns>
    public async Task<string?> GetFileContentAsync(string serverItem)
    {
        if (SourceControlType == SourceControlTypes.Git)
            return await GitGetFileContentAsync(serverItem);

        if (SourceControlType == SourceControlTypes.Tfvc)
            return await TfvcGetFileContentAsync(serverItem);

        throw new ArgumentException("Unsupported source control type", SourceControlType.ToString());
    }

    public Task<string?> TfvcGetFileContentAsync(string serverItem)
    {
        serverItem = VerifyFilePath(serverItem);
        var requestString = $"tfvc/items/?path={serverItem}";

        return Collection.GetRequestResultAsync(requestString, false, [HttpStatusCode.NotFound]);
    }

    public Task<string?> GitGetFileContentAsync(string serverItem)
    {
        serverItem = serverItem.Replace("$\\" + Name, string.Empty).Replace("\\", "/");
        var requestString = "git/" + Name + "/repositories/" + Name + "/items?scopePath=" + serverItem;

        return Collection.GetRequestResultAsync(requestString,
            false,
            [HttpStatusCode.BadRequest, HttpStatusCode.NotFound]);
    }

    public async Task<JObject> GitGetFileInfoAsync(string serverItem)
    {
        serverItem = VerifyFilePath(serverItem);
        var requestString = $"git/{Name}/repositories/{Name}/items?scopePath={serverItem}";

        var resp = await Collection.GetRequestResultAsync(requestString,
            true,
            [HttpStatusCode.BadRequest, HttpStatusCode.NotFound]);

        return resp.IsNullOrEmptyString()
            ? new()
            : JObject.Parse(resp);
    }

    /// <summary>
    /// Updates file at specified path with provided content.
    /// </summary>
    /// <param name="serverItem">
    /// Absolute path to desired file from the root of the project. I.e.:
    /// CatalogA\CatalogB\File.example
    /// </param>
    /// <param name="fileContent">Content used to overwrite current file content.</param>
    /// <param name="comment">Comment attached to this update.</param>
    /// <returns>True if operation was successful.</returns>
    public async Task<bool> SaveFileContentAsync(string serverItem, string fileContent, string comment = "")
    {
        switch (SourceControlType)
        {
            case SourceControlTypes.Git:
                return await GitSaveFileContentAsync(VerifyFilePath(serverItem), fileContent, comment);
            case SourceControlTypes.Tfvc:
                return TfsExtensions.SaveFileContent(VerifyFilePath(serverItem), this, fileContent, comment);
            default:
                throw new ArgumentException("Unsupported source control type", SourceControlType.ToString());
        }
    }

    public async Task<TfsChangeset> GetLastCheckinAsync()
    {
        switch (SourceControlType)
        {
            case SourceControlTypes.Git:
                return await GitGetLastCheckinAsync();
            case SourceControlTypes.Tfvc:
                return await Tfvc_GetLastCheckinAsync();
            default:
                throw new ArgumentException("Unsupported source control type", SourceControlType.ToString());
        }
    }

    /// <summary>
    /// Updates the name of the project.
    /// </summary>
    /// <param name="newName">The new name.</param>
    /// <returns></returns>
    public async Task<bool> UpdateProjectNameAsync(string newName)
    {
        if (newName != null)
        {
            var requestString = $"projects/{Name}";

            var patchContent = JsonConvert.SerializeObject(new
            {
                name = newName
            });

            return (await Collection.PatchRequestResultAsync(requestString, patchContent)).IsSuccess;
        }

        return false;
    }

    /// <summary>
    /// Updates the project description.
    /// </summary>
    /// <param name="newDescription">The new description.</param>
    /// <returns>
    /// System.Boolean
    /// </returns>
    public async Task<bool> UpdateProjectDescriptionAsync(string newDescription)
    {
        if (newDescription != null)
        {
            var requestString = $"projects/{Name}";

            var patchContent = JsonConvert.SerializeObject(new
            {
                description = newDescription
            });

            return (await Collection.PatchRequestResultAsync(requestString, patchContent)).IsSuccess;
        }

        return false;
    }

    /// <summary>
    /// Returns URL of project code explorer site.
    /// </summary>
    /// <returns><see cref="Uri" /> of code explorer site.</returns>
    private Uri GetProjectCodeSiteUrl()
    {
        var webUrl = WebUrl.Guard(nameof(WebUrl));
        switch (SourceControlType)
        {
            case SourceControlTypes.Git:
                var x = webUrl.Segments.ToList();
                x.Insert(x.Count - 1, "_git");

                return new(string.Join("/", x));
            case SourceControlTypes.Tfvc:
                return new(webUrl.AbsoluteUri + "/_versionControl");
            default:
                throw new ArgumentException("Unsupported source control type", SourceControlType.ToString());
        }
    }

    private string VerifyFilePath(string serverItem)
    {
        switch (SourceControlType)
        {
            case SourceControlTypes.Git:
                return $"/{serverItem.Replace($"$\\{Name}", string.Empty).Replace("\\", "/")}".Replace("//", "/");
            case SourceControlTypes.Tfvc:
                return serverItem.Replace("\\", "/").Replace("//", "/");
            default:
                throw new ArgumentException("Unsupported source control type", SourceControlType.ToString());
        }
    }

    /// <summary>
    /// Updates file at specified path with provided content.
    /// </summary>
    /// <param name="serverItem">
    /// Absolute path to desired file from the root of the project. I.e.:
    /// CatalogA\CatalogB\File.example
    /// </param>
    /// <param name="fileContent">Content used to overwrite current file content.</param>
    /// <param name="commitComment">Comment attached to this commit.</param>
    /// <returns>True if operation was successful.</returns>
    private async Task<bool> GitSaveFileContentAsync(string serverItem, string fileContent, string commitComment = "")
    {
        var requestString = $"git/repositories/{IdOfRepo}/pushes";
        var pushId = await GitGetLatestPushId_ByRepoIdAsync();
        var info = await GitGetFileInfoAsync(serverItem);

        var fileChangeType = (info?["count"]?.Value<int>() ?? 0) == 1
            ? "edit"
            : "add";

        if (commitComment.IsNullOrEmptyString())
            commitComment = $"{fileChangeType} {serverItem}";

        var patchContent = JsonConvert.SerializeObject(new
        {
            refUpdates = new List<object>
            {
                new
                {
                    name = "refs/heads/master",
                    oldObjectId = pushId
                }
            },
            commits = new List<object>
            {
                new
                {
                    comment = commitComment,
                    changes = new List<object>
                    {
                        new
                        {
                            changeType = fileChangeType,
                            item = new
                            {
                                path = serverItem
                            },
                            newContent = new
                            {
                                content = fileContent.Replace("\"", "'"),
                                contentType = "rawtext"
                            }
                        }
                    }
                }
            }
        });

        return await Collection.PostRequestResultAsync(requestString, patchContent);
    }

    /// <summary>
    /// Gets ID of project repository for specified project ID or name in specified TFS collection.
    /// </summary>
    /// <returns><see cref="string" /> with requested information.</returns>
    private async Task<string?> GitGetRepoIdAsync()
    {
        string? res = null;

        if (SourceControlType == SourceControlTypes.Git)
        {
            const string requestString = "git/repositories";
            var resp = await Collection.GetRequestResultAsync(requestString, true, null, $"{Name}/");

            if (resp.IsNotNullOrEmptyString())
            {
                var json = JObject.Parse(resp);
                res = json?["value"]?[0]?["id"]?.Value<string>() ?? string.Empty;
            }
        }

        return res;
    }

    /// <summary>
    /// Gets id of latest push to master branch of provided repository operation.
    /// </summary>
    /// <returns><see cref="string" /> with requested information.</returns>
    private async Task<string> GitGetLatestPushId_ByRepoIdAsync()
    {
        var res = string.Empty;
        var resp = await GitGetRawPushesInfoAsync();

        if (resp.IsNotNullOrEmptyString())
        {
            var json = JObject.Parse(resp);
            res = json?["value"]?[0]?["refUpdates"]?[0]?["newObjectId"]?.Value<string>() ?? string.Empty;
        }

        return res;
    }

    /// <summary>
    /// Gets raw JSON string with information about all push to master branch of provided repository operations.
    /// https://www.visualstudio.com/en-us/docs/integrate/api/git/pushes
    /// </summary>
    /// <returns>JSON <see cref="string" /> with requested information.</returns>
    private Task<string?> GitGetRawPushesInfoAsync()
    {
        var requestString =
            $"git/repositories/{IdOfRepo}/pushes?refName=refs/heads/master&includeRefUpdates=true&$top={int.MaxValue}";

        return Collection.GetRequestResultAsync(requestString);
    }

    private async Task<TfsChangeset> GitGetLastCheckinAsync()
    {
        var resp = await GitGetRawPushesInfoAsync();

        if (resp.IsNotNullOrEmptyString())
        {
            var json = JObject.Parse(resp);

            if (json == null
                || (json["count"]?.Value<int>() ?? 0) <= 0)
                return new();

            var displayName = json["value"]?[0]?["pushedBy"]?["displayName"]?.Value<string>() ?? string.Empty;
            var uniqueName = json["value"]?[0]?["pushedBy"]?["uniqueName"]?.Value<string>() ?? string.Empty;
            var createdDate = json["value"]?[0]?["date"]?.Value<string>() ?? string.Empty;
            var changesetId = json["value"]?[0]?["pushId"]?.Value<string>() ?? string.Empty;

            var url = (json["value"]?[0]?["repository"]?["remoteUrl"]?.Value<string>() ?? string.Empty)
                      + "/commit/"
                      + (json["value"]?[0]?["refUpdates"]?[0]?["newObjectId"]?.Value<string>() ?? string.Empty);

            Uri? uri = null;

            if (url.IsNotNullOrEmptyString()
                && url.Trim() != "/commit/")
                uri = new(url);

            var date = DateTime.ParseExact(createdDate,
                "MM/dd/yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);

            return new()
            {
                Committer = uniqueName.Replace("VCN\\", string.Empty),
                CommitterDisplayName = displayName,
                CreationDate = date,
                ChangesetId = changesetId,
                ChangesetUrl = uri
            };
        }

        return new();
    }

    private async Task<TfsChangeset> Tfvc_GetLastCheckinAsync()
    {
        var resp = await Collection.GetRequestResultAsync($"tfvc/changesets?searchCriteria.itemPath=$/{Name}&$top=1",
            true,
            [HttpStatusCode.NotFound]);

        if (resp.IsNullOrEmptyString())
            return new();

        var json = JObject.Parse(resp);

        if ((json["count"]?.Value<int>() ?? 0) <= 0)
            return new();

        var displayName = json["value"]?[0]?["checkedInBy"]?["displayName"]?.Value<string>() ?? string.Empty;
        var uniqueName = json["value"]?[0]?["checkedInBy"]?["uniqueName"]?.Value<string>() ?? string.Empty;
        var createdDate = json["value"]?[0]?["createdDate"]?.Value<string>() ?? string.Empty;
        var changesetId = json["value"]?[0]?["changesetId"]?.Value<string>() ?? string.Empty;

        var chResp =
            await Collection.GetRequestResultAsync($"tfvc/changesets/{changesetId}", true, [HttpStatusCode.NotFound]);

        var chJson = JObject.Parse(chResp.Guard(nameof(chResp)));
        var url = chJson["_links"]?["web"]?["href"]?.Value<string>() ?? string.Empty;
        Uri? uri = null;

        if (url.IsNotNullOrEmptyString())
            uri = new(url);

        var date = DateTime.ParseExact(createdDate,
            "MM/dd/yyyy HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);

        return new()
        {
            Committer = uniqueName.Replace("VCN\\", string.Empty),
            CommitterDisplayName = displayName,
            CreationDate = date,
            ChangesetId = changesetId,
            ChangesetUrl = uri
        };
    }
}