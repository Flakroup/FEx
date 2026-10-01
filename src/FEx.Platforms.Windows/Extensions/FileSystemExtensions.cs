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
    // as code (#77). -LiteralPath keeps [ ] in a path from being read as a wildcard. The root is
    // listed alongside its descendants, so an empty directory is still checked. Progress is
    // silenced because Windows PowerShell writes module-loading progress to a redirected stderr,
    // which the caller reads as a failed check.
    internal const string AccessCheckScript =
        "$ProgressPreference = 'SilentlyContinue'; (@(Get-Item -LiteralPath $env:" + AccessCheckPathVariable + ") + @(Get-ChildItem -LiteralPath $env:" + AccessCheckPathVariable + " -Recurse))"
        + " | % { $path1 = $_.FullName; Get-Acl -LiteralPath $_.FullName }"
        + " | % { $owner = $_.Owner; ($_.Access.IdentityReference | % { (($env:" + AccessCheckEveryoneVariable + " -like $_) -or ($env:" + AccessCheckUserVariable + " -like $_)) }) -contains $true }"
        + " | % { $path1 + '|' + $_ + '|' + (($owner -like $env:" + AccessCheckEveryoneVariable + ") -or ($owner -like $env:" + AccessCheckUserVariable + ")) }";

    internal const string AccessCheckArguments = "-NoProfile -NonInteractive -Command " + AccessCheckScript;

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

        var argsA = BuildSetOwnerArguments(dInfo.FullName, sid);
        var argsB = BuildGrantArguments(dInfo.FullName, sid);
        bool isSuccess;

        // icacls.exe is started directly, not through cmd /C, so no shell ever parses the path.
        using (var c = new Cmd("icacls"))
        {
            c.Run(argsA);
            isSuccess = c.ErrOut.ToString().IsNullOrEmptyString() && c.Code == 0;

            if (isSuccess)
            {
                c.Run(argsB);
                isSuccess = c.ErrOut.ToString().IsNullOrEmptyString() && c.Code == 0;
            }
        }

        // The elevated fallback needs cmd for its output redirection, and cmd expands %VAR% even
        // inside double quotes, so a path containing % would reach icacls as a different path.
        // Such a path is never elevated; the final check below then reports the failure.
        if (!isSuccess && CanPassThroughCmd(dInfo.FullName))
        {
            using var c = new ElevatedCmd();

            try
            {
                RunIcacls("icacls " + argsA, c);
                RunIcacls("icacls " + argsB, c);
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

    // The SID form (*S-1-1-0 for Everyone) does not depend on the OS language; a localized name
    // such as "Tout le monde" contains spaces that icacls splits into separate arguments.
    internal static string BuildSetOwnerArguments(string path, SecurityIdentifier sid) =>
        $"{QuoteArgument(path)} /T /C /setowner *{sid.Value}";

    internal static string BuildGrantArguments(string path, SecurityIdentifier sid) =>
        $"{QuoteArgument(path)} /grant *{sid.Value}:(OI)(CI)F /T";

    // Quotes one argument for the Windows command-line parser: a run of backslashes before the
    // closing quote is doubled so it is not read as an escaped quote (e.g. "C:\").
    internal static string QuoteArgument(string value)
    {
        if (value.IndexOf('"') >= 0)
            throw new ArgumentException("A Windows path cannot contain a double quote.", nameof(value));

        var trailingBackslashes = value.Length - value.TrimEnd('\\').Length;

        return "\"" + value + new string('\\', trailingBackslashes) + "\"";
    }

    internal static bool CanPassThroughCmd(string path) => path.IndexOf('%') < 0;

    /// <returns><see langword="true"/> when access still has to be granted, including when the check itself failed.</returns>
    private static bool CheckAccess(DirectoryInfo dInfo,
                                    NTAccount everyoneAccount,
                                    NTAccount userAccount,
                                    params string[] excludes)
    {
        var raw = QueryAccess(dInfo, everyoneAccount, userAccount);

        // A check that failed or saw nothing (the root itself is always listed) proves nothing,
        // so it must never read as "access is already in place".
        if (raw is null
            || raw.Length == 0)
            return true;

        var wrongOutput = false;
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

        var access = raw.AsParallel()
            .Select(x =>
            {
                var splitted = x.Split('|');

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

        return wrongOutput || access.Any(x => x?.HasEveryoneAccess != true || !x.HasEveryoneOwner);
    }

    /// <returns>The script's <c>path|hasAccess|isOwner</c> lines, or <see langword="null"/> when PowerShell wrote to stderr or exited non-zero.</returns>
    internal static string[]? QueryAccess(DirectoryInfo dInfo, NTAccount everyoneAccount, NTAccount userAccount)
    {
        using var c = CreateAccessCheckCmd(dInfo.FullName, everyoneAccount.Value, userAccount.Value);
        c.Run(AccessCheckArguments);

        if (c.Code != 0
            || c.ErrOut.ToString().IsNotNullOrEmptyString())
            return null;

        return
        [
            ..c.Output.ToString()
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
        ];
    }

    internal static Cmd CreateAccessCheckCmd(string path, string everyoneAccount, string userAccount) =>
        new("powershell", cfg: si => SetAccessCheckEnvironment(si, path, everyoneAccount, userAccount));

    internal static void SetAccessCheckEnvironment(ProcessStartInfo startInfo,
                                                   string path,
                                                   string everyoneAccount,
                                                   string userAccount)
    {
        startInfo.Environment[AccessCheckPathVariable] = path;
        startInfo.Environment[AccessCheckEveryoneVariable] = everyoneAccount;
        startInfo.Environment[AccessCheckUserVariable] = userAccount;

        // A host started from pwsh 7 passes on a PSModulePath that makes Windows PowerShell 5.1
        // fail to load Get-Acl's module; without it 5.1 falls back to its own default.
        startInfo.Environment.Remove("PSModulePath");
    }

    private static void RunIcacls(string args, ElevatedCmd c)
    {
        c.Run(args);
        var lines = c.Output.ToString().Trim().Split('\n');
        var last = lines.Last();
        var failed = last.Split(';')[1];
        var failedCount = int.Parse(failed.Trim().Split(' ')[2]);

        if (c.Code != 0
            || failedCount != 0)
            throw new($"Command has failed: {last}");
    }
}
