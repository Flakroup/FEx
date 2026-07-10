using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.AzureDevOpsx.Responses;
using FEx.AzureDevOpsx.Services;
using FEx.Core.Collections.Concurrent;
using FEx.Core.Extensions;
using FEx.Json.Extensions;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FEx.AzureDevOpsx.Entities;

public class ShelvesetContent : NotifyPropertyChanged
{
    private TfsEnvironment? _tfsEnvironment;

    private ConcurrentObservableList<ShelvesetChange>? _changes;
    private string? _name;
    private string? _id;
    private Owner? _owner;
    private DateTimeOffset _createdDate;
    private Uri? _url;
    private string? _comment;
    private Links? _links;

    [JsonIgnore]
    public string? EnvironmentId { get; internal set; }

    [JsonProperty("changes")]
    public ConcurrentObservableList<ShelvesetChange>? Changes
    {
        get => _changes;
        set => SetProperty(ref _changes, value);
    }

    [JsonProperty("name")]
    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [JsonProperty("id")]
    public string? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [JsonProperty("owner")]
    public Owner? Owner
    {
        get => _owner;
        set => SetProperty(ref _owner, value);
    }

    [JsonProperty("createdDate")]
    public DateTimeOffset CreatedDate
    {
        get => _createdDate;
        set => SetProperty(ref _createdDate, value);
    }

    [JsonProperty("url")]
    public Uri? Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    [JsonProperty("comment")]
    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    [JsonProperty("_links")]
    public Links? Links
    {
        get => _links;
        set => SetProperty(ref _links, value);
    }

    [JsonIgnore]
    private TfsEnvironment? TfsEnvironment
    {
        get
        {
            if (_tfsEnvironment == null
                || _tfsEnvironment.EnvironmentId != EnvironmentId)
                _tfsEnvironment =
                    TfsService.Instance.TfsEnvironments.FindInEnumerable(x => x.EnvironmentId == EnvironmentId);

            return _tfsEnvironment;
        }
    }

    public async Task LoadChangesAsync()
    {
        var env = TfsEnvironment.Guard(nameof(TfsEnvironment));
        var url = Url.Guard(nameof(Url));
        var jsonStr = await env.RunRawAsync(url.AbsoluteUri,
            new Dictionary<string, object>
            {
                ["maxChangeCount"] = int.MaxValue
            },
            new List<HttpStatusCode>
            {
                HttpStatusCode.NotFound,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.BadRequest
            });

        if (jsonStr != null)
        {
            var res = jsonStr.FromJson<ShelvesetContent>(ShelvesetResponse.Settings);
            Refresh(res);
            GetShelve();
        }
        else
        {
            GetShelve();
        }
    }

    private void GetShelve()
    {
        var env = TfsEnvironment.Guard(nameof(TfsEnvironment));
        var vcs = env.ProjectsCollections[0].TfsTeamProjectCollection.GetService<VersionControlServer>();

        var owner = Owner.Guard(nameof(Owner));
        var sh = vcs.QueryShelvesets(Name, owner.UniqueName);
        var ch = vcs.QueryShelvedChanges(sh[0]);

        Changes = ch[0]
            .PendingChanges.Select(x => new ShelvesetChange
            {
                ChangeTypeString = x.ChangeTypeName,
                SourceServerItem = x.SourceServerItem,
                Item = new()
                {
                    HashValue = Convert.ToBase64String(x.HashValue),
                    Url = new(
                        $"{x.VersionControlServer.TeamProjectCollection.Uri}/_apis/tfvc/items/{x.ServerItem}?versionType=Shelveset&version="),
                    Path = x.ServerItem,
                    Version = x.Version,
                    IsFolder = x.ItemType == ItemType.Folder
                }
            })
            .ToConcurrentObservableList();
    }

    private void Refresh(ShelvesetContent? res)
    {
        if (res != null)
        {
            var resChanges = res.Changes.Guard(nameof(res.Changes));
            if (Changes != null)
            {
                Changes.Clear();
                Changes.AddRange(resChanges);
            }
            else
            {
                Changes = resChanges.ToConcurrentObservableList();
            }

            Name = res.Name;

            Id = res.Id;

            if (Owner != null)
            {
                var resOwner = res.Owner.Guard(nameof(res.Owner));
                Owner.Id = resOwner.Id;
                Owner.DisplayName = resOwner.DisplayName;
                Owner.UniqueName = resOwner.UniqueName;
                Owner.Url = resOwner.Url;
                Owner.ImageUrl = resOwner.ImageUrl;
            }
            else
            {
                Owner = res.Owner;
            }

            CreatedDate = res.CreatedDate;

            Url = res.Url;

            Comment = res.Comment;

            if (Links != null)
            {
                var resLinks = res.Links.Guard(nameof(res.Links));
                Links.Changes.Guard(nameof(Links.Changes)).Href = resLinks.Changes.Guard(nameof(resLinks.Changes)).Href;
                Links.Owner.Guard(nameof(Links.Owner)).Href = resLinks.Owner.Guard(nameof(resLinks.Owner)).Href;
                Links.Self.Guard(nameof(Links.Self)).Href = resLinks.Self.Guard(nameof(resLinks.Self)).Href;
                Links.WorkItems.Guard(nameof(Links.WorkItems)).Href = resLinks.WorkItems.Guard(nameof(resLinks.WorkItems)).Href;
            }
            else
            {
                Links = res.Links;
            }
        }
    }
}