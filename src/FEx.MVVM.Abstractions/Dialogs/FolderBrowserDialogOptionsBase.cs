using System;

namespace FEx.MVVM.Abstractions.Dialogs;

public abstract class FolderBrowserDialogOptionsBase<TDialog> : DialogOptionsBase<TDialog>
{
    /// <summary>
    ///     Gets or sets the descriptive text displayed above the tree view control in the dialog box.
    /// </summary>
    /// <returns>
    ///     The description to display. The default is an empty string ("").
    /// </returns>
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the initial directory displayed by the file dialog box.
    /// </summary>
    /// <value>
    ///     The initial directory.
    /// </value>
    public string SelectedPath { get; set; }

    /// <summary>
    ///     Gets or sets the default file name extension.
    /// </summary>
    /// <value>
    ///     The default ext.
    /// </value>
    public bool ShowNewFolderButton { get; set; }

    public Environment.SpecialFolder RootFolder { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FolderBrowserDialogOptionsBase" /> class.
    /// </summary>
    protected FolderBrowserDialogOptionsBase()
    {
        SelectedPath = string.Empty;
        ShowNewFolderButton = true;
        Description = string.Empty;
        RootFolder = Environment.SpecialFolder.Desktop;
    }
}