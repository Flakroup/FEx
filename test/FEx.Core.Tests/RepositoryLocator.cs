using System;
using System.IO;

namespace FEx.Core.Tests;

/// <summary>
/// Finds the repository root for tests that read repository files: the nearest directory above the test output
/// that holds <c>FEx.slnx</c>.
/// </summary>
internal static class RepositoryLocator
{
    public static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FEx.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException($"FEx.slnx was not found above {AppContext.BaseDirectory}.");
    }
}
