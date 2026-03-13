using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.Imaging.Windows;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FEx.AzureDevOpsx.Extensions;

public static class TfsExtensions
{
    public static Dictionary<ChangeType, string> ChangeTypes { get; } = Enum.GetValues(typeof(ChangeType))
        .Cast<ChangeType>()
        .ToDictionary(x => x, x => Enum.GetName(x.GetType(), x));

    public static Dictionary<SourceControlTypes, string> SourceControlTypes { get; } = Enum.GetValues(typeof(SourceControlTypes))
        .Cast<SourceControlTypes>()
        .ToDictionary(x => x, x => Enum.GetName(x.GetType(), x));

    public static Dictionary<ProjectState, string> ProjectStates { get; } = Enum.GetValues(typeof(ProjectState))
        .Cast<ProjectState>()
        .ToDictionary(x => x, x => Enum.GetName(x.GetType(), x));

    public static TfsBuildServerVersion GetBuildServerVersion(this IBuildServer buildServer)
    {
        if (buildServer != null)
            return (TfsBuildServerVersion)buildServer.BuildServerVersion;
        return TfsBuildServerVersion.None;
    }

    public static string GetServerVersionDescription(this TfsBuildServerVersion buildServerVersion)
    {
        return buildServerVersion.GetEnumValueDescription();
    }

    public static List<Workspace> GetProjectWorkspaces(this Project project)
    {
        return project.Collection.Workspaces.SelectMany(v => v.Folders, (v, folder) => new { v, folder })
            .Where(t => (t.folder.ServerItem == $"$/{project.Name}" || t.folder.ServerItem == "$/") && !t.folder.IsCloaked && Directory.Exists(t.folder.LocalItem))
            .Select(t => t.v)
            .ToList();
    }

    public static TfsTeamProjectCollection GetTeamProjectCollection(this CatalogNode collectionNode, TfsConfigurationServer server)
    {
        return GetTeamProjectCollection(collectionNode.Resource.Properties["InstanceId"], server);
    }

    public static TfsTeamProjectCollection GetTeamProjectCollection(string instanceId, TfsConfigurationServer server)
    {
        return server.GetTeamProjectCollection(new Guid(instanceId));
    }

    /// <summary>
    ///     Gets the name of the current user.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="separator">The separator.</param>
    /// <returns></returns>
    public static string GetCurrentUserName(this TfsConfigurationServer server, string separator = " ")
    {
        if (server != null)
            return server.AuthorizedIdentity.DisplayName + separator + server.AuthorizedIdentity.UniqueName;
        return null;
    }

    public static async Task<ImageSource> LoadImageAsync(Uri imageUrl, ICredentials credentials, SemaphoreSlim rateLimit = null)
    {
        if (rateLimit != null)
            await rateLimit.WaitAsync();

        ImageSource res = null;
        try
        {
            IFilesCacheService cache = FExServiceProvider.Instance.GetRequiredService<FilesCacheService>();
            res = await cache.GetImageAsync(imageUrl, null, new WebRequestParams
            {
                Credentials = credentials,
                UserAgent = "VSTSAuthSample-AuthenticateADALNonInteractive",
                Headers = new Dictionary<string, string> { ["X-TFS-FedAuthRedirect"] = "Suppress" }
            });
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        rateLimit?.Release();

        return res;
    }

    internal static bool SaveFileContent(string serverItem, Project project, string fileContent, string checkinComment = "")
    {
        try
        {
            List<Workspace> localWorkspaces = project.GetProjectWorkspaces();
            if (localWorkspaces.Count <= 0)
                return false;

            Workspace workspace = project.ProjectWorkspaces.Find(x => x.MappingsAvailable);
            if (workspace != null)
            {
                WorkingFolder folder = workspace.Folders.FindInEnumerable(t => (t.ServerItem == "$/" || t.ServerItem == $"$/{project.Name}") && !t.IsCloaked && Directory.Exists(t.LocalItem));
                if (folder != null)
                {
                    string filePath = folder.LocalItem;
                    if (folder.ServerItem == "$/")
                        filePath = Path.Combine(filePath, project.Name);

                    if (filePath.IsNullOrEmptyString()
                        || !Directory.Exists(filePath))
                        return false;

                    filePath = Path.Combine(filePath, Path.GetFileName($"$\\{project.Name}\\{serverItem}".Replace("\\\\", "\\")));

                    if (checkinComment.IsNullOrEmptyString())
                        checkinComment = $"Updated {serverItem}";

                    File.WriteAllText(filePath, fileContent);
                    PendingChange[] pendingChanges = workspace.GetPendingChanges();
                    workspace.CheckIn([pendingChanges.First(x => x.ServerItem == $"$/{project.Name}/{serverItem}")], checkinComment);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return false;
    }
}
