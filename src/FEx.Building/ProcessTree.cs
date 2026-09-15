using Nuke.Common.Tooling;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FEx.Building;

/// <summary>
/// A started process whose <see cref="Kill" /> takes everything it spawned with it.
/// </summary>
/// <remarks>
/// <c>dotnet jb inspectcode</c> is a tree - the muxer, the tool host, the inspection itself, its MSBuild
/// and Roslyn workers - and NUKE's <see cref="IProcess.Kill" /> reaches only the root. On Windows a job
/// object takes the rest down with it; on Linux nothing does, and the inspection keeps every core busy for
/// minutes after the build that started it has ended. The tree kill is what makes a discard mean the same
/// on both.
/// </remarks>
public sealed class ProcessTree : IProcess
{
    private readonly IProcess _root;

    public ProcessTree(IProcess root) => _root = root;

    public string FileName => _root.FileName;
    public string Arguments => _root.Arguments;
    public string WorkingDirectory => _root.WorkingDirectory;
    public IReadOnlyCollection<Output> Output => _root.Output;
    public int ExitCode => _root.ExitCode;
    public bool HasExited => _root.HasExited;
    public int Id => _root.Id;

    /// <exception cref="InvalidOperationException">The root has already exited - the tree is gone with it.</exception>
    public void Kill()
    {
        // Checked first, and not only for the message: a freed id can already belong to somebody else's
        // process, and a tree kill aimed by id would take that one down instead.
        if (HasExited)
            throw new InvalidOperationException($"Process {Id} has already exited");

        using var tree = Process.GetProcessById(Id);
        tree.Kill(entireProcessTree: true);
    }

    public bool WaitForExit() => _root.WaitForExit();

#pragma warning disable IDISP007 // The tree owns the root it was handed: its creator never sees it again.
    public void Dispose() => _root.Dispose();
#pragma warning restore IDISP007
}
