using Nuke.Common.Tooling;
using Shouldly;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace FEx.Building.Tests;

public sealed class ProcessTreeTests
{
    private static readonly TimeSpan Ceiling = TimeSpan.FromMinutes(3);

    [Fact]
    public void Kill_TakesTheProcessesTheRootSpawned_WithIt()
    {
        // A real tree, because this is exactly what a fake cannot show: pwsh running pwsh, the inner one
        // writing its own id. Killing the outer one alone leaves the inner one running. Every wait is on a
        // condition with a ceiling far above any load - starting two nested pwsh hosts takes many seconds on a
        // machine with more runnable processes than hardware threads - and none is a fixed pause.
        var innerIdFile = Path.Combine(Path.GetTempPath(), $"fex-inner-id-{Guid.NewGuid():N}");
        var inner = $"[IO.File]::WriteAllText('{innerIdFile}.tmp', [string]$PID); [IO.File]::Move('{innerIdFile}.tmp', '{innerIdFile}'); Start-Sleep -Seconds 600";
        var outer = $"pwsh -NoProfile -EncodedCommand {Encoded(inner)}";

        using var tree = new ProcessTree(ProcessTasks.StartProcess("pwsh",
            $"-NoProfile -EncodedCommand {Encoded(outer)}",
            logOutput: false,
            logInvocation: false)!);

        Process? innerProcess = null;
        try
        {
            WaitFor(() => File.Exists(innerIdFile) || tree.HasExited, Ceiling);
            File.Exists(innerIdFile).ShouldBeTrue("the inner process never started");

            // The handle is held before the kill, so the id cannot be recycled while the test waits on it.
            innerProcess = Process.GetProcessById(int.Parse(File.ReadAllText(innerIdFile), CultureInfo.InvariantCulture));

            tree.Kill();

            innerProcess.WaitForExit(Ceiling).ShouldBeTrue("the inner process outlived the kill of its parent");
        }
        finally
        {
            // A failed assertion must not leave the tree it started behind to keep burning the machine.
            if (!tree.HasExited)
                tree.Kill();

            if (innerProcess is { HasExited: false })
                innerProcess.Kill();

            innerProcess?.Dispose();
            File.Delete(innerIdFile);
            File.Delete($"{innerIdFile}.tmp");
        }
    }

    [Fact]
    public void Kill_OnARootThatHasExited_RefusesRatherThanAimingByItsId()
    {
        // The id of an exited process is free for the next one the system starts - a tree kill by that id
        // would hit a stranger.
        using var root = ProcessTasks.StartProcess("pwsh", "-NoProfile -Command exit 0", logOutput: false, logInvocation: false)!;
        root.WaitForExit();
        using var tree = new ProcessTree(root);

        Should.Throw<InvalidOperationException>(tree.Kill);
    }

    private static string Encoded(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    private static void WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        var clock = Stopwatch.StartNew();
        while (!condition() && clock.Elapsed <= timeout)
            Thread.Sleep(100);
    }
}
