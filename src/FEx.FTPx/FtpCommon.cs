using FEx.Logging;
using FEx.MVVM.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using FluentFTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

public static class FtpCommon
{
    public static async Task<FtpListItem?> GetFtpFileInfoAsync(string ftpfilepath,
                                                              Uri ftphost,
                                                              string username,
                                                              string password,
                                                              bool useProxy = false,
                                                              int port = 0,
                                                              FtpDataConnectionType? dataConnectionType = null)
    {
        try
        {
            return await RunFtpActionAsync(ftphost,
                username,
                password,
                useProxy,
                client => client.GetObjectInfo(ftpfilepath, true),
                port,
                dataConnectionType);
        }
        catch (Exception ex)
        {
            FExLoggingModule.Log(ex.ToString(), typeof(FtpCommon));

            return null; //todo https://github.com/robinrodricks/FluentFTP/issues/268 currently ignored
        }
    }

    public static async Task<AsyncFtpClient> CreateAsync(Uri ftphost,
                                                         string username,
                                                         string password,
                                                         bool useProxy = false,
                                                         int port = 0,
                                                         FtpDataConnectionType? dataConnectionType = null)
    {
        var factory = await FtpClientFactory.GetInstanceAsync(ftphost.AbsoluteUri);

        return await factory.CreateAsync(username, password, useProxy, port, dataConnectionType);
    }

    public static Task ReleaseAsync(AsyncFtpClient client) => FtpClientFactory.ReleaseOwnedAsync(client);

    public static async Task<string?> DownloadFileFtpAsync(string inputdirpath,
                                                          Uri ftphost,
                                                          string ftpfilepath,
                                                          string username,
                                                          string password,
                                                          IProgressAggregator? viewModel = null,
                                                          bool useProxy = false,
                                                          int port = 0,
                                                          FtpDataConnectionType? dataConnectionType = null)
    {
        var fName = Path.GetFileName(ftpfilepath);

        if (fName != null
            && !fName.Contains("[")
            && !fName.Contains("]")) // TODO: FluentFTP#268 - brackets in filenames cause timeout
        {
            var target = Path.Combine(inputdirpath, fName);
            var size = await RunFtpActionAsync(ftphost,
                username,
                password,
                useProxy,
                async client =>
                {
                    var remoteSize = await client.GetFileSize(ftpfilepath);
                    viewModel?.PrgSetMax(remoteSize);
                    var prg = GetProgress(viewModel, remoteSize);
                    await client.DownloadFile(target, ftpfilepath, FtpLocalExists.Overwrite, FtpVerify.None, prg);

                    return remoteSize;
                },
                port,
                dataConnectionType);

            var info = new FileInfo(target);

            if (info.Exists
                && info.Length == size)
            {
                return target;
            }
        }

        return null;
    }

    public static async Task<FtpStatus?> UploadFileFtpAsync(string inputfilepath,
                                                            Uri ftphost,
                                                            string ftpdirpath,
                                                            string username,
                                                            string password,
                                                            Func<string, bool> onTargetExists,
                                                            IProgressAggregator? viewModel = null,
                                                            bool useProxy = false,
                                                            int port = 0,
                                                            FtpDataConnectionType? dataConnectionType = null)
    {
        var fName = Path.GetFileName(inputfilepath);

        if (fName == null)
            return null;

        var target = $"{ftpdirpath}/{fName}";

        return await RunFtpActionAsync<FtpStatus?>(ftphost,
            username,
            password,
            useProxy,
            async client =>
            {
                var size = new FileInfo(inputfilepath).Length;
                viewModel?.PrgSetMax(size);
                var prg = GetProgress(viewModel, size);

                if (await client.FileExists(target)
                    && !onTargetExists(target))
                {
                    return null;
                }

                return await client.UploadFile(inputfilepath,
                    target,
                    FtpRemoteExists.Overwrite,
                    true,
                    FtpVerify.None,
                    prg,
                    CancellationToken.None);
            },
            port,
            dataConnectionType);
    }

    public static async Task<FtpListItem[]> GetListingAsync(string ftpdirpath,
                                                            Uri ftphost,
                                                            string username,
                                                            string password,
                                                            bool useProxy = false,
                                                            int port = 0,
                                                            FtpDataConnectionType? dataConnectionType = null)
    {
        FtpListItem[] res = [];

        if (ftphost != null)
        {
            res = await RunFtpActionAsync(ftphost,
                username,
                password,
                useProxy,
                client => client.GetListing(ftpdirpath),
                port,
                dataConnectionType);
        }

        return res;
    }

    private static IProgress<FtpProgress>? GetProgress(IProgressAggregator? viewModel, long size)
    {
        return viewModel != null
            ? new Progress<FtpProgress>(x =>
            {
                if (x.Progress != -1d)
                    viewModel.PrgSet(size * x.Progress / 100d);
                else
                    viewModel.SetIsIndeterminate(true);
            })
            : null;
    }

    private static async Task<T> RunFtpActionAsync<T>(Uri ftphost,
                                                      string username,
                                                      string password,
                                                      bool useProxy,
                                                      Func<AsyncFtpClient, Task<T>> func,
                                                      int port = 0,
                                                      FtpDataConnectionType? dataConnectionType = null)
    {
#pragma warning disable IDISP001 // released in the finally block below
        var client = await CreateAsync(ftphost, username, password, useProxy, port, dataConnectionType);
#pragma warning restore IDISP001

        try
        {
            await client.Connect();

            return await func(client);
        }
        finally
        {
            await ReleaseAsync(client);
        }
    }
}
