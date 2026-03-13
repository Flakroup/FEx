using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class DialogOptionsBase<TDialog>
{
    public TDialog Dialog { get; protected set; }

    protected abstract TDialog MapToDialog();

    protected abstract void MapFromDialog(TDialog dialog);

    protected T ShowDialog<T>(Func<TDialog, T> dialogFunc, IProgressAggregator viewModel)
    {
        var wasRunning = viewModel?.Stopwatch?.IsRunning ?? false;

        if (wasRunning)
            viewModel.Stopwatch.Stop();

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
                viewModel.Stopwatch.Start();
        }
    }
}