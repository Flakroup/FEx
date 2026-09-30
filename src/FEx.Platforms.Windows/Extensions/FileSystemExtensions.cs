using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Utilities;
using FEx.Core.Abstractions.Extensions;
using FEx.Platforms.Windows.Models;
using FEx.Platforms.Windows.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace FEx.Platforms.Windows.Extensions;

// CA1416: This type is Windows-only by design - it manipulates NTFS ACLs via WindowsIdentity,
// NTAccount/SecurityIdentifier and icacls. CA1416 fires only on the net10.0 target
// (cross-platform); netstandard targets do not run the platform analyzer. Suppressed rather
// than annotated because SupportedOSPlatformAttribute is unavailable on the netstandard2.0 BCL.
#pragma warning disable CA1416

public static class FileSystemExtensions
{
    internal const string AccessCheckPathVariable = "FEX_ACL_PATH";
    internal const string AccessCheckEveryoneVariable = "FEX_ACL_EVERYONE";
    internal const string AccessCheckUserVariable = "FEX_ACL_USER";

    // Constant on purpose: the path and account names reach PowerShell only through the child's
    // environment, never through the script text, so no value can end a string literal and run
    // as code (#77). -LiteralPath keeps [ ] in a path from being read as a wildcard.
    internal const string AccessCheckScript =
        "Get-ChildItem -LiteralPath $env:" + AccessCheckPathVariable + " -Recurse"
        + " | % { $path1 = $_.FullName; Get-Acl -LiteralPath $_.FullName }"
        + " | % { $owner = $_.Owner; ($_.Access.IdentityReference | % { (($env:" + AccessCheckEveryoneVariable + " -like $_) -or ($env:" + AccessCheckUserVariable + " -like $_)) }) -contains $true }"
        + " | % { $path1 + '|' + $_ + '|' + (($owner -like $env:" + AccessCheckEveryoneVariable + ") -or ($owner -like $env:" + AccessCheckUserVariable + ")) }";

    public static bool SetEverybodyFullControl(this DirectoryInfo dInfo, params string[] excludes)
    {
        dInfo.Create();
        SecurityIdentifier user;

        using (var current = WindowsIdentity.GetCurrent())
            user = current.User.Guard(nameof(WindowsIdentity.User));

        return EnsureAccess(dInfo, new(WellKnownSidType.WorldSid, null), user, excludes);
    }

    private static bool EnsureAccess(DirectoryInfo dInfo,
                                     SecurityIdentifier sid,
                                     SecurityIdentifier cuSid,
                                     params string[] excludes)
    {
        var account = (NTAccount)sid.Translate(typeof(NTAccount));
        var cuAccount = (NTAccount)cuSid.Translate(typeof(NTAccount));
        var shouldRun = CheckAccess(dInfo, account, cuAccount, excludes);

        if (!shouldRun)
            return true;

        var everyone = account.Value;
        var argsA = $"icacls \"{dInfo.FullName}\" /T /C /setowner {everyone}";
        var argsB = $"icacls \"{dInfo.FullName}\" /grant {everyone}:(OI)(CI)F /T";
        bool isSuccess;

        using (var c = new Cmd())
        {
            c.Run(argsA);
            isSuccess = c.ErrOut.ToString().IsNullOrEmptyString() && c.Code == 0;

            if (isSuccess)
            {
                c.Run(argsB);
                isSuccess = c.ErrOut.ToString().IsNullOrEmptyString() && c.Code == 0;
            }
        }

        if (!isSuccess)
        {
            using var c = new ElevatedCmd();

            try
            {
                RunIcacls(argsA, c);
                RunIcacls(argsB, c);
                //isSuccess = true;
            }
            catch (Exception ex)
            {
                //isSuccess = false;
                ex.HandleException();
            }
        }

        return !CheckAccess(dInfo, account, cuAccount, excludes);
    }

    private static bool CheckAccess(DirectoryInfo dInfo,
                                    NTAccount everyoneAccount,
                                    NTAccount userAccount,
                                    params string[] excludes)
    {
        string errOut;
        string output;

        using (var c = new Cmd("powershell",
                               cfg: si => SetAccessCheckEnvironment(si,
                                                                    dInfo.FullName,
                                                                    everyoneAccount.Value,
                                                                    userAccount.Value)))
        {
            c.Run(AccessCheckScript);
            errOut = c.ErrOut.ToString().Trim();
            output = c.Output.ToString().Trim();
        }

        if (errOut.IsNotNullOrEmptyString())
            return false;

        var wrongOutput = false;
        ACL?[]? access = null;

        var raw = output.IsNotNullOrEmptyString()
            ? output.Split('\n')
            : [];

        HashSet<string>? excluded = null;

        if (excludes.IsNotNullOrEmptyList())
            try
            {
                excluded =
                [
                    ..excludes.SelectMany(e => dInfo.GetFileSystemInfos(e, SearchOption.AllDirectories))
                        .Select(x => x.FullName)
                ];
            }
            catch (Exception ex)
            {
                ex.HandleException();
            }

        if (raw.IsNotNullOrEmptyList())
            access = raw.AsParallel()
                .Select(x =>
                {
                    var splitted = x.Trim().Split('|');

                    try
                    {
                        if (excludes.IsNullOrEmptyList()
                            || excluded?.Contains(splitted[0]) != true)
                            return new ACL(splitted);
                    }
                    catch
                    {
                        wrongOutput = true;
                    }

                    return null;
                })
                .Where(x => x is not null)
                .ToArray();

        return wrongOutput || (access?.Any(x => x?.HasEveryoneAccess != true || !x.HasEveryoneOwner) ?? false);
    }

    internal static void SetAccessCheckEnvironment(ProcessStartInfo startInfo,
                                                   string path,
                                                   string everyoneAccount,
                                                   string userAccount)
    {
        startInfo.Environment[AccessCheckPathVariable] = path;
        startInfo.Environment[AccessCheckEveryoneVariable] = everyoneAccount;
        startInfo.Environment[AccessCheckUserVariable] = userAccount;
    }

    private static void RunIcacls(string argsA, ElevatedCmd c)
    {
        c.Run(argsA);
        var lines = c.Output.ToString().Trim().Split('\n');
        var last = lines.Last();
        var failed = last.Split(';')[1];
        var failedCount = int.Parse(failed.Trim().Split(' ')[2]);

        if (c.Code != 0
            || failedCount != 0)
            throw new($"Command has failed: {last}");
    }
}