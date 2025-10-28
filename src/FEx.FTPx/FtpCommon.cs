using FEx.Logging;
using FEx.MVVM.Abstractions.Interfaces;
using FluentFTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.FTPx;

public static class FtpCommon
{
    public static async Task<FtpListItem> GetFtpFileInfoAsync(string ftpfilepath,
                                                              Uri ftphost,
                                                              string username,
                                                              string password,
                                                              bool useProxy = false,
                                                              int port = 0)
    {
        try
        {
            var client = await CreateAsync(ftphost, username, password, useProxy, port);
            //client.ListingParser = FtpParser.Unix;
            //client.Encoding = Encoding.UTF8;
            //client.DataConnectionType = FtpDataConnectionType.PASV;
            await client.ConnectAsync();
            //string parentPath = ftpfilepath.GetFtpDirectoryName();
            //FtpListItem[] ldist = (await client.GetListingAsync("/video")).Where(x => x.FullName == parentPath).ToArray();
            //string a = HttpUtility.UrlEncode("[");
            //string b = HttpUtility.UrlEncode("]");
            //string encodedPath = parentPath.Replace("[", a).Replace("]", b);
            var item = await client.GetObjectInfoAsync(ftpfilepath, true);
            //await client.SetWorkingDirectoryAsync(parentPath);
            //string xyz = await client.GetWorkingDirectoryAsync();
            //FtpListItem[] list = await client.GetListingAsync(parentPath, FtpListOption.Auto);
            //if (list.Any())
            //{

            //}
            //FtpListItem item = list.Find(x => x.FullName == ftpfilepath);
            await ReleaseAsync(client);

            return item;
        }
        catch (Exception ex)
        {
            FExLoggingModule.Log(ex.ToString(), typeof(FtpCommon));

            return null; //todo https://github.com/robinrodricks/FluentFTP/issues/268 currently ignored
        }
    }

    public static async Task<FtpClient> CreateAsync(Uri ftphost,
                                                    string username,
                                                    string password,
                                                    bool useProxy = false,
                                                    int port = 0)
    {
        var factory = await FtpClientFactory.GetInstanceAsync(ftphost.AbsoluteUri);

        return await factory.CreateAsync(username, password, useProxy, port);
    }

    public static async Task ReleaseAsync(FtpClient client)
    {
        var factory = await FtpClientFactory.GetInstanceAsync($"ftp://{client.Host}");
        await factory.ReleaseClientAsync(client);
    }

    public static async Task<string> DownloadFileFtpAsync(string inputdirpath,
                                                          Uri ftphost,
                                                          string ftpfilepath,
                                                          string username,
                                                          string password,
                                                          IProgressAggregator viewModel = null,
                                                          bool useProxy = false)
    {
        var fName = Path.GetFileName(ftpfilepath);

        if (fName != null
            && (!fName.Contains("[") && !fName.Contains("]") || Debugger.IsAttached)) //todo bug causing timeout
        {
            var target = Path.Combine(inputdirpath, fName);
            var client = await CreateAsync(ftphost, username, password, useProxy);
            await client.ConnectAsync();
            var size = await client.GetFileSizeAsync(ftpfilepath);
            viewModel?.PrgSetMax(size);
            var prg = GetProgress(viewModel, size);
            await client.DownloadFileAsync(target, ftpfilepath, FtpLocalExists.Overwrite, FtpVerify.None, prg);
            await ReleaseAsync(client);

            var info = new FileInfo(target);

            if (info.Exists
                && info.Length == size)
                return target;
        }

        return null;
    }

    public static async Task<FtpStatus?> UploadFileFtpAsync(string inputfilepath,
                                                            Uri ftphost,
                                                            string ftpdirpath,
                                                            string username,
                                                            string password,
                                                            Func<string, bool> onTargetExists,
                                                            IProgressAggregator viewModel = null,
                                                            bool useProxy = false)
    {
        FtpStatus? res = null;
        var fName = Path.GetFileName(inputfilepath);

        if (fName != null)
        {
            var target = $"{ftpdirpath}/{fName}";
            var client = await CreateAsync(ftphost, username, password, useProxy);
            await client.ConnectAsync();
            var size = new FileInfo(inputfilepath).Length;
            viewModel?.PrgSetMax(size);
            var prg = GetProgress(viewModel, size);

            if (!await client.FileExistsAsync(target)
                || onTargetExists(target))
                //CommonMVVM.ShowMessage($"File {target} already exists. Do you want to overwrite it?", typeof(FtpCommon), ownerWindow, true, true, "Target file exists", MessageIcon.Question, MessageButton.YesNo) == MessageResult.Yes
                res = await client.UploadFileAsync(inputfilepath,
                    target,
                    FtpRemoteExists.Overwrite,
                    true,
                    FtpVerify.None,
                    prg,
                    CancellationToken.None);

            await ReleaseAsync(client);
        }

        return res;
    }

    public static async Task<FtpListItem[]> GetListingAsync(string ftpdirpath,
                                                            Uri ftphost,
                                                            string username,
                                                            string password,
                                                            bool useProxy = false)
    {
        FtpListItem[] res = [];

        if (ftphost != null)
            res = await RunFtpActionAsync(ftphost,
                username,
                password,
                useProxy,
                client => client.GetListingAsync(ftpdirpath));

        return res;
    }

    public static void RunFtpAction(Uri targetUrl, Action<FtpClient> action, NetworkCredential credentials = null)
    {
        using var client = new FtpClient(targetUrl.Host)
        {
            Credentials = credentials
        };

        client.Connect();
        action(client);
        client.Disconnect();
    }

    private static IProgress<FtpProgress> GetProgress(IProgressAggregator viewModel, long size)
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
                                                      Func<FtpClient, Task<T>> func)
    {
        var client = await CreateAsync(ftphost, username, password, useProxy);
        await client.ConnectAsync();
        var res = await func(client);
        await ReleaseAsync(client);

        return res;
    }
}