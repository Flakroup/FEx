using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FEx.Agnostics.Utilities;

public interface ICmd : IDisposable
{
    int? Code { get; }
    StringBuilder ErrOut { get; }
    StringBuilder Output { get; }
    Process Proc { get; }
    ProcessStartInfo StartInfo { get; }
    Stopwatch Stopwatch { get; }

    Task RunAsync(string args, string? verb = null, bool waitForExit = true, Func<ICmd, Task>? onStarted = null);
}