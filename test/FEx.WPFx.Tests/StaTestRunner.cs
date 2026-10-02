using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace FEx.WPFx.Tests;

/// <summary>Runs WPF object tests on a dedicated STA thread.</summary>
internal static class StaTestRunner
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ExceptionDispatchInfo.Capture(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        error?.Throw();
    }
}
