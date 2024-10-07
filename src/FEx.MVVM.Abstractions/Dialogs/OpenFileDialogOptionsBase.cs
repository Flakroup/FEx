namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class OpenFileDialogOptionsBase<TDialog> : FileDialogOptionsBase<TDialog>
{
    public bool Multiselect { get; set; }
}