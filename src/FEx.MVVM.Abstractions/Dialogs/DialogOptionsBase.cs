using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class DialogOptionsBase<TDialog>
{
    public TDialog Dialog { get; protected set; }

    protected abstract TDialog MapToDialog();

    protected T ShowDialog<T>(Func<TDialog, T> dialogFunc, IProgressAggregator viewModel)
    {
        bool wasRunning = viewModel?.Stopwatch?.IsRunning ?? false;

        if (wasRunning)
            viewModel.Stopwatch.Stop();

        try
        {
            Dialog = MapToDialog();

            return dialogFunc(Dialog);
        }
        finally
        {
            if (wasRunning)
                viewModel.Stopwatch.Start();
        }
    }
}