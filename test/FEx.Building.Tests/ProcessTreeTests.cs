using Nuke.Common.Tooling;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace FEx.Building.Tests;

public sealed class ProcessTreeTests
{
    [Fact]
    public void Kill_TakesTheProcessesTheRootSpawned_WithIt()
    {
        // A real tree, because this is exactly what a fake cannot show: pwsh running pwsh, the inner one
        // writing a heartbeat. Killing the outer one alone leaves the heartbeat going.
        var heartbeat = Path.Combine(Path.GetTempPath(), $"fex-heartbeat-{Guid.NewGuid():N}");
        var inner = $"while ($true) {{ [IO.File]::WriteAllText('{heartbeat}', [DateTime]::UtcNow.Ticks); Start-Sleep -Milliseconds 50 }}";
        var outer = $"pwsh -NoProfile -EncodedCommand {Encoded(inner)}";

        using var tree = new ProcessTree(ProcessTasks.StartProcess("pwsh",
            $"-NoProfile -EncodedCommand {Encoded(outer)}",
            logOutput: false,
            logInvocation: false)!);

        try
        {
            WaitFor(() => File.Exists(heartbeat), TimeSpan.FromSeconds(30)).ShouldBeTrue("the inner process never started");

            tree.Kill();

            tree.WaitForExit();
            Thread.Sleep(300);
            var last = File.ReadAllText(heartbeat);
            Thread.Sleep(500);

            File.ReadAllText(heartbeat).ShouldBe(last);
        }
        finally
        {
            File.Delete(heartbeat);
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

    private static bool WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed > timeout)
                return false;

            Thread.Sleep(100);
        }

        return true;
    }
}
