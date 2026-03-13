using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.OneDrv;

public class OneDriveClient
{
    private readonly string[] _scopes;
    private readonly PublicClientApplicationOptions _appConfiguration;

    public OneDriveClient()
    {
        _scopes = ["User.Read", "Files.Read", "Files.Read.All"];

        _appConfiguration = new()
        {
            Instance = "https://login.microsoftonline.com/",
            ClientId = Environment.GetEnvironmentVariable("OneDriveAppClientId", EnvironmentVariableTarget.User),
            TenantId = "common"
        };
    }

    public async Task<List<Drive>> ListDrivesAsync(CancellationToken cancellationToken = default)
    {
        var drives = new List<Drive>();

        try
        {
            var client = GetGraphServiceClient();
            var r = await client.Me.Drives.GetAsync(cancellationToken: cancellationToken);

            var pageIterator = PageIterator<Drive, DriveCollectionResponse>.CreatePageIterator(client,
                r,
                d =>
                {
                    drives.Add(d);

                    return true;
                });

            await pageIterator.IterateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
        }

        return drives;
    }

    private GraphServiceClient GetGraphServiceClient()
    {
        var interactiveBrowserCredentialOptions = new InteractiveBrowserCredentialOptions
        {
            ClientId = _appConfiguration.ClientId
        };

        var interactiveBrowserCredential = new InteractiveBrowserCredential(interactiveBrowserCredentialOptions);

        return
            new(interactiveBrowserCredential,
                _scopes); // you can pass the TokenCredential directly to the GraphServiceClient
    }
}