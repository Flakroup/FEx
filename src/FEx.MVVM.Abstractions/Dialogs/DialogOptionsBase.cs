using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class DialogOptionsBase<TDialog>
{
    // Assigned by ShowDialog before any consumer reads it.
    public TDialog Dialog { get; protected set; } = default!;

    protected abstract TDialog MapToDialog();

    protected abstract void MapFromDialog(TDialog dialog);

    protected T ShowDialog<T>(Func<TDialog, T> dialogFunc, IProgressAggregator viewModel)
    {
        var stopwatch = viewModel?.Stopwatch;
        var wasRunning = stopwatch?.IsRunning ?? false;

        if (wasRunning)
            stopwatch!.Stop();

        try
        {
            Dialog = MapToDialog();

            var result = dialogFunc(Dialog);
            MapFromDialog(Dialog);

            return result;
        }
        finally
        {
            if (wasRunning)
                stopwatch!.Start();
        }
    }
}