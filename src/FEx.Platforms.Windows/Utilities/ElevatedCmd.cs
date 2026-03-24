using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;
using AdminHelper = UACHelper.UACHelper;

namespace FEx.Platforms.Windows.Utilities;

public class ElevatedCmd : Cmd
{
    public string OutFile { get; protected set; }
    public Task OutTask { get; protected set; }

    public ElevatedCmd(string procName = "cmd")
    {
        StartInfo = new()
        {
            FileName = procName,
            UseShellExecute = true
        };
    }

    protected override void StartProc()
    {
        AttachToOutput();
#pragma warning disable IDISP003 // Process managed by base class Cmd
        Proc = AdminHelper.StartElevated(StartInfo);
#pragma warning restore IDISP003
    }

    protected override void OnStarted(bool waitForExit, Action<ICmd> onStarted)
    {
        onStarted?.Invoke(this);

        if (!waitForExit)
            OutTask = Task.Run(GetProcOutput);
        else
            GetProcOutput();
    }

    protected override void AttachToOutput()
    {
        OutFile = Path.GetTempFileName();
        StartInfo.Arguments = $"{StartInfo.Arguments} > {OutFile}";
    }

    private void GetProcOutput()
    {
        Proc.WaitForExit();
        File.ReadAllLines(OutFile).ForEachInEnumerable(x => Output.AppendLine(x));
    }
}