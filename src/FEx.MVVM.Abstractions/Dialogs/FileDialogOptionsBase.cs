namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class FileDialogOptionsBase<TDialog> : DialogOptionsBase<TDialog>
{
    /// <summary>
    ///     Gets or sets the file dialog box title.
    /// </summary>
    /// <value>
    ///     The title.
    /// </value>
    public string Title { get; set; }

    /// <summary>
    ///     Gets or sets the current file name filter string, which determines the choices that appear in the "Save as file
    ///     type" or "Files of type" box in the dialog box.
    /// </summary>
    /// <value>
    ///     The filter.
    /// </value>
    public string Filter { get; set; }

    /// <summary>
    ///     Gets or sets a string containing the file name selected in the file dialog box.
    /// </summary>
    /// <value>
    ///     The name of the file.
    /// </value>
    public string FileName { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the dialog box restores the directory to the previously selected directory
    ///     before closing.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [restore directory]; otherwise, <c>false</c>.
    /// </value>
    public bool RestoreDirectory { get; set; }

    /// <summary>
    ///     Gets or sets the initial directory displayed by the file dialog box.
    /// </summary>
    /// <value>
    ///     The initial directory.
    /// </value>
    public string InitialDirectory { get; set; }

    /// <summary>
    ///     Gets or sets the default file name extension.
    /// </summary>
    /// <value>
    ///     The default ext.
    /// </value>
    public string DefaultExt { get; set; }

    /// <summary>
    ///     Gets or sets the index of the filter currently selected in the file dialog box.
    /// </summary>
    /// <value>
    ///     The index of the filter.
    /// </value>
    public int FilterIndex { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [check file exists].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [check file exists]; otherwise, <c>false</c>.
    /// </value>
    public bool CheckFileExists { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [check path exists].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [check path exists]; otherwise, <c>false</c>.
    /// </value>
    public bool CheckPathExists { get; set; }

    /// <summary>Gets an array that contains one file name for each selected file.</summary>
    /// <returns>
    /// An array of <see cref="T:System.String" /> that contains one file name for each selected file. The default is
    /// an array with a single item whose value is <see cref="F:System.String.Empty" />.
    /// </returns>
    public string[] FileNames { get; set; }

    protected FileDialogOptionsBase()
    {
        FilterIndex = 1;
        DefaultExt = string.Empty;
        InitialDirectory = string.Empty;
        RestoreDirectory = true;
        FileName = string.Empty;
        Filter = "All files (*.*)|*.*";
        Title = string.Empty;
        CheckPathExists = true;
    }
}