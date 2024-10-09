namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class OpenFileDialogOptionsBase<TDialog> : FileDialogOptionsBase<TDialog>
{
    /// <summary>
    /// Gets or sets an option indicating whether <see cref="T:Microsoft.Win32.OpenFileDialog" /> allows users to
    /// select multiple files.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if multiple selections are allowed; otherwise, <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </returns>
    public bool Multiselect { get; set; }
}